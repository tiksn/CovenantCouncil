using System.Reactive;
using CovenantCouncil.UseCases.Parties;
using ReactiveUI;

namespace CovenantCouncil.ViewModels.Parties;

public sealed class AddPartyViewModel : ViewModelBase
{
  private readonly IPartyService partyService;
  private string? email;
  private string? firstName;
  private string? fullName;
  private PartyKind kind = PartyKind.Individual;
  private string? lastName;
  private string? longName;
  private string? shortName;
  private string? website;

  public AddPartyViewModel(IPartyService partyService)
  {
    this.partyService = partyService;
    Save = ReactiveCommand.CreateFromTask(SaveAsync, outputScheduler: RxSchedulers.MainThreadScheduler);
    ObserveCommandErrors(Save);
  }

  public IReadOnlyList<PartyKind> Kinds { get; } = [PartyKind.Individual, PartyKind.Organization];

  public PartyKind Kind
  {
    get => kind;
    set
    {
      this.RaiseAndSetIfChanged(ref kind, value);
      this.RaisePropertyChanged(nameof(IsIndividual));
      this.RaisePropertyChanged(nameof(IsOrganization));
    }
  }

  public bool IsIndividual => Kind == PartyKind.Individual;

  public bool IsOrganization => Kind == PartyKind.Organization;

  public string? Email
  {
    get => email;
    set => this.RaiseAndSetIfChanged(ref email, value);
  }

  public string? Website
  {
    get => website;
    set => this.RaiseAndSetIfChanged(ref website, value);
  }

  public string? FirstName
  {
    get => firstName;
    set => this.RaiseAndSetIfChanged(ref firstName, value);
  }

  public string? LastName
  {
    get => lastName;
    set => this.RaiseAndSetIfChanged(ref lastName, value);
  }

  public string? FullName
  {
    get => fullName;
    set => this.RaiseAndSetIfChanged(ref fullName, value);
  }

  public string? ShortName
  {
    get => shortName;
    set => this.RaiseAndSetIfChanged(ref shortName, value);
  }

  public string? LongName
  {
    get => longName;
    set => this.RaiseAndSetIfChanged(ref longName, value);
  }

  public ReactiveCommand<Unit, Unit> Save { get; }

  private async Task SaveAsync()
  {
    await partyService.SaveAsync(new UpsertParty(
      Id: null,
      Kind,
      Email,
      Website,
      FirstName,
      LastName,
      FullName,
      ShortName,
      LongName));
  }
}
