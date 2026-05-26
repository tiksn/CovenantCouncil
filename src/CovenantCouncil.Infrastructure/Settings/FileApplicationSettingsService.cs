using System.Text.Json;
using CovenantCouncil.UseCases.Settings;

namespace CovenantCouncil.Infrastructure.Settings;

public sealed class FileApplicationSettingsService : IApplicationSettingsService
{
  private static readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web) { WriteIndented = true };
  private readonly string _settingsPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "CovenantCouncil",
    "settings.json");

  public async Task<ApplicationSettings> GetAsync(CancellationToken cancellationToken = default)
  {
    if (!File.Exists(_settingsPath))
    {
      return new ApplicationSettings(null, []);
    }

    await using var stream = new FileStream(_settingsPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
    return await JsonSerializer.DeserializeAsync<ApplicationSettings>(stream, _options, cancellationToken)
      ?? new ApplicationSettings(null, []);
  }

  public async Task SaveAsync(ApplicationSettings settings, CancellationToken cancellationToken = default)
  {
    Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath) ?? ".");
    await using var stream = new FileStream(_settingsPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true);
    await JsonSerializer.SerializeAsync(stream, settings with
    {
      RecentDatabasePaths = [.. settings.RecentDatabasePaths
        .Where(path => !string.IsNullOrWhiteSpace(path))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Take(5)]
    }, _options, cancellationToken);
  }
}
