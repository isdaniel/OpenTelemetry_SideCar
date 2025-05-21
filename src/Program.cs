using System.Diagnostics.Metrics;
using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient();
// Custom metrics for the application
var greeterMeter = new Meter("OTLP.Example", "1.0.0");
var countGreetings = greeterMeter.CreateCounter<int>("greetings.count", description: "Counts the number of greetings");

// Custom ActivitySource for the application
var greeterActivitySource = new ActivitySource("OTLP.Example");

var otel = builder.Services.AddOpenTelemetry();

// Configure OpenTelemetry Resources with the application name
otel.ConfigureResource(resource => resource
    .AddService(serviceName: builder.Environment.ApplicationName));

// Add Metrics for ASP.NET Core and our custom metrics and export to Prometheus
otel.WithMetrics(metricsProviderBuilder =>
{
    metricsProviderBuilder
        .AddMeter(greeterMeter.Name)
        .AddMeter("Microsoft.AspNetCore.Hosting")
        .AddMeter("Microsoft.AspNetCore.Server.Kestrel")
        .AddMeter("System.Net.Http")
        .AddMeter("System.Net.NameResolution")
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter();
});


otel.WithTracing(tracing =>
{
    tracing.AddAspNetCoreInstrumentation();
    tracing.AddHttpClientInstrumentation();
    tracing.AddSource(greeterActivitySource.Name);
    tracing.AddOtlpExporter();
});


var app = builder.Build();

app.MapGet("/", SendGreeting);
app.MapGet("/NestedGreeting", SendNestedGreeting);
app.Run();

async Task<string> SendGreeting(ILogger<Program> logger)
{
    // Create a new Activity scoped to the method
    using var activity = greeterActivitySource.StartActivity("GreeterActivity");

    // Log a message
    logger.LogInformation("Sending greeting");

    // Increment the custom counter
    countGreetings.Add(1);

    // Add a tag to the Activity
    activity?.SetTag("greeting", "Hello World!");

    return "Hello World!";
}

async Task SendNestedGreeting(int nestlevel, ILogger<Program> logger, HttpContext context, IHttpClientFactory clientFactory)
{
    // Create a new Activity scoped to the method
    using var activity = greeterActivitySource.StartActivity("GreeterActivity");

    if (nestlevel <= 5)
    {
        // Log a message
        logger.LogInformation("Sending greeting, level {nestlevel}", nestlevel);

        // Increment the custom counter
        countGreetings.Add(1);

        // Add a tag to the Activity
        activity?.SetTag("nest-level", nestlevel);

        await context.Response.WriteAsync($"Nested Greeting, level: {nestlevel}\r\n");

        if (nestlevel > 0)
        {
            var request = context.Request;
            var url = new Uri($"{request.Scheme}://{request.Host}{request.Path}?nestlevel={nestlevel - 1}");

            // Makes an http call passing the activity information as http headers
            var nestedResult = await clientFactory.CreateClient().GetStringAsync(url);
            await context.Response.WriteAsync(nestedResult);
        }
    }
    else
    {
        // Log a message
        logger.LogError("Greeting nest level {nestlevel} too high", nestlevel);
        await context.Response.WriteAsync("Nest level too high, max is 5");
    }
}
