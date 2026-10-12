<!-- devrecall-section: one-boundary-several-providers -->
Configuration combines values from providers. For a duplicate key, a provider added later takes precedence. In a typical default ASP.NET Core setup, an environment variable can override a JSON setting; Email__Host represents Email:Host. Check the actual provider order rather than assuming a file always wins. Credentials belong in an appropriate secret source, not committed examples.

<!-- devrecall-section: name-the-dependency -->
Reading Email:Host string keys throughout application code spreads knowledge of storage format into consumers. Group related settings into EmailOptions at the composition boundary. Consumers then request the values they need, and binding, validation and tests have one clear entry point. Typed objects still need validation: an empty host is not made correct by being a string property.

<!-- devrecall-section: bind-validate-consume -->
```csharp
public sealed class EmailOptions
{
    public string Host { get; set; } = "";
    public int Port { get; set; }
}

builder.Services.AddOptions<EmailOptions>()
    .Bind(builder.Configuration.GetSection("Email"))
    .Validate(value => !string.IsNullOrWhiteSpace(value.Host)
        && value.Port is > 0 and <= 65535, "Email host/port is invalid")
    .ValidateOnStart();

public sealed class EmailSender(IOptions<EmailOptions> options)
{
    private readonly EmailOptions _settings = options.Value;
}
```

This is a binding/consumer fragment, not an email implementation. Validation catches bad settings before an actual send.

<!-- devrecall-section: choose-how-values-refresh -->
IOptions provides a cached value and does not pick up configuration changes after startup. IOptionsSnapshot is scoped: values are computed when accessed and cached for that scope, usually a request. Later scopes can see updated configuration if the provider supports reload. Do not inject a scoped snapshot into a singleton. IOptionsMonitor can provide current values/change notifications to long-lived consumers; its details are outside this foundation lesson.

<!-- devrecall-section: key-takeaway -->
Treat configuration as an application boundary. Bind related values to typed, validated options instead of scattering string keys. Choose cached or per-scope settings deliberately; provider reload support determines whether new values are available.
