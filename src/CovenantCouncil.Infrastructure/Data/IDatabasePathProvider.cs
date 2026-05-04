namespace CovenantCouncil.Infrastructure.Data;

public interface IDatabasePathProvider
{
  string? DatabasePath { get; }

  void SetDatabasePath(string databasePath);
}
