using CovenantCouncil.Infrastructure.Data;
using CovenantCouncil.Infrastructure.Data.Entities;
using CovenantCouncil.UseCases.Parties;
using Microsoft.EntityFrameworkCore;

namespace CovenantCouncil.Infrastructure.Parties;

public sealed class PartyService(IDbContextFactory<CovenantCouncilDbContext> dbContextFactory) : IPartyService
{
  public async Task<IReadOnlyList<PartySummary>> ListAsync(PartyKind? kind, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var query = db.Parties.AsNoTracking();
    if (kind is not null)
    {
      query = query.Where(p => p.Kind == kind);
    }

    return await query
      .OrderBy(p => p.Kind)
      .ThenBy(p => p.FullName ?? p.LongName ?? p.ShortName ?? p.Email)
      .Select(p => new PartySummary(p.Id, p.Kind, DisplayName(p), p.Email, p.Website, p.CreatedUtc))
      .ToListAsync(cancellationToken);
  }

  public async Task<Guid> SaveAsync(UpsertParty party, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    var normalizedParty = Normalize(party);
    var existing = await db.Parties
      .AsNoTracking()
      .Where(p => p.Kind == normalizedParty.Kind
        && p.Email == normalizedParty.Email
        && p.Website == normalizedParty.Website
        && p.FirstName == normalizedParty.FirstName
        && p.LastName == normalizedParty.LastName
        && p.FullName == normalizedParty.FullName
        && p.ShortName == normalizedParty.ShortName
        && p.LongName == normalizedParty.LongName)
      .Select(p => (Guid?)p.Id)
      .FirstOrDefaultAsync(cancellationToken);

    if (existing is Guid existingId)
    {
      return existingId;
    }

    var id = party.Id ?? Guid.NewGuid();
    var entity = party.Id is null ? null : await db.Parties.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    if (entity is null)
    {
      entity = new PartyEntity { Id = id, CreatedUtc = DateTimeOffset.UtcNow };
      db.Parties.Add(entity);
    }

    entity.Kind = normalizedParty.Kind;
    entity.Email = normalizedParty.Email;
    entity.Website = normalizedParty.Website;
    entity.FirstName = normalizedParty.FirstName;
    entity.LastName = normalizedParty.LastName;
    entity.FullName = normalizedParty.FullName;
    entity.ShortName = normalizedParty.ShortName;
    entity.LongName = normalizedParty.LongName;

    await db.SaveChangesAsync(cancellationToken);
    return id;
  }

  public async Task<PartyDetail> GetAsync(Guid id, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    return await db.Parties
      .AsNoTracking()
      .Where(p => p.Id == id)
      .Select(p => new PartyDetail(
        p.Id,
        p.Kind,
        p.Email,
        p.Website,
        p.FirstName,
        p.LastName,
        p.FullName,
        p.ShortName,
        p.LongName))
      .SingleAsync(cancellationToken);
  }

  public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
  {
    await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
    if (await db.Licenses.AnyAsync(l => l.PartyId == id, cancellationToken))
    {
      throw new InvalidOperationException("Party deletion is blocked because licenses depend on it.");
    }

    var entity = await db.Parties.SingleAsync(x => x.Id == id, cancellationToken);
    db.Parties.Remove(entity);
    await db.SaveChangesAsync(cancellationToken);
  }

  private static string DisplayName(PartyEntity party)
  {
    return party.Kind == PartyKind.Individual
      ? party.FullName ?? $"{party.FirstName} {party.LastName}".Trim()
      : party.LongName ?? party.ShortName ?? "";
  }

  private static UpsertParty Normalize(UpsertParty party)
  {
    return party with
    {
      Email = NormalizeText(party.Email),
      Website = NormalizeText(party.Website),
      FirstName = NormalizeText(party.FirstName),
      LastName = NormalizeText(party.LastName),
      FullName = NormalizeText(party.FullName),
      ShortName = NormalizeText(party.ShortName),
      LongName = NormalizeText(party.LongName)
    };
  }

  private static string? NormalizeText(string? value)
  {
    var normalized = value?.Trim();
    return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
  }
}
