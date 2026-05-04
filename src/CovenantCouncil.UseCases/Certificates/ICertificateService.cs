namespace CovenantCouncil.UseCases.Certificates;

public interface ICertificateService
{
  Task<IReadOnlyList<CertificateSummary>> ListAsync(CancellationToken cancellationToken = default);

  Task<IReadOnlyList<CertificateTreeNode>> GetTreeAsync(CancellationToken cancellationToken = default);

  Task ImportPublicChainAsync(IReadOnlyList<string> certificatePaths, CancellationToken cancellationToken = default);

  Task ImportPublicCertificateFromPfxAsync(string pfxPath, string password, CancellationToken cancellationToken = default);

  Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
