# Q4 — Decompose the OrderProcessor + Confirmation Workflow

---

## Part 1 — Microservice Boundaries

### Service Map

| Service | Owns | Exposes | Backing store |
|---|---|---|---|
| **Pricing Service** | Tier discount logic, embroidery surcharge rules, price catalogue | `POST /price-order` → `PricedOrderDto` | Products table (read-only replica) |
| **Inventory Service** | Stock levels, reservation, release | `POST /reserve`, `DELETE /reserve/{reservationId}` | Stock table (write master) |
| **Order Service** | Order lifecycle: draft → reserved → paid → confirmed | `POST /orders`, `GET /orders/{id}`, `PATCH /orders/{id}/status` | Orders + OrderLines tables |
| **Payment Service** | Payment intent creation, webhook ingestion, refunds | `POST /intents`, `POST /webhooks/payment` | PaymentIntents table |
| **Notification Service** | Email/SMS dispatch, retry queue | Consumes events from broker | Outbox table |

---

### Contracts Between Services

```
Browser → Order Service
    POST /orders  { schoolId, parentEmail, lines[] }

Order Service → Pricing Service  (sync, internal)
    POST /price-order  { schoolId, lines[] }
    ← { pricedLines[], subtotal }

Order Service → Inventory Service  (sync, internal)
    POST /reserve  { lines[] }
    ← { reservationId }   or  409 { sku, available }

Order Service → Payment Service  (sync, internal)
    POST /intents  { amount, email, orderId, idempotencyKey }
    ← { intentId, clientSecret }

Payment Service → Order Service  (async, webhook)
    POST /webhooks/payment  { intentId, status: "succeeded"|"failed" }

Order Service → Broker  (async, publish)
    Topic: order.confirmed  { orderId, schoolId, parentEmail, total }

Notification Service ← Broker  (async, consume)
    Subscribes: order.confirmed → sends confirmation email
```

---

### Transaction Boundaries

This is the most important design decision in the decomposition. Each service owns exactly one database. There is no distributed transaction.

The sequence uses the **Saga pattern** (orchestrated by Order Service):

```
1. Pricing    — stateless, no transaction needed
2. Inventory  — RESERVE (pessimistic lock for the duration of checkout)
3. Payment    — CREATE INTENT (money not moved yet)
4. [webhook]  — PAYMENT SUCCEEDED arrives
5. Order      — mark paid; publish order.confirmed
6. Inventory  — COMMIT reservation (stock decremented permanently)
```

Compensating actions on failure:
- Payment fails → Order Service calls `DELETE /reserve/{reservationId}` to release stock
- Webhook never arrives → Order Service runs a background job after TTL (e.g. 15 min) to expire the reservation and void the intent

No two-phase commit. Eventual consistency between Payment and Inventory is acceptable; the order is not visible to fulfilment until Order Service has confirmed both.

---

## Part 2 — Confirmation Flow Design

**Broker:** Azure Service Bus (or RabbitMQ on-prem). Topic-per-event-type, durable, at-least-once delivery.

**Flow from Submit to "Order Confirmed":**

1. Admin clicks **Submit** → `POST /orders` hits Order Service.
2. Order Service calls Pricing (sync) → Inventory reserve (sync) → Payment intent (sync). Returns `202 Accepted` with `orderId` immediately; browser polls or holds an SSE connection.
3. Payment provider calls `POST /webhooks/payment` when the card is charged. Payment Service verifies the signature and publishes `payment.succeeded` to the broker.
4. Order Service consumes `payment.succeeded`, marks the order `paid`, commits the inventory reservation, then publishes `order.confirmed`.
5. Notification Service consumes `order.confirmed` and sends the email.

**Retry policy:** Exponential backoff, 3 attempts, then dead-letter queue. Ops get alerted on DLQ depth.

**Idempotency:** Every intent creation sends `orderId` as the `idempotencyKey` to the payment provider. If the webhook is delivered twice, Order Service checks current status before transitioning — `paid → paid` is a no-op.

**If payment succeeds but email fails:** The order is already `paid` and the event is already on the broker. Notification Service retries independently from its own dead-letter queue. The admin sees "order confirmed" in the UI (driven by order status, not email delivery). A failed email is an ops alert, not a failed order.

---

## Part 3 — On-Call Runbook: Scenario (a)

### INC: School admin re-submits an identical order 30 seconds after first submission appears to hang

**Symptoms:** Admin sees no response or a timeout on the first submit, clicks Submit again. Two `POST /orders` requests arrive within ~30 seconds for the same `(schoolId, parentEmail, lines[])`.

**Immediate triage:**

1. Check Order Service logs for the first `orderId`. If it exists and status is `reserved` or `paid`, the first request succeeded — the hang was cosmetic (likely a slow payment-intent round-trip).
2. Check Payment Service for duplicate intents. Because Order Service sends `orderId` as the `idempotencyKey`, the provider will have returned the *same* intent for both requests — no double charge.
3. Check Inventory Service: a second `POST /reserve` for the same lines will either find them already reserved under `orderId-1` (and return 409) or succeed with a new `reservationId`. If a second reservation exists, release it immediately: `DELETE /reserve/{reservationId-2}`.

**Resolution:**
- If order-1 is `paid`: cancel order-2 at Order Service, release reservation-2, inform admin via support that one order was placed successfully.
- If order-1 is still `pending` after 15 min TTL: the saga will auto-compensate. Confirm with admin which order to keep.

**Follow-up:** Add a client-side disable-on-submit to the Confirm button to prevent duplicate submissions at source.
