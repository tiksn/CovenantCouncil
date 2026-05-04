namespace CovenantCouncil.UseCases.Licenses;

public interface ILicenseService
{
  Task<IReadOnlyList<LicenseSummary>> ListAsync(string? descriptorDiscriminator, CancellationToken cancellationToken = default);

  Task<Guid> IssueAsync(IssueLicenseRequest request, CancellationToken cancellationToken = default);

  Task ExportAsync(Guid id, string licensePath, CancellationToken cancellationToken = default);

  Task ImportAsync(string licensePath, CancellationToken cancellationToken = default);

  Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
