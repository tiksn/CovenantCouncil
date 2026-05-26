namespace CovenantCouncil.UseCases.Abstractions;

public interface IRecentDatabaseService
{
  Task<IReadOnlyList<string>> GetRecentAsync(CancellationToken cancellationToken = default);

  Task RememberAsync(string databasePath, CancellationToken cancellationToken = default);
}
