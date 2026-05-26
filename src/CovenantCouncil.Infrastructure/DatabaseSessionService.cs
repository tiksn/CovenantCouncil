using CovenantCouncil.Infrastructure.Data;
using CovenantCouncil.Infrastructure.Security;
using CovenantCouncil.UseCases.Abstractions;

namespace CovenantCouncil.Infrastructure;

public sealed class DatabaseSessionService(
  IDatabasePathProvider databasePathProvider,
  IPortableProtectionService protectionService,
  IRecentDatabaseService recentDatabaseService) : IDatabaseSessionService
{
  public DatabaseSession Current { get; private set; } = new("", false);

  public async Task CreateAsync(string databasePath, string password, CancellationToken cancellationToken = default)
  {
    if (await SqliteSchemaInitializer.HasProtectionMetadataAsync(databasePath, cancellationToken))
    {
      throw new InvalidOperationException("The selected database is already initialized. Use Open instead.");
    }

    if (await SqliteSchemaInitializer.HasApplicationDataAsync(databasePath, cancellationToken))
    {
      throw new InvalidOperationException("The selected database contains application data but has no protection metadata. Create a new database file or open a valid Covenant Council database.");
    }

    databasePathProvider.SetDatabasePath(databasePath);
    await SqliteSchemaInitializer.InitializeAsync(databasePath, cancellationToken);
    await protectionService.InitializeNewAsync(databasePath, password, cancellationToken);
    await recentDatabaseService.RememberAsync(databasePath, cancellationToken);
    Current = new(databasePath, true);
  }

  public async Task OpenAsync(string databasePath, string password, CancellationToken cancellationToken = default)
  {
    if (!await SqliteSchemaInitializer.HasProtectionMetadataAsync(databasePath, cancellationToken))
    {
      throw new InvalidOperationException("The selected database has not been initialized. Use Create to initialize this database file.");
    }

    databasePathProvider.SetDatabasePath(databasePath);
    await SqliteSchemaInitializer.InitializeAsync(databasePath, cancellationToken);
    await protectionService.UnlockAsync(databasePath, password, cancellationToken);
    await recentDatabaseService.RememberAsync(databasePath, cancellationToken);
    Current = new(databasePath, true);
  }

  public async Task OpenOrCreateAsync(string databasePath, string password, CancellationToken cancellationToken = default)
  {
    if (await SqliteSchemaInitializer.HasProtectionMetadataAsync(databasePath, cancellationToken))
    {
      await OpenAsync(databasePath, password, cancellationToken);
      return;
    }

    await CreateAsync(databasePath, password, cancellationToken);
  }

  public async Task ChangePasswordAsync(string oldPassword, string newPassword, CancellationToken cancellationToken = default)
  {
    if (!Current.IsOpen)
    {
      throw new InvalidOperationException("Open a database before changing the password.");
    }

    await protectionService.ChangePasswordAsync(Current.DatabasePath, oldPassword, newPassword, () => Task.CompletedTask, cancellationToken);
  }
}
