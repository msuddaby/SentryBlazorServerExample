# Improved Sentry logging for ASP.NET Core Blazor

Blazor Server's SignalR-based rendering architecture presents unique challenges for error tracking and debugging. This integration leverages Microsoft's new activity tracing capabilities [(introduced in ASP.NET Core 10.0)](https://learn.microsoft.com/en-us/aspnet/core/blazor/performance/?view=aspnetcore-10.0) to provide comprehensive automatic breadcrumbs and enriched error context in Sentry.

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

To capture Blazor-specific traces with full context, OpenTelemetry and Sentry must be configured together. Without this integration, activities like Microsoft.AspNetCore.Components.Navigate will lack meaningful tags and metadata.

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


## Screenshots
<img width="501" height="92" alt="Screenshot_20251124_170515" src="https://github.com/user-attachments/assets/f6abe74f-8128-4f9d-a236-ef523d0ccd5f" />
<img width="681" height="846" alt="Screenshot_20251124_170559" src="https://github.com/user-attachments/assets/8f5d2243-bcd3-411d-a1c3-8d1ff0d0503b" />
<img width="681" height="604" alt="Screenshot_20251124_170611" src="https://github.com/user-attachments/assets/17708cb8-a698-4b06-87cd-5c58ea4cfbc2" />


