namespace CovenantCouncil.UseCases.Settings;

public sealed record ApplicationSettings(
  string? OtlpEndpoint,
  IReadOnlyList<string> RecentDatabasePaths);
