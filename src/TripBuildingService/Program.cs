using Marten;
using Marten.Events.Daemon.Resiliency;
using Marten.Events.Projections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

var builder = Host.CreateApplicationBuilder();

builder.Logging.AddOpenTelemetry(logging =>
{
    logging.IncludeFormattedMessage = true;
    logging.IncludeScopes = true;
});

builder.Services.AddOpenTelemetry().UseOtlpExporter();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing.AddSource("Marten");
        tracing.AddZipkinExporter();
    })
    .WithMetrics(metrics =>
    {
        metrics.AddMeter("Marten");
        metrics.AddOtlpExporter(opts =>
        {
            opts.Endpoint = new Uri("http://localhost:9090/api/v1/otlp/v1/metrics");
            opts.Protocol = OtlpExportProtocol.HttpProtobuf;
        });
    });

builder.Services.AddMarten(opts =>
{
    opts.DatabaseSchemaName = "cli";

    // Register all event store projections ahead of time
    opts.Projections
        .Add(new TripProjection(), ProjectionLifecycle.Async);

    opts.Projections
        .Add(new DayProjection(), ProjectionLifecycle.Async);

    opts.Projections
        .Add(new DistanceProjection(), ProjectionLifecycle.Async);
}).AddAsyncDaemon(DaemonMode.Solo)
    
    // Use PostgreSQL data source from the IoC container
    .UseNpgsqlDataSource();

await builder.Build().RunAsync();