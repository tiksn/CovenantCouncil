using System.Security.Cryptography;
using System.Text;
using CovenantCouncil.Infrastructure.Data;
using CovenantCouncil.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace CovenantCouncil.Infrastructure.Security;

public sealed class PortableProtectionService(IDbContextFactory<CovenantCouncilDbContext> dbContextFactory) : IPortableProtectionService
{
  private const int DefaultIterations = 310_000;
  private const int DefaultKeyLength = 32;
  private const string ApplicationName = "CovenantCouncil";
  private const int NonceLength = 12;
  private const int TagLength = 16;
  private byte[]? key;

  public bool IsUnlocked => key is not null;

  public byte[] Protect(byte[] plaintext)
  {
    var currentKey = key ?? throw new InvalidOperationException("Database protection is locked.");
    var nonce = RandomNumberGenerator.GetBytes(NonceLength);
    var ciphertext = new byte[plaintext.Length];
    var tag = new byte[TagLength];
    using var aes = new AesGcm(currentKey, TagLength);
    aes.Encrypt(nonce, plaintext, ciphertext, tag);
    return [.. nonce, .. tag, .. ciphertext];
  }

  public byte[] Unprotect(byte[] protectedPayload)
  {
    var currentKey = key ?? throw new InvalidOperationException("Database protection is locked.");
    if (protectedPayload.Length < NonceLength + TagLength)
    {
      throw new CryptographicException("Protected payload is invalid.");
    }

    var nonce = protectedPayload[..NonceLength];
    var tag = protectedPayload[NonceLength..(NonceLength + TagLength)];
    var ciphertext = protectedPayload[(NonceLength + TagLength)..];
    var plaintext = new byte[ciphertext.Length];
    using var aes = new AesGcm(currentKey, TagLength);
    aes.Decrypt(nonce, ciphertext, tag, plaintext);
    return plaintext;
  }

  public async Task InitializeNewAsync(string databasePath, string password, CancellationToken cancellationToken = default)
  {
    var salt = RandomNumberGenerator.GetBytes(32);
    key = DeriveKey(password, salt, DefaultIterations, DefaultKeyLength);

    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var existing = await db.ProtectionMetadata.SingleOrDefaultAsync(x => x.Id == 1, cancellationToken);
    if (existing is not null)
    {
      throw new InvalidOperationException("The selected database already has protection metadata.");
    }

    var now = DateTimeOffset.UtcNow;
    db.ProtectionMetadata.Add(new ProtectionMetadataEntity
    {
      Id = 1,
      KdfAlgorithm = "PBKDF2-HMACSHA256",
      ProtectedKdfIterations = ProtectMetadata(password, Encoding.UTF8.GetBytes(DefaultIterations.ToString())),
      ProtectedKdfSalt = ProtectMetadata(password, salt),
      ProtectedKeyLength = ProtectMetadata(password, Encoding.UTF8.GetBytes(DefaultKeyLength.ToString())),
      ProtectedKeyRingXml = ProtectMetadata(password, Encoding.UTF8.GetBytes("<keyRing />")),
      CreatedUtc = now,
      UpdatedUtc = now
    });
    await db.SaveChangesAsync(cancellationToken);
  }

  public async Task UnlockAsync(string databasePath, string password, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var metadata = await db.ProtectionMetadata.AsNoTracking().SingleOrDefaultAsync(x => x.Id == 1, cancellationToken)
      ?? throw new InvalidOperationException("The selected database has not been initialized.");

    var salt = UnprotectMetadata(password, metadata.ProtectedKdfSalt);
    var iterations = int.Parse(Encoding.UTF8.GetString(UnprotectMetadata(password, metadata.ProtectedKdfIterations)), System.Globalization.CultureInfo.InvariantCulture);
    var keyLength = int.Parse(Encoding.UTF8.GetString(UnprotectMetadata(password, metadata.ProtectedKeyLength)), System.Globalization.CultureInfo.InvariantCulture);
    key = DeriveKey(password, salt, iterations, keyLength);
    _ = UnprotectMetadata(password, metadata.ProtectedKeyRingXml);
  }

  public async Task ChangePasswordAsync(string databasePath, string oldPassword, string newPassword, Func<Task> reencrypt, CancellationToken cancellationToken = default)
  {
    await UnlockAsync(databasePath, oldPassword, cancellationToken);
    await reencrypt();

    var salt = RandomNumberGenerator.GetBytes(32);
    key = DeriveKey(newPassword, salt, DefaultIterations, DefaultKeyLength);

    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var metadata = await db.ProtectionMetadata.SingleAsync(x => x.Id == 1, cancellationToken);
    metadata.ProtectedKdfIterations = ProtectMetadata(newPassword, Encoding.UTF8.GetBytes(DefaultIterations.ToString()));
    metadata.ProtectedKdfSalt = ProtectMetadata(newPassword, salt);
    metadata.ProtectedKeyLength = ProtectMetadata(newPassword, Encoding.UTF8.GetBytes(DefaultKeyLength.ToString()));
    metadata.ProtectedKeyRingXml = ProtectMetadata(newPassword, Encoding.UTF8.GetBytes("<keyRing />"));
    metadata.UpdatedUtc = DateTimeOffset.UtcNow;
    await db.SaveChangesAsync(cancellationToken);
  }

  private static byte[] DeriveKey(string password, byte[] salt, int iterations, int keyLength)
  {
    return Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, keyLength);
  }

  private static byte[] DeriveMetadataKey(string password)
  {
    return DeriveKey(password, Encoding.UTF8.GetBytes(ApplicationName), 210_000, DefaultKeyLength);
  }

  private static byte[] ProtectMetadata(string password, byte[] plaintext)
  {
    using var aes = new AesGcm(DeriveMetadataKey(password), TagLength);
    var nonce = RandomNumberGenerator.GetBytes(NonceLength);
    var ciphertext = new byte[plaintext.Length];
    var tag = new byte[TagLength];
    aes.Encrypt(nonce, plaintext, ciphertext, tag);
    return [.. nonce, .. tag, .. ciphertext];
  }

  private static byte[] UnprotectMetadata(string password, byte[] payload)
  {
    using var aes = new AesGcm(DeriveMetadataKey(password), TagLength);
    var nonce = payload[..NonceLength];
    var tag = payload[NonceLength..(NonceLength + TagLength)];
    var ciphertext = payload[(NonceLength + TagLength)..];
    var plaintext = new byte[ciphertext.Length];
    aes.Decrypt(nonce, ciphertext, tag, plaintext);
    return plaintext;
  }
}
