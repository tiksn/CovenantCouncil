namespace CovenantCouncil.UseCases.Licenses;

public interface ILicenseCatalog
{
  IReadOnlyList<LicenseDescriptorSummary> GetDescriptors();
}
