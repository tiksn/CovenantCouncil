namespace CovenantCouncil.Infrastructure.Data.Entities;

public sealed class CertificateEntity
{
  public Guid Id { get; set; }

  public string Thumbprint { get; set; } = "";

  public string Subject { get; set; } = "";

  public string Issuer { get; set; } = "";

  public string SerialNumber { get; set; } = "";

  public DateTimeOffset NotBefore { get; set; }

  public DateTimeOffset NotAfter { get; set; }

  public string? ParentThumbprint { get; set; }

  public byte[] RawDer { get; set; } = [];

  public DateTimeOffset ImportedUtc { get; set; }
}
