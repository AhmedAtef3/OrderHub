# Q3 — Razor Page Triage

## The 3 Issues That Most Matter

### 1. XSS via `Html.Raw(Model.SchoolName)`

```html
<h1>Order for @Html.Raw(Model.SchoolName)</h1>
```

`Html.Raw` bypasses Razor's automatic HTML encoding. If `SchoolName` is ever
sourced from user input or an external system and contains `<script>` tags or
event attributes, it executes in the admin's browser. In an order-confirmation
context, a successful XSS attack could silently alter quantities or intercept
form submissions before they reach the server.

**Fix:** Remove `Html.Raw`. Razor's default `@Model.SchoolName` encodes safely.
Only use `Html.Raw` for content you have explicitly sanitised and trust.

---

### 2. Form submits on *any click* inside a row — including quantity edits

```html
<div onclick="updateQty(@line.Id)">
  <input name="qty_@line.Id" value="@line.Quantity" />
</div>
```

The `onclick` is on the wrapping `<div>`, not on a dedicated button. Clicking
the quantity `<input>` to edit it fires the handler immediately, submitting the
form before the admin can finish typing. The `updateQty` function also does
nothing with the `id` argument — it just submits `document.forms[0]`. This
means partial quantity changes are silently sent, which in a financial order
context means incorrect line totals reaching the payment pipeline.

**Fix:** Remove `onclick` from the container div entirely. Wire the live-update
to the `input`'s `change` event in JS, and handle the subtotal update
client-side without a full form submission.

---

### 3. No CSRF protection and no `[ValidateAntiForgeryToken]`

The `<form method="post">` has no `@Html.AntiForgeryToken()` and the
`PageModel` has no `[ValidateAntiForgeryToken]` attribute. Any page on any
domain can POST to this endpoint and confirm (or manipulate) an order on behalf
of an authenticated school admin. Given that this form controls financial
order confirmation, a CSRF attack could submit orders the admin never intended
to place.

**Fix:** Add `@Html.AntiForgeryToken()` to the form and
`[ValidateAntiForgeryToken]` to the `OnPost` handler. ASP.NET Core Razor Pages
validates the token automatically when you use the tag helper `<form
method="post">` — as long as the token is present in the form.
