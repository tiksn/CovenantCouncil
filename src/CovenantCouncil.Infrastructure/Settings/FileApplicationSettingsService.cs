using System.Text.Json;
using CovenantCouncil.UseCases.Settings;

namespace CovenantCouncil.Infrastructure.Settings;

public sealed class FileApplicationSettingsService : IApplicationSettingsService
{
  private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { WriteIndented = true };
  private readonly string settingsPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "CovenantCouncil",
    "settings.json");

  public async Task<ApplicationSettings> GetAsync(CancellationToken cancellationToken = default)
  {
    if (!File.Exists(settingsPath))
    {
      return new ApplicationSettings(null, []);
    }

    await using var stream = new FileStream(settingsPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
    return await JsonSerializer.DeserializeAsync<ApplicationSettings>(stream, Options, cancellationToken)
      ?? new ApplicationSettings(null, []);
  }

  public async Task SaveAsync(ApplicationSettings settings, CancellationToken cancellationToken = default)
  {
    Directory.CreateDirectory(Path.GetDirectoryName(settingsPath) ?? ".");
    await using var stream = new FileStream(settingsPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true);
    await JsonSerializer.SerializeAsync(stream, settings with
    {
      RecentDatabasePaths = settings.RecentDatabasePaths
        .Where(path => !string.IsNullOrWhiteSpace(path))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Take(5)
        .ToArray()
    }, Options, cancellationToken);
  }
}
