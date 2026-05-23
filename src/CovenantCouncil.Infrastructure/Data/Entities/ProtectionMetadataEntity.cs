namespace CovenantCouncil.Infrastructure.Data.Entities;

public sealed class ProtectionMetadataEntity
{
  public int Id { get; set; }

  public string KdfAlgorithm { get; set; } = "PBKDF2-HMACSHA256";

  public byte[] ProtectedKdfIterations { get; set; } = [];

  public byte[] ProtectedKdfSalt { get; set; } = [];

  public byte[] ProtectedKeyLength { get; set; } = [];

  public byte[] ProtectedKeyRingXml { get; set; } = [];

  public DateTimeOffset CreatedUtc { get; set; }

  public DateTimeOffset UpdatedUtc { get; set; }
}
