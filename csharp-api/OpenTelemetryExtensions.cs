using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

public static class OpenTelemetryExtensions
{
    public static WebApplicationBuilder AddAppTelemetryV1(
        this WebApplicationBuilder builder,
        string serviceName
    )
    {
        // Configure OpenTelemetry Core
        builder
            .Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithMetrics(metrics =>
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddSqlClientInstrumentation()
                    .AddPrometheusExporter()
            )
            .WithTracing(tracing =>
                tracing
                    .AddAspNetCoreInstrumentation()
                    // FIX: Use the official enrichment hook instead of a global background listener
                    .AddSqlClientInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.Enrich = (activity, method, cmd) =>
                        {
                            // Safely grab the true HTTP route attribute from the active web execution layer
                            var httpRoute =
                                Activity.Current?.GetTagItem("http.route")?.ToString()
                                ?? "unknown-endpoint";

                            activity.SetTag("api.endpoint.route", httpRoute);
                        };
                    })
            );
        return builder;
    }

    public static WebApplicationBuilder AddAppTelemetryV2(
        this WebApplicationBuilder builder,
        string serviceName
    )
    {
        // 1. Define shared resource metadata
        var resourceBuilder = ResourceBuilder
            .CreateDefault()
            .AddService(serviceName, serviceVersion: "1.0.0")
            .AddAttributes(
                new Dictionary<string, object>
                {
                    ["deployment.environment"] = builder.Environment.EnvironmentName,
                }
            );

        // 2. Configure Tracing and Metrics
        builder
            .Services.AddOpenTelemetry()
            .WithTracing(tracing =>
                tracing
                    .SetResourceBuilder(resourceBuilder)
                    .AddAspNetCoreInstrumentation()
                    .AddSqlClientInstrumentation(options =>
                    {
                        options.SetDbStatementForText = true; // Captures raw SQL text
                        options.RecordException = true; // Captures SQL exceptions
                    })
                    .AddOtlpExporter()
            ) // Reads OTEL_EXPORTER_OTLP_ENDPOINT automatically
            .WithMetrics(metrics =>
                metrics
                    .SetResourceBuilder(resourceBuilder)
                    .AddAspNetCoreInstrumentation()
                    .AddSqlClientInstrumentation() // Captures connection pool metrics
                    .AddOtlpExporter()
            );

        // 3. Configure Logging
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.SetResourceBuilder(resourceBuilder);
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
            logging.AddOtlpExporter();
        });

        return builder;
    }
}
