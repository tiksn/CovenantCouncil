namespace CovenantCouncil.UseCases.Parties;

public sealed record PartySummary(
  Guid Id,
  PartyKind Kind,
  string DisplayName,
  string? Email,
  string? Website,
  DateTimeOffset CreatedUtc);

public sealed record UpsertParty(
  Guid? Id,
  PartyKind Kind,
  string? Email,
  string? Website,
  string? FirstName,
  string? LastName,
  string? FullName,
  string? ShortName,
  string? LongName);
