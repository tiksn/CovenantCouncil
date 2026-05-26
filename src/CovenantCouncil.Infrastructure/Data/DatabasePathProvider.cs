namespace CovenantCouncil.Infrastructure.Data;

public sealed class DatabasePathProvider : IDatabasePathProvider
{
  public string? DatabasePath { get; private set; }

  public void SetDatabasePath(string databasePath)
  {
    if (Path.GetExtension(databasePath) is not ".ccdb")
    {
      throw new ArgumentException("Covenant Council databases must use the .ccdb extension.", nameof(databasePath));
    }

    DatabasePath = databasePath;
  }
}
