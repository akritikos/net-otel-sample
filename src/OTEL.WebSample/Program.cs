#pragma warning disable CA5394
using System.Diagnostics;

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
  options.SetResourceBuilder(
    ResourceBuilder.CreateDefault()
      .AddService(serviceName, serviceVersion: serviceVersion));
  options.IncludeFormattedMessage = true;
  options.IncludeScopes = true;
  options.ParseStateValues = true;
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

var cities = new[]
{
  "London", "Paris", "Tokyo", "New York", "Sydney", "Berlin", "Mumbai", "Cairo", "São Paulo", "Toronto",
};
var conditions = new[] { "Sunny", "Cloudy", "Rainy", "Stormy", "Snowy", "Windy", "Foggy", "Clear" };

app.MapOpenApi();

app.MapGet("/weatherforecast", (ILogger<Program> logger) =>
{
  logger.LogInformation("Generating weather forecast for all cities");
  var forecasts = Enumerable.Range(0, 5)
    .Select(_ =>
    {
      var city = cities[Random.Shared.Next(cities.Length)];
      return new
      {
        City = city,
        Date = DateOnly.FromDateTime(DateTime.Now.AddDays(Random.Shared.Next(7))),
        TemperatureC = Random.Shared.Next(-20, 45),
        Condition = conditions[Random.Shared.Next(conditions.Length)],
      };
    });
  return Results.Ok(forecasts);
});

app.MapGet("/weatherforecast/{city}", async (string city, HttpClient http, ILogger<Program> logger) =>
{
  using var activity = activitySource.StartActivity("GetCityWeather");
  activity?.SetTag("city", city);

  logger.LogInformation("Fetching weather for {City}", city);
  await Task.Delay(Random.Shared.Next(10, 100));

  // Internal call to simulate distributed tracing
  try
  {
    await http.GetAsync($"http://localhost:8080/internal/lookup?city={Uri.EscapeDataString(city)}");
  }
  catch (Exception ex)
  {
    logger.LogWarning(ex, "Internal lookup failed for {City}", city);
  }

  var forecast = new
  {
    City = city,
    Date = DateOnly.FromDateTime(DateTime.Now),
    TemperatureC = Random.Shared.Next(-20, 45),
    Condition = conditions[Random.Shared.Next(conditions.Length)],
  };

  logger.LogInformation("Weather for {City}: {Temp}°C, {Condition}", city, forecast.TemperatureC, forecast.Condition);
  return Results.Ok(forecast);
});

app.MapGet("/internal/lookup", async (string? city, ILogger<Program> logger) =>
{
  using var activity = activitySource.StartActivity("InternalLookup");
  logger.LogDebug("Internal lookup for {City}", city);
  await Task.Delay(Random.Shared.Next(5, 50));
  return Results.Ok(new { Source = "cache", City = city });
});

app.MapGet("/chain", async (HttpClient http, ILogger<Program> logger) =>
{
  using var activity = activitySource.StartActivity("ChainRequest");
  logger.LogInformation("Starting chain request");

  var city = cities[Random.Shared.Next(cities.Length)];
  var response = await http.GetStringAsync($"http://localhost:8080/weatherforecast/{Uri.EscapeDataString(city)}");

  logger.LogInformation("Chain completed for {City}", city);
  return Results.Ok(new { Chain = true, City = city, Result = response });
});

app.MapGet("/slow", async (ILogger<Program> logger) =>
{
  using var activity = activitySource.StartActivity("SlowOperation");
  var delay = Random.Shared.Next(500, 3000);
  activity?.SetTag("delay_ms", delay);

  logger.LogWarning("Slow endpoint hit, delay: {Delay}ms", delay);
  await Task.Delay(delay);

  return Results.Ok(new { Slow = true, DelayMs = delay });
});

app.MapGet("/error", (ILogger<Program> logger) =>
{
  var roll = Random.Shared.Next(3);
  if (roll == 0)
  {
    logger.LogError("Simulated server error");
    return Results.StatusCode(500);
  }

  if (roll == 1)
  {
    logger.LogWarning("Simulated not found");
    return Results.NotFound();
  }

  logger.LogInformation("Error endpoint - no error this time");
  return Results.Ok(new { Error = false });
});

app.MapGet("/health", () => Results.Ok("healthy"));

await app.RunAsync();
