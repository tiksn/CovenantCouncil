namespace CovenantCouncil.UseCases.Parties;

public interface IPartyService
{
  Task<IReadOnlyList<PartySummary>> ListAsync(PartyKind? kind, CancellationToken cancellationToken = default);

  Task<PartyDetail> GetAsync(Guid id, CancellationToken cancellationToken = default);

  Task<Guid> SaveAsync(UpsertParty party, CancellationToken cancellationToken = default);

  Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
