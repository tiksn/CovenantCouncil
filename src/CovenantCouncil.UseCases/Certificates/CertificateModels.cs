namespace CovenantCouncil.UseCases.Certificates;

public sealed record CertificateSummary(
  Guid Id,
  string Thumbprint,
  string Subject,
  string Issuer,
  string SerialNumber,
  DateTimeOffset NotBefore,
  DateTimeOffset NotAfter,
  string? ParentThumbprint);

public sealed record CertificateTreeNode(
  CertificateSummary Certificate,
  IReadOnlyList<CertificateTreeNode> Children);
