using EventPublisher;
using Marten;
using Marten.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Trace;

var builder = Host.CreateApplicationBuilder();


builder.Logging.AddConsole();

builder.Logging.AddOpenTelemetry(logging =>
{
    logging.IncludeFormattedMessage = true;
    logging.IncludeScopes = true;
    
});


builder.Services.AddOpenTelemetry()
    .UseOtlpExporter()
    
    // Enable exports of Open Telemetry activities for Marten
    .WithTracing(tracing =>
    {
        tracing.AddSource("Marten");
        tracing.AddZipkinExporter();
    })

    // Enable exports of metrics for Marten
    .WithMetrics(metrics =>
    {
        metrics.AddMeter("Marten");
    });

builder.Services.AddHostedService<HostedPublisher>();

builder.Services.AddMarten(opts =>
    {
        opts.DatabaseSchemaName = "cli";

        // Turn on Otel tracing for connection activity, and
        // also tag events to each span for all the Marten "write"
        // operations
        opts.OpenTelemetry.TrackConnections = TrackLevel.Verbose;

        opts.OpenTelemetry.TrackEventCounters();
    });

builder.Build().Run();