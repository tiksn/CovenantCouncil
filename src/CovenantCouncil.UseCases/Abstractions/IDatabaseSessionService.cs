namespace CovenantCouncil.UseCases.Abstractions;

public interface IDatabaseSessionService
{
  DatabaseSession Current { get; }

  Task CreateAsync(string databasePath, string password, CancellationToken cancellationToken = default);

  Task OpenAsync(string databasePath, string password, CancellationToken cancellationToken = default);

  Task OpenOrCreateAsync(string databasePath, string password, CancellationToken cancellationToken = default);

  Task ChangePasswordAsync(string oldPassword, string newPassword, CancellationToken cancellationToken = default);
}
