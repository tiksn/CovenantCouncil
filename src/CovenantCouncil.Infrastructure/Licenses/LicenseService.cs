using CovenantCouncil.Infrastructure.Certificates;
using CovenantCouncil.Infrastructure.Data;
using CovenantCouncil.Infrastructure.Data.Entities;
using CovenantCouncil.Infrastructure.Security;
using CovenantCouncil.UseCases.Licenses;
using CovenantCouncil.UseCases.Parties;
using Google.Protobuf;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Net.Mail;
using System.Security.Cryptography.X509Certificates;
using TIKSN.Deployment;
using TIKSN.Licensing;
using FossaCompanyEntitlements = Fossa.Licensing.CompanyEntitlements;
using FossaSystemEntitlements = Fossa.Licensing.SystemEntitlements;
using VerdantSystemEntitlements = VerdantApp.Licensing.SystemEntitlements;

namespace CovenantCouncil.Infrastructure.Licenses;

public sealed class LicenseService(
  IDbContextFactory<CovenantCouncilDbContext> dbContextFactory,
  IPortableProtectionService protectionService,
  ILicenseCatalog licenseCatalog,
  IServiceProvider serviceProvider) : ILicenseService
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
    if (request.NotAfterUtc <= request.NotBeforeUtc)
    {
      throw new InvalidOperationException("License validity end must be after the validity start.");
    }

    if (!Path.GetExtension(request.PfxPath).Equals(".pfx", StringComparison.OrdinalIgnoreCase))
    {
      throw new InvalidOperationException("License issuing requires a PFX private certificate file.");
    }

    var descriptor = licenseCatalog.GetDescriptors().Single(d => d.Discriminator == request.DescriptorDiscriminator);
    var pfx = X509CertificateLoader.LoadPkcs12FromFile(
      request.PfxPath,
      request.PfxPassword,
      X509KeyStorageFlags.EphemeralKeySet);

    if (!pfx.HasPrivateKey)
    {
      throw new InvalidOperationException("The selected PFX certificate does not contain a private key.");
    }

    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var licensorEntity = await db.Parties.AsNoTracking().SingleAsync(p => p.Id == request.LicensorPartyId, cancellationToken);
    var licenseeEntity = await db.Parties.AsNoTracking().SingleAsync(p => p.Id == request.LicenseePartyId, cancellationToken);
    var terms = new LicenseTerms(
      Ulid.Parse(request.SerialNumber),
      ToTikSNParty(licensorEntity),
      ToTikSNParty(licenseeEntity),
      request.NotBeforeUtc,
      request.NotAfterUtc);

    var payload = descriptor.EntitlementKind switch
    {
      LicenseEntitlementKinds.FossaCompany => CreateLicenseData<FossaCompanyEntitlements, Fossa.Licensing.CompanyLicenseEntitlements>(
        terms,
        CreateFossaCompanyEntitlements(request.EntitlementValues),
        pfx),
      LicenseEntitlementKinds.FossaSystem => CreateLicenseData<FossaSystemEntitlements, Fossa.Licensing.SystemLicenseEntitlements>(
        terms,
        CreateFossaSystemEntitlements(request.EntitlementValues),
        pfx),
      LicenseEntitlementKinds.VerdantSystem => CreateLicenseData<VerdantSystemEntitlements, VerdantApp.Licensing.SystemLicenseEntitlements>(
        terms,
        CreateVerdantSystemEntitlements(request.EntitlementValues),
        pfx),
      _ => throw new InvalidOperationException($"Unsupported license descriptor kind '{descriptor.EntitlementKind}'.")
    };

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
      PartyId = request.LicenseePartyId,
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

  private byte[] CreateLicenseData<TEntitlements, TEntitlementsData>(
    LicenseTerms terms,
    TEntitlements entitlements,
    X509Certificate2 signingCertificate)
    where TEntitlementsData : IMessage<TEntitlementsData>
  {
    if (serviceProvider.GetService(typeof(ILicenseFactory<TEntitlements, TEntitlementsData>)) is not ILicenseFactory<TEntitlements, TEntitlementsData> factory)
    {
      throw new InvalidOperationException($"License factory for {typeof(TEntitlements).Name} is not registered.");
    }

    var license = GetValid(factory.Create(terms, entitlements, signingCertificate));
    return license.Data.ToArray();
  }

  private static T GetValid<T>(Validation<Error, T> validation)
  {
    return validation.Match(
      Succ: value => value,
      Fail: errors => throw new InvalidOperationException(string.Join(Environment.NewLine, errors.Select(error => error.Message))));
  }

  private static FossaCompanyEntitlements CreateFossaCompanyEntitlements(IReadOnlyDictionary<string, string> values)
  {
    return new FossaCompanyEntitlements(
      ParseUlid(values, LicenseEntitlementFields.SystemId),
      ParseInt64(values, LicenseEntitlementFields.CompanyId),
      ParseInt32(values, LicenseEntitlementFields.MaximumBranchCount),
      ParseInt32(values, LicenseEntitlementFields.MaximumEmployeeCount),
      ParseInt32(values, LicenseEntitlementFields.MaximumDepartmentCount));
  }

  private static FossaSystemEntitlements CreateFossaSystemEntitlements(IReadOnlyDictionary<string, string> values)
  {
    return new FossaSystemEntitlements(
      ParseUlid(values, LicenseEntitlementFields.SystemId),
      EnvironmentName.Parse(Require(values, LicenseEntitlementFields.EnvironmentName), CultureInfo.InvariantCulture),
      ParseInt32(values, LicenseEntitlementFields.MaximumCompanyCount),
      LanguageExt.Prelude.Seq(ParseCountries(values)));
  }

  private static VerdantSystemEntitlements CreateVerdantSystemEntitlements(IReadOnlyDictionary<string, string> values)
  {
    return new VerdantSystemEntitlements(
      ParseUlid(values, LicenseEntitlementFields.SystemId),
      EnvironmentName.Parse(Require(values, LicenseEntitlementFields.EnvironmentName), CultureInfo.InvariantCulture),
      LanguageExt.Prelude.Seq(ParseCountries(values)));
  }

  private static RegionInfo[] ParseCountries(IReadOnlyDictionary<string, string> values)
  {
    var countryCodes = Require(values, LicenseEntitlementFields.CountryCodes)
      .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
      .Distinct(StringComparer.OrdinalIgnoreCase)
      .ToArray();

    if (countryCodes.Length == 0)
    {
      throw new InvalidOperationException("At least one country must be selected.");
    }

    return countryCodes.Select(code => new RegionInfo(code)).ToArray();
  }

  private static int ParseInt32(IReadOnlyDictionary<string, string> values, string key)
  {
    return int.TryParse(Require(values, key), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
      ? value
      : throw new InvalidOperationException($"{key} must be a whole number.");
  }

  private static long ParseInt64(IReadOnlyDictionary<string, string> values, string key)
  {
    return long.TryParse(Require(values, key), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
      ? value
      : throw new InvalidOperationException($"{key} must be a whole number.");
  }

  private static Ulid ParseUlid(IReadOnlyDictionary<string, string> values, string key)
  {
    return Ulid.Parse(Require(values, key));
  }

  private static string Require(IReadOnlyDictionary<string, string> values, string key)
  {
    if (!values.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
    {
      throw new InvalidOperationException($"{key} is required.");
    }

    return value.Trim();
  }

  private static Party ToTikSNParty(PartyEntity party)
  {
    var email = string.IsNullOrWhiteSpace(party.Email) ? null : new MailAddress(party.Email);
    var website = string.IsNullOrWhiteSpace(party.Website) ? null : new Uri(party.Website, UriKind.Absolute);
    return party.Kind switch
    {
      PartyKind.Individual => new IndividualParty(
        party.FirstName ?? "",
        party.LastName ?? "",
        party.FullName ?? "",
        email!,
        website!),
      PartyKind.Organization => new OrganizationParty(
        party.ShortName ?? "",
        party.LongName ?? "",
        email!,
        website!),
      _ => throw new InvalidOperationException($"Unsupported party kind '{party.Kind}'.")
    };
  }
}
