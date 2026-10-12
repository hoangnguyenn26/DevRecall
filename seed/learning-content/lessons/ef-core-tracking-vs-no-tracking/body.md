<!-- devrecall-section: read-or-update -->
Tracking records entity instances in the current DbContext. EF Core can detect changes to those instances and persist them with SaveChanges. Load tracked entities when the use case intends to modify them. If the result is only a read model, decide what must be returned before loading whole entities; tracking is not a substitute for authorization or filtering.

<!-- devrecall-section: entity-reads-and-dto-projections -->
```csharp
// Read-only entities: opt out of change tracking.
var orders = await dbContext.Orders.AsNoTracking()
    .Where(order => order.CustomerId == customerId)
    .ToListAsync(cancellationToken);

// Scalar DTO only: no entity instance is returned to track.
var summaries = await dbContext.Orders
    .Where(order => order.CustomerId == customerId)
    .Select(order => new OrderSummary(order.Id, order.Total))
    .ToListAsync(cancellationToken);
```

A DTO containing an Order entity can still cause that entity to be tracked by default. The important distinction is whether entity instances are materialized, not whether the outer result type is named DTO. Do not load entities and then project after materialization when SQL can select the required columns.

<!-- devrecall-section: key-takeaway -->
Use tracking when updating entities through the same DbContext. Use AsNoTracking for read-only entity queries; scalar-only DTO projections already return no entities to track. Filter and project before materialization.
