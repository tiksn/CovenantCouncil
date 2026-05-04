namespace CovenantCouncil.UseCases.Licenses;

public sealed record LicenseDescriptorSummary(
  string Discriminator,
  string Name);

public sealed record LicenseSummary(
  Guid Id,
  string DescriptorDiscriminator,
  string DescriptorName,
  Guid PartyId,
  string PartyDisplayName,
  string SigningCertificateThumbprint,
  DateTimeOffset IssuedUtc);

public sealed record IssueLicenseRequest(
  string DescriptorDiscriminator,
  Guid PartyId,
  string PfxPath,
  string PfxPassword,
  IReadOnlyDictionary<string, string> EntitlementValues);
