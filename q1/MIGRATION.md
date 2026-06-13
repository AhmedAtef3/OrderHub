# Migration: OrderHub .NET Framework 4.7 → .NET 8

## Approach: Strangler Fig behind a Shared Abstraction

The constraint — no downtime, existing WebForms/ASMX callers still in production — rules out a big-bang rewrite. Instead, apply the **Strangler Fig** pattern:

### Phase 1 — Introduce the abstraction (Week 1–2)
Extract an `IOrderProcessor` interface wrapping the legacy class's `ProcessOrder` method. All existing callers (Razor Pages, ASMX services) are updated to depend on the interface. No behavioural change — the seam now exists.

### Phase 2 — Run .NET 8 Application layer in parallel (Week 3–8)
`OrderHub.Core` and `OrderHub.Application` target `netstandard2.0` so both runtimes can reference them. Deploy a lightweight .NET 8 sidecar hosting `ProcessOrderUseCase` behind an internal HTTP endpoint.

### Phase 3 — Traffic migration (Week 9–16)
Add a proxy implementation of `IOrderProcessor` in the legacy app that routes to the sidecar. Use a feature flag to shift traffic school-by-school, with instant rollback.

### Phase 4 — Retire the legacy class (Week 17–22)
Once all schools are on the new path and a full August peak is observed, collapse the sidecar into the main .NET 8 app and delete the legacy class.

### Why this fits
- Zero downtime throughout.
- Incremental risk: one school at a time, instant rollback.
- `Core` and `Application` are independently testable before any traffic shifts.

---

## Risk to surface to leadership before starting

**SQL injection is live in production today.**

The legacy `OrderProcessor` concatenates `schoolId` and `line.Sku` directly into SQL strings. This is a one-day fix (parameterise the queries) that must not wait six months for the rewrite. Leadership should know this is a current exposure, not a future one.
