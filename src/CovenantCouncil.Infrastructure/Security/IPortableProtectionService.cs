namespace CovenantCouncil.Infrastructure.Security;

public interface IPortableProtectionService
{
  bool IsUnlocked { get; }

  byte[] Protect(byte[] plaintext);

  byte[] Unprotect(byte[] protectedPayload);

  Task InitializeNewAsync(string databasePath, string password, CancellationToken cancellationToken = default);

  Task UnlockAsync(string databasePath, string password, CancellationToken cancellationToken = default);

  Task ChangePasswordAsync(string databasePath, string oldPassword, string newPassword, Func<Task> reencrypt, CancellationToken cancellationToken = default);
}
