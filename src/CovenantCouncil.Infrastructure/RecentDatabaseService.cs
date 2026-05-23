using CovenantCouncil.UseCases.Abstractions;
using CovenantCouncil.UseCases.Settings;

namespace CovenantCouncil.Infrastructure;

public sealed class RecentDatabaseService(IApplicationSettingsService settingsService) : IRecentDatabaseService
{
  public async Task<IReadOnlyList<string>> GetRecentAsync(CancellationToken cancellationToken = default)
  {
    return (await settingsService.GetAsync(cancellationToken)).RecentDatabasePaths;
  }

  public async Task RememberAsync(string databasePath, CancellationToken cancellationToken = default)
  {
    var settings = await settingsService.GetAsync(cancellationToken);
    var recent = (await GetRecentAsync(cancellationToken))
      .Where(path => !StringComparer.OrdinalIgnoreCase.Equals(path, databasePath))
      .Prepend(databasePath)
      .Take(5)
      .ToArray();

    await settingsService.SaveAsync(settings with { RecentDatabasePaths = recent }, cancellationToken);
  }
}
