<!-- devrecall-section: a-signal-not-a-force-kill -->
CancellationToken carries a request to stop. The receiving operation must observe it and stop at a supported safe point; the token does not kill a thread or undo completed writes. A timeout can request cancellation, but callers can cancel for other reasons too. In ASP.NET Core, RequestAborted signals an abandoned request, and endpoint CancellationToken parameters can receive that signal.

<!-- devrecall-section: keep-the-caller-s-lifetime -->
Accept the caller's token and pass it to async APIs that support cancellation. Dropping it or replacing it with CancellationToken.None means abandoned work can continue. If a separate timeout is needed, explicitly link that timeout with caller cancellation rather than ignoring the caller. CPU loops may need occasional ThrowIfCancellationRequested checks; a cancellable I/O API observes the token itself.

<!-- devrecall-section: request-to-database -->
```csharp
app.MapGet("/orders/{id:guid}", async (Guid id, OrderService service, CancellationToken token)
    => await service.GetOrderAsync(id, token));

public sealed class OrderService(AppDbContext db)
{
    public Task<Order?> GetOrderAsync(Guid id, CancellationToken token)
        => db.Orders.SingleOrDefaultAsync(order => order.Id == id, token);
}
```

The types are illustrative. The same token reaches the database API; there is no need to poll it manually beside every await.

<!-- devrecall-section: cancellation-versus-failure -->
An operation that observes cancellation commonly reports OperationCanceledException (including TaskCanceledException). When the caller requested cancellation, treat it as that outcome rather than blindly logging an unexpected server failure. Do not swallow it and claim success. An unrelated exception is still a failure, and cancellation is not proof that no side effect occurred. Whether the HTTP client can receive a response after disconnecting is a separate transport concern.

<!-- devrecall-section: key-takeaway -->
Cancellation is cooperative. Accept the caller's token and propagate it through cancellable async operations so abandoned work can stop. Requested cancellation is distinct from an unexpected failure and does not roll back completed side effects.
