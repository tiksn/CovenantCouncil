using CovenantCouncil.UseCases.Licenses;

namespace CovenantCouncil.Infrastructure.Licenses;

public sealed class LicenseCatalog : ILicenseCatalog
{
  public IReadOnlyList<LicenseDescriptorSummary> GetDescriptors()
  {
    var descriptors = new LicenseDescriptorSummary[]
    {
      FromDescriptor(new Fossa.Licensing.CompanyLicenseDescriptor(), LicenseEntitlementKinds.FossaCompany),
      FromDescriptor(new Fossa.Licensing.SystemLicenseDescriptor(), LicenseEntitlementKinds.FossaSystem),
      FromDescriptor(new VerdantApp.Licensing.SystemLicenseDescriptor(), LicenseEntitlementKinds.VerdantSystem)
    };

    return descriptors
      .OrderBy(x => x.Name)
      .ToList();
  }

  private static LicenseDescriptorSummary FromDescriptor<TEntitlements>(
    TIKSN.Licensing.ILicenseDescriptor<TEntitlements> descriptor,
    string entitlementKind)
  {
    return new LicenseDescriptorSummary(
      descriptor.Discriminator.ToString(),
      descriptor.Name,
      entitlementKind);
  }
}
