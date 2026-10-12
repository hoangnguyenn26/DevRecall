<!-- devrecall-section: transient-scoped-and-singleton -->
Transient creates a new instance when resolved. Scoped reuses one instance within a request scope. Singleton reuses one instance for the application's lifetime.

<!-- devrecall-section: registration -->
```csharp
services.AddTransient<IFormatter, Formatter>();
services.AddScoped<IOrderService, OrderService>();
services.AddSingleton<ISystemClock, SystemClock>();
```

<!-- devrecall-section: lifetime-mismatch -->
A singleton must not capture a scoped dependency. Doing so extends the scoped object's effective lifetime and can leak request-specific state.

<!-- devrecall-section: key-takeaway -->
Transient creates a new instance each time it is resolved, Scoped reuses one instance within a request scope, and Singleton reuses one instance for the application lifetime. Choose the shortest lifetime that fits the required state sharing, and never let a Singleton capture a Scoped dependency.
