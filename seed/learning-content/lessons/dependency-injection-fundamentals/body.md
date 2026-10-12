<!-- devrecall-section: composition-versus-behavior -->
Imagine an order service that creates its own SQL repository. Changing storage or testing without a database now requires changing that service. Dependency injection moves the choice and construction of collaborators to the application boundary. The service declares what behavior it needs; the caller supplies an implementation. An interface is useful where an alternative implementation or boundary is meaningful, not as mandatory decoration on every class.

<!-- devrecall-section: before-and-after -->
```csharp
// Before: the service chooses its infrastructure.
public sealed class CoupledOrderService
{
    private readonly SqlOrderRepository _repository = new();
}

// After: the service consumes an explicit dependency.
public sealed class OrderService(IOrderRepository repository)
{
    public Task<Order?> FindAsync(Guid id, CancellationToken cancellationToken)
        => repository.FindAsync(id, cancellationToken);
}

// Application composition boundary:
services.AddScoped<IOrderRepository, SqlOrderRepository>();
services.AddScoped<OrderService>();
```

The example assumes the repository types exist. A test can construct OrderService with a fake repository without changing business code. Registration selects the implementation; it does not replace the need to choose an appropriate lifetime.

<!-- devrecall-section: key-takeaway -->
Constructor injection makes required dependencies explicit and replaceable. Keep object composition at the application boundary so business behavior does not choose or construct its infrastructure.
