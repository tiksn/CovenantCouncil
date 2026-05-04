namespace CovenantCouncil.Infrastructure.Data.Entities;

public sealed class LicenseEntity
{
  public Guid Id { get; set; }

  public string DescriptorDiscriminator { get; set; } = "";

  public string DescriptorName { get; set; } = "";

  public Guid PartyId { get; set; }

  public PartyEntity? Party { get; set; }

  public string SigningCertificateThumbprint { get; set; } = "";

  public byte[] ProtectedPayload { get; set; } = [];

  public DateTimeOffset IssuedUtc { get; set; }
}
