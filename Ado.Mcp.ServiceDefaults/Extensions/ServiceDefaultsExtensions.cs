using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

namespace Ado.Mcp.ServiceDefaults.Extensions;

public static class ServiceDefaultsExtensions
{
    public static WebApplicationBuilder AddServiceDefaults(this WebApplicationBuilder builder)
    {
        // Health checks
        builder.Services.AddHealthChecks();

        // OpenTelemetry (basic tracing + metrics) with console export by default
        var serviceName = builder.Environment.ApplicationName ?? "Ado.Mcp";
        builder.Services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(serviceName))
            .WithTracing(tracer => tracer
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddSource(serviceName)
                .AddOtlpExporterIfConfigured()
                .AddConsoleExporter())
            .WithMetrics(meter => meter
                .AddRuntimeInstrumentation()
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddOtlpExporterIfConfigured()
                .AddConsoleExporter());
        return builder;
    }

    public static WebApplication UseServiceDefaults(this WebApplication app)
    {
        // Health endpoint: expose liveness/readiness
        app.MapHealthChecks("/healthz");
        app.MapHealthChecks("/readyz");
        return app;
    }
}

internal static class OtelExtensions
{
    public static TracerProviderBuilder AddOtlpExporterIfConfigured(this TracerProviderBuilder builder)
    {
        var endpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
        return string.IsNullOrWhiteSpace(endpoint) ? builder : builder.AddOtlpExporter();
    }

    public static MeterProviderBuilder AddOtlpExporterIfConfigured(this MeterProviderBuilder builder)
    {
        var endpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
        return string.IsNullOrWhiteSpace(endpoint) ? builder : builder.AddOtlpExporter();
    }
}
