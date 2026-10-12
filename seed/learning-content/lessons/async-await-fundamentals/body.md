<!-- devrecall-section: task-and-await -->
A Task represents completion of an operation; it may already be complete when returned. Await consumes its result or exception. If it is incomplete, await suspends the async method rather than blocking the calling thread. During genuinely asynchronous I/O in an ASP.NET Core request, the thread can return to the pool while the operation is pending. Awaiting a completed task can continue synchronously; await does not promise a thread switch.

<!-- devrecall-section: async-is-not-a-background-thread -->
Awaiting a database call does not start a new thread. Asynchronous I/O releases the waiting thread instead of adding parallelism. Background threads and `Task.Run` are tools for CPU-bound work; request I/O does not need them. The value of async on a web server is handling many concurrent requests with one thread pool, not making a single request faster.

<!-- devrecall-section: an-asynchronous-call-chain -->
```csharp
public sealed class OrderService(IOrderRepository repository)
{
    public async Task<OrderSummary> GetSummaryAsync(Guid orderId, CancellationToken cancellationToken)
    {
        // If the task is incomplete, await suspends without blocking the request thread.
        var order = await repository.GetByIdAsync(orderId, cancellationToken);
        return new OrderSummary(order.Id, order.Total);
    }
}
```

<!-- devrecall-section: why-result-and-wait-are-not-the-default -->
Using `.Result` or `.Wait()` to wait for incomplete asynchronous work blocks the calling thread. Under load, many blocked request threads can exhaust thread-pool capacity and increase latency. Await composes that work without holding a thread during the wait. The example also forwards the caller's `CancellationToken` to the repository so a supported operation can stop when cancellation is requested. Cancellation is a related concept, not a required next step or a guarantee that every operation stops immediately.

<!-- devrecall-section: key-takeaway -->
Task represents completion; await composes asynchronous work without blocking on an incomplete task. Async does not automatically create another thread. Avoid .Result and .Wait() in request code because blocking can exhaust thread-pool capacity under load.
