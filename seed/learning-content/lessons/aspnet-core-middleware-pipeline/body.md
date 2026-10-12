<!-- devrecall-section: a-pipeline-of-decisions -->
An HTTP request passes through ordered components rather than one giant handler. Each component can inspect or change request state, invoke the next component, and then do work after it returns. Think A before → B before → endpoint → B after → A after for a successful request. Code after next is not guaranteed to run if downstream throws; use try/finally when cleanup must occur.

<!-- devrecall-section: what-order-means -->
Upstream middleware decides what downstream components receive and whether they run at all. In an explicitly configured security pipeline, authentication establishes the user before authorization checks permissions. An exception handler must wrap the components whose exceptions it handles. UseRouting selects an endpoint; it is not by itself terminal middleware that prevents later static-file handling.

<!-- devrecall-section: control-flow-before-and-after-next -->
```csharp
app.Use(async (context, next) =>
{
    Console.WriteLine("A before");
    await next();
    Console.WriteLine("A after");
});
app.Use(async (context, next) =>
{
    Console.WriteLine("B before");
    await next();
    Console.WriteLine("B after");
});
app.Run(async context =>
{
    Console.WriteLine("Endpoint");
    await context.Response.WriteAsync("Hello");
});
```

This illustrative trace shows the nesting on a successful request. Run is terminal here: it produces the response without invoking another component.

<!-- devrecall-section: short-circuiting-and-ordering-mistakes -->
A component that returns without calling next skips the remaining pipeline. This can be intentional, such as serving a static file or rejecting a request, but an early terminal handler can also make later endpoints unreachable. For an explicit security setup, the mental order is routing → authentication → authorization → endpoint. The exact application setup matters; do not mistake a method-name checklist for the control-flow model.

<!-- devrecall-section: key-takeaway -->
Middleware forms an ordered, nested pipeline. Each component can run before and after next, or short-circuit downstream work. Order controls both the request state later components receive and which components execute.
