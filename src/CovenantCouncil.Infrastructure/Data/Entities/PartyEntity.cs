using CovenantCouncil.UseCases.Parties;

namespace CovenantCouncil.Infrastructure.Data.Entities;

public sealed class PartyEntity
{
  public Guid Id { get; set; }

  public PartyKind Kind { get; set; }

  public string? Email { get; set; }

  public string? Website { get; set; }

  public string? FirstName { get; set; }

  public string? LastName { get; set; }

  public string? FullName { get; set; }

  public string? ShortName { get; set; }

  public string? LongName { get; set; }

  public DateTimeOffset CreatedUtc { get; set; }
}
