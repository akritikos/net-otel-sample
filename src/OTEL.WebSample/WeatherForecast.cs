namespace Kritikos.OTEL.WebSample;

/// <summary>
/// Represents a weather forecast entry.
/// </summary>
/// <param name="Date">The forecast date.</param>
/// <param name="TemperatureC">The temperature in degrees Celsius.</param>
/// <param name="Summary">An optional short weather description.</param>
internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
  /// <summary>
  /// Gets the temperature in degrees Fahrenheit.
  /// </summary>
  public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
