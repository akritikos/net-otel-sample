using Kritikos.OTEL.WebSample;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

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
