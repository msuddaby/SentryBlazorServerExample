# Improved Sentry logging for ASP.NET Core Blazor

Blazor unconventionally uses SignalR to render pages on Blazor, making error tracing a bit difficult.
Recently, Microsoft added new activity tracing capabilities ([source](https://learn.microsoft.com/en-us/aspnet/core/blazor/performance/?view=aspnetcore-10.0)) to Blazor, which makes it a lot easier to add automatic breadcrumbs.

## Setup
The following packages are required:
```
Sentry
Sentry.AspNetCore
Sentry.OpenTelemetry
OpenTelemetry
OpenTelemetry.Extensions.Hosting
OpenTelemetry.Instrumentation.AspNetCore
```

In order to get the new traces, we must set up OpenTelemetry and Sentry together. Otherwise, actions like `Microsoft.AspNetCore.Components.Navigate` will have no tags, thus no useful information.

### Add the following to `Program.cs`

First, we set up OpenTelemetry

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing.AddSource("Microsoft.AspNetCore.Components");
        tracing.AddSource("Microsoft.AspNetCore.Components.Server.Circuits");
        tracing.AddAspNetCoreInstrumentation();
        
        // Add Sentry as an exporter
        tracing.AddSentry();
    })
    .WithMetrics(metrics =>
    {
        metrics.AddMeter("Microsoft.AspNetCore.Components");
        metrics.AddMeter("Microsoft.AspNetCore.Components.Lifecycle");
        metrics.AddMeter("Microsoft.AspNetCore.Components.Server.Circuits");
        metrics.AddAspNetCoreInstrumentation();
    });
```

Next, we set up Sentry:

```csharp
builder.WebHost.UseSentry(options =>
{
    options.Dsn = "";
    options.TracesSampleRate = 1.0;
    // Services/BlazorEventProcessor.cs
    options.AddEventProcessor(new BlazorEventProcessor());
    // Required
    options.UseOpenTelemetry();
    options.Debug = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
});
});
```

And finally, add the hosted service for enriching our events:
```csharp
// Services/BlazorSentryIntegration.cs
builder.Services.AddSingleton<BlazorSentryIntegration>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<BlazorSentryIntegration>());
```

## Changes

- The Sentry issue feed will display the page name beside the issue ID (instead of GET /_blazor)
- Breadcrumbs will now track UI clicks and navigation
- UI clicks show which method was executed
- Tags now include the Blazor circuit id, the component, and the route


## Todo
I'd like to experiment and see if replay could be done via the JavaScript SDK + Blazor's JS interop