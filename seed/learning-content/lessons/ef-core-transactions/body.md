<!-- devrecall-section: start-with-the-consistency-requirement -->
With a provider supporting transactions, one SaveChanges call normally applies its database changes atomically: all succeed or none do. If a workflow can stage its changes and save once, an extra manual transaction usually adds no protection. Ask whether partial persistence would violate a business requirement before choosing a larger boundary.

<!-- devrecall-section: several-database-steps-one-outcome -->
Multiple SaveChanges calls normally commit separately. An explicit transaction can group them, or group EF changes and SQL using the same database transaction, when partial results must not remain. A remote HTTP request is not automatically a participant: rolling back local data cannot undo an email or a successful payment request. Such workflows need a separate consistency design, not a longer local transaction.

<!-- devrecall-section: two-saves-with-a-deliberate-boundary -->
```csharp
// Assume both database steps must succeed together.
await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
try
{
    order.MarkFulfilled();
    await db.SaveChangesAsync(cancellationToken);
    db.FulfillmentLogs.Add(new FulfillmentLog(order.Id));
    await db.SaveChangesAsync(cancellationToken);
    await transaction.CommitAsync(cancellationToken);
}
catch
{
    // Disposal also rolls back an uncommitted transaction.
    await transaction.RollbackAsync(CancellationToken.None);
    throw;
}
```

<!-- devrecall-section: keep-the-protected-boundary-small -->
If both changes can be staged before one SaveChanges, prefer that simpler form; this example illustrates coordination when separate persistence steps are required. Keep slow remote calls and user think-time outside a database transaction: open transactions occupy connections and can hold locks. Configured retrying execution strategies require explicit transaction coordination; do not paste this snippet into a retrying workflow without considering that policy. These are explanatory fragments, not a complete production retry/cleanup implementation.

<!-- devrecall-section: key-takeaway -->
A normal relational SaveChanges already protects its database changes atomically. Use an explicit transaction only for a wider required database boundary, keep it short, and never assume rollback undoes external HTTP side effects.
