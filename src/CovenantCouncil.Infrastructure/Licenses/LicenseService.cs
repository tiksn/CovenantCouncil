using System.Text;
using CovenantCouncil.Infrastructure.Certificates;
using CovenantCouncil.Infrastructure.Data;
using CovenantCouncil.Infrastructure.Data.Entities;
using CovenantCouncil.Infrastructure.Security;
using CovenantCouncil.UseCases.Licenses;
using Microsoft.EntityFrameworkCore;

namespace CovenantCouncil.Infrastructure.Licenses;

public sealed class LicenseService(
  IDbContextFactory<CovenantCouncilDbContext> dbContextFactory,
  IPortableProtectionService protectionService,
  ILicenseCatalog licenseCatalog) : ILicenseService
{
  public async Task<IReadOnlyList<LicenseSummary>> ListAsync(string? descriptorDiscriminator, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var query = db.Licenses.AsNoTracking().Include(l => l.Party).AsQueryable();
    if (!string.IsNullOrWhiteSpace(descriptorDiscriminator))
    {
      query = query.Where(l => l.DescriptorDiscriminator == descriptorDiscriminator);
    }

    var licenses = await query
      .Select(l => new LicenseSummary(
        l.Id,
        l.DescriptorDiscriminator,
        l.DescriptorName,
        l.PartyId,
        l.Party == null ? "" : (l.Party.FullName ?? l.Party.LongName ?? l.Party.ShortName ?? l.Party.Email ?? ""),
        l.SigningCertificateThumbprint,
        l.IssuedUtc))
      .ToListAsync(cancellationToken);

    return licenses
      .OrderByDescending(l => l.IssuedUtc)
      .ToList();
  }

  public async Task<Guid> IssueAsync(IssueLicenseRequest request, CancellationToken cancellationToken = default)
  {
    if (!Path.GetExtension(request.PfxPath).Equals(".pfx", StringComparison.OrdinalIgnoreCase))
    {
      throw new InvalidOperationException("License issuing requires a PFX private certificate file.");
    }

    var descriptor = licenseCatalog.GetDescriptors().Single(d => d.Discriminator == request.DescriptorDiscriminator);
    var pfx = System.Security.Cryptography.X509Certificates.X509CertificateLoader.LoadPkcs12FromFile(
      request.PfxPath,
      request.PfxPassword,
      System.Security.Cryptography.X509Certificates.X509KeyStorageFlags.EphemeralKeySet);

    var payload = Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(new
    {
      descriptor = request.DescriptorDiscriminator,
      partyId = request.PartyId,
      issuedUtc = DateTimeOffset.UtcNow,
      entitlements = request.EntitlementValues
    }));

    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    if (!await db.Certificates.AnyAsync(c => c.Thumbprint == pfx.Thumbprint, cancellationToken))
    {
      db.Certificates.Add(new CertificateEntity
      {
        Id = Guid.NewGuid(),
        Thumbprint = pfx.Thumbprint,
        Subject = pfx.Subject,
        Issuer = pfx.Issuer,
        SerialNumber = pfx.SerialNumber,
        NotBefore = pfx.NotBefore,
        NotAfter = pfx.NotAfter,
        ParentThumbprint = null,
        RawDer = pfx.Export(System.Security.Cryptography.X509Certificates.X509ContentType.Cert),
        ImportedUtc = DateTimeOffset.UtcNow
      });
    }

    var id = Guid.NewGuid();
    db.Licenses.Add(new LicenseEntity
    {
      Id = id,
      DescriptorDiscriminator = descriptor.Discriminator,
      DescriptorName = descriptor.Name,
      PartyId = request.PartyId,
      SigningCertificateThumbprint = pfx.Thumbprint,
      ProtectedPayload = protectionService.Protect(payload),
      IssuedUtc = DateTimeOffset.UtcNow
    });
    await db.SaveChangesAsync(cancellationToken);
    return id;
  }

  public async Task ExportAsync(Guid id, string licensePath, CancellationToken cancellationToken = default)
  {
    if (Path.GetExtension(licensePath) is not ".cclic")
    {
      throw new ArgumentException("License export files must use the .cclic extension.", nameof(licensePath));
    }

    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var license = await db.Licenses.AsNoTracking().SingleAsync(x => x.Id == id, cancellationToken);
    var payload = protectionService.Unprotect(license.ProtectedPayload);
    await File.WriteAllBytesAsync(licensePath, payload, cancellationToken);
  }

  public async Task ImportAsync(string licensePath, CancellationToken cancellationToken = default)
  {
    if (Path.GetExtension(licensePath) is not ".cclic")
    {
      throw new ArgumentException("License import files must use the .cclic extension.", nameof(licensePath));
    }

    var payload = await File.ReadAllBytesAsync(licensePath, cancellationToken);
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    db.Licenses.Add(new LicenseEntity
    {
      Id = Guid.NewGuid(),
      DescriptorDiscriminator = "imported",
      DescriptorName = "Imported",
      PartyId = await db.Parties.Select(p => p.Id).FirstAsync(cancellationToken),
      SigningCertificateThumbprint = "",
      ProtectedPayload = protectionService.Protect(payload),
      IssuedUtc = DateTimeOffset.UtcNow
    });
    await db.SaveChangesAsync(cancellationToken);
  }

  public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var license = await db.Licenses.SingleAsync(x => x.Id == id, cancellationToken);
    db.Licenses.Remove(license);
    await db.SaveChangesAsync(cancellationToken);
  }
}
