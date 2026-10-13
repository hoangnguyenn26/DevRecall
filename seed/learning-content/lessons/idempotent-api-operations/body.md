<!-- devrecall-section: the-ambiguous-outcome -->
A client sends `POST /payments`, waits, and times out. The missing response does not prove that the request failed. The server may have committed the payment immediately before the connection disappeared. If the client repeats the request as a new operation, the customer can be charged twice.

Reliable APIs treat this as an **ambiguous outcome**. Networks, process restarts, and load balancers can all interrupt the response path after work has begun. "Never retry" leaves recoverable operations stuck; "always retry" can duplicate business effects. The API needs a way to recognize that two HTTP requests represent one logical mutation.

<!-- devrecall-section: identify-the-logical-operation -->
For a retryable non-idempotent operation, the client can generate an idempotency key and reuse it only for retries of that operation. The server scopes the key, commonly by caller and operation, then stores the completed outcome. A retry with the same key and equivalent payload returns the recorded outcome instead of creating another effect.

The key is not magic. The contract must define its scope and retention. It must also prevent one key from silently representing two different commands. A payload fingerprint can detect conflicting reuse, but it must be calculated from a canonical representation of the fields that determine operation identity—not from unstable serialization details.

<!-- devrecall-section: one-consistency-boundary -->
```csharp
await using var transaction =
    await db.Database.BeginTransactionAsync(cancellationToken);

// A unique constraint on (UserId, Operation, Key) is the final race guard.
var payment = Payment.Create(request.Amount, request.Currency);
db.Payments.Add(payment);
db.IdempotencyRecords.Add(IdempotencyRecord.Complete(
    userId,
    "CreatePayment",
    request.IdempotencyKey,
    requestFingerprint,
    payment.Id));

await db.SaveChangesAsync(cancellationToken);
await transaction.CommitAsync(cancellationToken);
```

The business change and the completed idempotency record share a transaction. A pre-insert lookup is useful for the normal retry path, but it cannot prevent two first attempts from racing. A database uniqueness constraint remains the authority; the application handles that conflict by loading the winning record and checking that the payload identity matches.

Real payment systems may involve an external provider that cannot join the database transaction. That requires provider-supported idempotency or a deliberately designed workflow such as an outbox and reconciliation. A local transaction alone cannot make a remote side effect atomic.

<!-- devrecall-section: deduplication-is-a-contract -->
Do not apply idempotency middleware indiscriminately to every request. Reads already have safe/idempotent HTTP semantics, while different mutations need different scopes, response retention, and privacy rules. Stored responses may contain sensitive data and should not be retained forever without a policy.

Idempotency also does not mean that every request receives byte-for-byte identical headers or timestamps. It means retries converge on one intended business outcome. The API can return a reconstructed response as long as the contract remains truthful.

<!-- devrecall-section: key-takeaway -->
Design retryable mutations around a stable logical operation identity. Deduplicate under a database uniqueness constraint, bind the key to the request meaning, and commit the business effect with its idempotency record whenever they share one consistency boundary.
