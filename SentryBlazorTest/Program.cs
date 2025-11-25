using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Sentry.OpenTelemetry;
using SentryBlazorTest.Components;
using SentryBlazorTest.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

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

builder.WebHost.UseSentry(options =>
{
    options.Dsn = builder.Configuration["Sentry:DSN"];
    options.TracesSampleRate = 1.0;
    options.AddEventProcessor(new BlazorEventProcessor());
    options.UseOpenTelemetry();
    options.Debug = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
});


builder.Services.AddSingleton<BlazorSentryIntegration>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<BlazorSentryIntegration>());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();