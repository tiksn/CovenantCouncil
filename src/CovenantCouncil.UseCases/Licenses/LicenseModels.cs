namespace CovenantCouncil.UseCases.Licenses;

public sealed record LicenseDescriptorSummary(
  string Discriminator,
  string Name,
  string EntitlementKind);

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
  string SerialNumber,
  Guid LicensorPartyId,
  Guid LicenseePartyId,
  DateTimeOffset NotBeforeUtc,
  DateTimeOffset NotAfterUtc,
  string PfxPath,
  string PfxPassword,
  IReadOnlyDictionary<string, string> EntitlementValues);

public static class LicenseEntitlementKinds
{
  public const string FossaCompany = "fossa-company";

  public const string FossaSystem = "fossa-system";

  public const string VerdantSystem = "verdant-system";
}

public static class LicenseEntitlementFields
{
  public const string CompanyId = "CompanyId";

  public const string CountryCodes = "CountryCodes";

  public const string EnvironmentName = "EnvironmentName";

  public const string MaximumBranchCount = "MaximumBranchCount";

  public const string MaximumCompanyCount = "MaximumCompanyCount";

  public const string MaximumDepartmentCount = "MaximumDepartmentCount";

  public const string MaximumEmployeeCount = "MaximumEmployeeCount";

  public const string SystemId = "SystemId";
}
