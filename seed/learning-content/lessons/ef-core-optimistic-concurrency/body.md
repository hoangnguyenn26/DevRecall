<!-- devrecall-section: the-lost-update-comes-first -->
Alice and Bob both read version 4 of an order. Alice saves a change and the row becomes version 5. Bob then submits an edit based on version 4. An unconditional update can silently replace Alice's work. A successful SQL statement is not enough: the application must know whether the data still matches the state on which Bob made his decision.

<!-- devrecall-section: check-the-expected-version-at-write-time -->
Conceptually, UPDATE orders SET ..., version = 5 WHERE id = ... AND version = 4 succeeds only if the old version still matches. Zero matching rows indicates a stale write or a deleted row, not permission to overwrite. EF Core includes a configured concurrency token's original value in update/delete conditions and reports a conflict with DbUpdateConcurrencyException. Optimistic concurrency does not hold a lock during user think-time; database statements still use their normal locking behavior.

<!-- devrecall-section: an-application-managed-token -->
```csharp
// Mapping: an integer token works without SQL Server rowversion.
builder.Property(order => order.Version).IsConcurrencyToken();

// Order was loaded as a tracked entity with its original Version.
order.ChangeAddress(address); // Business method also increments Version.
try
{
    await db.SaveChangesAsync(cancellationToken);
}
catch (DbUpdateConcurrencyException)
{
    throw new OrderEditConflictException(order.Id);
}
```

These types are illustrative. Every relevant mutation must advance an application-managed token. For edits sent long after a read, also compare the client's expected version; loading the latest row alone does not detect that stale client decision.

<!-- devrecall-section: resolve-do-not-erase -->
A conflict response depends on the use case: reload and ask the user, merge independent changes, or reload and re-apply an operation only if it remains valid. A web API may use 409 Conflict. Retrying the same stale overwrite without reconsidering current state defeats the protection. No single merge/retry policy fits every business action.

<!-- devrecall-section: key-takeaway -->
Optimistic concurrency detects stale writes by requiring a matching expected version. Surface or resolve the conflict deliberately instead of silently replacing newer data. Token advancement and client expected-version checks are part of that design.
