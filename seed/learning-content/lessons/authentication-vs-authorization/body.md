<!-- devrecall-section: identity-is-not-permission -->
Authentication establishes who the caller is using the configured credential mechanism. Authorization decides whether that identity may perform this action on this resource. A logged-in customer can still be forbidden from reading another customer's order. Neither a login screen nor possession of a guessed UUID establishes ownership.

<!-- devrecall-section: from-request-to-protected-resource -->
In an explicit ASP.NET Core pipeline, authentication runs before authorization. Roles, claims and policies are mechanisms for permission decisions; resource ownership is another important input. An endpoint requiring an authenticated user is a first gate, not the final check for every row it loads. Apply ownership filtering/checks server-side before returning private data.

<!-- devrecall-section: a-private-order-lookup -->
```csharp
// Simplified backend use case; currentUserId comes from trusted auth state.
var order = await db.Orders.AsNoTracking()
    .SingleOrDefaultAsync(order => order.Id == orderId
        && order.OwnerId == currentUserId, cancellationToken);
if (order is null)
    return OrderLookup.NotFound();
return OrderLookup.Found(new OrderSummary(order.Id, order.Total));
```

The authenticated caller cannot fetch another owner's order through this lookup. The types are illustrative. The server decides access; hiding a button in the browser does not protect an API.

<!-- devrecall-section: resource-aware-decisions -->
Resource-based authorization evaluates the requested action together with the particular resource, such as ownership of an order or permission to approve it. It may use an authorization handler or scoped query depending on the design. Unauthenticated and authenticated-but-forbidden are different outcomes; APIs often use 401 and 403 respectively, while some hide private resource existence with 404. Choose intentionally and consistently.

<!-- devrecall-section: key-takeaway -->
Authentication establishes identity; authorization checks an action on a resource. A valid identity can still lack access. Enforce permissions and ownership on the server, not only in navigation or UI controls.
