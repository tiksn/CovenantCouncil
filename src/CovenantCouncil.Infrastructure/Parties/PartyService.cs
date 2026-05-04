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
    var id = party.Id ?? Guid.NewGuid();
    var entity = party.Id is null ? null : await db.Parties.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    if (entity is null)
    {
      entity = new PartyEntity { Id = id, CreatedUtc = DateTimeOffset.UtcNow };
      db.Parties.Add(entity);
    }

    entity.Kind = party.Kind;
    entity.Email = party.Email;
    entity.Website = party.Website;
    entity.FirstName = party.FirstName;
    entity.LastName = party.LastName;
    entity.FullName = party.FullName;
    entity.ShortName = party.ShortName;
    entity.LongName = party.LongName;

    await db.SaveChangesAsync(cancellationToken);
    return id;
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
}
