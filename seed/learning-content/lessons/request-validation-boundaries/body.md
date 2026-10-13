<!-- devrecall-section: different-rules-have-different-owners -->
"Validate the request" sounds like one task, but backend rules come from different sources. JSON must bind to the expected shape. A title may have a public length limit. A referenced project must exist and belong to the caller. An order must never have a negative total. Treating all of these as controller checks produces duplicated logic and lets another entry point bypass important rules.

Good validation is layered by ownership. Reject malformed input early, coordinate rules that need repositories in the use case, and keep state invariants inside the domain object that must always enforce them.

<!-- devrecall-section: three-validation-boundaries -->
**Transport and contract validation** checks whether the request can represent the API operation: required values, string limits, supported enum names, and basic ranges. ASP.NET Core can report binding and model-validation failures before business orchestration begins.

**Application validation** checks facts needed by a use case: whether a referenced product exists, whether a name is unique in the caller's scope, or whether the authenticated user owns the target. This layer can coordinate repositories and authorization without putting infrastructure into the domain.

**Domain invariants** protect state regardless of caller. An aggregate should refuse an impossible transition or invalid value even when invoked from a background job, import, or unit test. HTTP validation can improve feedback, but it cannot be the only enforcement of a business invariant.

<!-- devrecall-section: a-create-order-flow -->
```csharp
public sealed record CreateOrderRequest(Guid CustomerId, IReadOnlyList<OrderLineRequest> Lines);

public async Task<Result<Guid>> HandleAsync(
    CreateOrderRequest request,
    Guid userId,
    CancellationToken cancellationToken)
{
    // Application boundary: lookup and ownership require persistence.
    var customer = await customerRepository.FindAsync(request.CustomerId, cancellationToken);
    if (customer is null || customer.UserId != userId)
        return Result.NotFound("CUSTOMER_NOT_FOUND");

    // Domain boundary: AddLine still protects quantity and order state.
    var order = Order.Create(Guid.NewGuid(), customer.Id);
    foreach (var line in request.Lines)
        order.AddLine(line.ProductId, line.Quantity);

    await orderRepository.AddAsync(order, cancellationToken);
    return Result.Success(order.Id);
}
```

The endpoint can reject an empty `Lines` collection or a quantity outside the public contract before calling the handler. The aggregate still enforces what must always be true. Some duplication is intentional when an early check gives better client feedback while the deeper rule remains authoritative.

<!-- devrecall-section: validation-is-not-every-protection -->
Validation does not replace authorization: a well-formed resource ID can still belong to another user. It does not replace optimistic concurrency: input can be valid but based on a stale version. It also does not make a check-then-insert uniqueness query race-free; the database constraint remains the final authority under concurrency.

Return validation failures using the API's established error contract, commonly a `400 Bad Request` with field-level details when the request can be corrected. Do not leak stack traces, SQL constraint messages, or internal entity names. Other outcomes—permission denied, not found, or conflict—retain their own semantics rather than being mislabeled as validation.

<!-- devrecall-section: key-takeaway -->
Put each rule where its knowledge belongs: request shape at the API boundary, use-case and ownership checks in Application, and always-valid state rules in Domain. Layered validation improves feedback without sacrificing invariant enforcement.
