using System.Diagnostics;

using Kritikos.OTEL.WebSample;

using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

var serviceName = builder.Environment.ApplicationName;
var serviceVersion = typeof(Program).Assembly.GetName().Version?.ToString() ?? "0.0.0";
using var activitySource = new ActivitySource(serviceName);

builder.Services.AddOpenApi();
builder.Services.AddHttpClient();

builder.Services.AddOpenTelemetry()
  .ConfigureResource(resource => resource
    .AddService(serviceName, serviceVersion: serviceVersion)
    .AddAttributes([
      new KeyValuePair<string, object>("deployment.environment", builder.Environment.EnvironmentName),
    ]))
  .WithTracing(trace => trace
    .AddSource(serviceName)
    .AddAspNetCoreInstrumentation()
    .AddHttpClientInstrumentation()
    .AddOtlpExporter())
  .WithMetrics(metric => metric
    .SetExemplarFilter(ExemplarFilterType.TraceBased)
    .AddAspNetCoreInstrumentation()
    .AddHttpClientInstrumentation()
    .AddRuntimeInstrumentation()
    .AddOtlpExporter());

builder.Logging.AddOpenTelemetry(options =>
{
  options.IncludeFormattedMessage = true;
  options.IncludeScopes = true;
  options.AddOtlpExporter();
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
  app.MapOpenApi();
}

app.UseHttpsRedirection();

var summaries = new[]
{
  "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching",
};

app.MapGet("/weatherforecast", () =>
  {
    var forecast = Enumerable.Range(1, 5)
      .Select(index =>
        new WeatherForecast(
          DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
#pragma warning disable CA5394
          Random.Shared.Next(-20, 55),
          summaries[Random.Shared.Next(summaries.Length)]))
#pragma warning restore CA5394
      .ToArray();
    return forecast;
  })
  .WithName("GetWeatherForecast");

app.Run();
