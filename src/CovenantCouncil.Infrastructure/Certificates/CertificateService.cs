using System.Security.Cryptography.X509Certificates;
using CovenantCouncil.Infrastructure.Data;
using CovenantCouncil.Infrastructure.Data.Entities;
using CovenantCouncil.UseCases.Certificates;
using Microsoft.EntityFrameworkCore;

namespace CovenantCouncil.Infrastructure.Certificates;

public sealed class CertificateService(IDbContextFactory<CovenantCouncilDbContext> dbContextFactory) : ICertificateService
{
  public async Task<IReadOnlyList<CertificateSummary>> ListAsync(CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    return await db.Certificates.AsNoTracking()
      .OrderBy(c => c.Subject)
      .Select(c => ToSummary(c))
      .ToListAsync(cancellationToken);
  }

  public async Task<IReadOnlyList<CertificateTreeNode>> GetTreeAsync(CancellationToken cancellationToken = default)
  {
    var all = await ListAsync(cancellationToken);
    var children = all.GroupBy(c => c.ParentThumbprint ?? "").ToDictionary(g => g.Key, g => g.ToList());

    CertificateTreeNode Build(CertificateSummary certificate)
    {
      var descendants = children.GetValueOrDefault(certificate.Thumbprint) ?? [];
      return new CertificateTreeNode(certificate, descendants.Select(Build).ToList());
    }

    return all
      .Where(c => c.ParentThumbprint is null || !all.Any(parent => parent.Thumbprint == c.ParentThumbprint))
      .Select(Build)
      .ToList();
  }

  public async Task ImportPublicChainAsync(IReadOnlyList<string> certificatePaths, CancellationToken cancellationToken = default)
  {
    var certificates = new List<X509Certificate2>(certificatePaths.Count);
    foreach (var path in certificatePaths)
    {
      var bytes = await File.ReadAllBytesAsync(path, cancellationToken);
      certificates.Add(X509CertificateLoader.LoadCertificate(bytes));
    }

    EnsureCompleteChain(certificates);
    await StoreAsync(certificates, cancellationToken);
  }

  public async Task ImportPublicCertificateFromPfxAsync(string pfxPath, string password, CancellationToken cancellationToken = default)
  {
    var pfxBytes = await File.ReadAllBytesAsync(pfxPath, cancellationToken);
    using var pfx = X509CertificateLoader.LoadPkcs12(pfxBytes, password, X509KeyStorageFlags.EphemeralKeySet);
    var publicOnly = X509CertificateLoader.LoadCertificate(pfx.Export(X509ContentType.Cert));
    EnsureCompleteChain([publicOnly]);
    await StoreAsync([publicOnly], cancellationToken);
  }

  public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var certificate = await db.Certificates.SingleAsync(x => x.Id == id, cancellationToken);
    if (await db.Licenses.AnyAsync(l => l.SigningCertificateThumbprint == certificate.Thumbprint, cancellationToken) ||
        await db.Certificates.AnyAsync(c => c.ParentThumbprint == certificate.Thumbprint, cancellationToken))
    {
      throw new InvalidOperationException("Certificate deletion is blocked because dependent licenses or child certificates exist.");
    }

    db.Certificates.Remove(certificate);
    await db.SaveChangesAsync(cancellationToken);
  }

  private async Task StoreAsync(IReadOnlyList<X509Certificate2> certificates, CancellationToken cancellationToken)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    foreach (var certificate in certificates)
    {
      if (await db.Certificates.AnyAsync(c => c.Thumbprint == certificate.Thumbprint, cancellationToken))
      {
        continue;
      }

      db.Certificates.Add(new CertificateEntity
      {
        Id = Guid.NewGuid(),
        Thumbprint = certificate.Thumbprint,
        Subject = certificate.Subject,
        Issuer = certificate.Issuer,
        SerialNumber = certificate.SerialNumber,
        NotBefore = certificate.NotBefore,
        NotAfter = certificate.NotAfter,
        ParentThumbprint = FindParentThumbprint(certificate, certificates),
        RawDer = certificate.RawData,
        ImportedUtc = DateTimeOffset.UtcNow
      });
    }

    await db.SaveChangesAsync(cancellationToken);
  }

  private static void EnsureCompleteChain(IReadOnlyList<X509Certificate2> certificates)
  {
    foreach (var certificate in certificates)
    {
      if (certificate.Subject == certificate.Issuer)
      {
        continue;
      }

      var parent = certificates.Any(candidate => candidate.Subject == certificate.Issuer);
      if (!parent)
      {
        throw new InvalidOperationException($"The issuer certificate for '{certificate.Subject}' must be imported in the same operation.");
      }
    }
  }

  private static string? FindParentThumbprint(X509Certificate2 certificate, IReadOnlyList<X509Certificate2> certificates)
  {
    return certificate.Subject == certificate.Issuer
      ? null
      : certificates.FirstOrDefault(candidate => candidate.Subject == certificate.Issuer)?.Thumbprint;
  }

  private static CertificateSummary ToSummary(CertificateEntity entity)
  {
    return new CertificateSummary(entity.Id, entity.Thumbprint, entity.Subject, entity.Issuer, entity.SerialNumber, entity.NotBefore, entity.NotAfter, entity.ParentThumbprint);
  }
}
