using System.Reactive;
using CovenantCouncil.UseCases.Parties;
using ReactiveUI;

namespace CovenantCouncil.ViewModels.Parties;

public sealed class AddPartyViewModel : ViewModelBase
{
  private readonly IPartyService _partyService;
  private string? _email;
  private string? _firstName;
  private string? _fullName;
  private PartyKind _kind = PartyKind.Individual;
  private string? _lastName;
  private string? _longName;
  private string? _shortName;
  private string? _website;

  public AddPartyViewModel(IPartyService partyService)
  {
    _partyService = partyService;
    Save = ReactiveCommand.CreateFromTask(SaveAsync, outputScheduler: RxSchedulers.MainThreadScheduler);
    ObserveCommandErrors(Save);
  }

  public IReadOnlyList<PartyKind> Kinds { get; } = [PartyKind.Individual, PartyKind.Organization];

  public PartyKind Kind
  {
    get => _kind;
    set
    {
      this.RaiseAndSetIfChanged(ref _kind, value);
      this.RaisePropertyChanged(nameof(IsIndividual));
      this.RaisePropertyChanged(nameof(IsOrganization));
    }
  }

  public bool IsIndividual => Kind == PartyKind.Individual;

  public bool IsOrganization => Kind == PartyKind.Organization;

  public string? Email
  {
    get => _email;
    set => this.RaiseAndSetIfChanged(ref _email, value);
  }

  public string? Website
  {
    get => _website;
    set => this.RaiseAndSetIfChanged(ref _website, value);
  }

  public string? FirstName
  {
    get => _firstName;
    set => this.RaiseAndSetIfChanged(ref _firstName, value);
  }

  public string? LastName
  {
    get => _lastName;
    set => this.RaiseAndSetIfChanged(ref _lastName, value);
  }

  public string? FullName
  {
    get => _fullName;
    set => this.RaiseAndSetIfChanged(ref _fullName, value);
  }

  public string? ShortName
  {
    get => _shortName;
    set => this.RaiseAndSetIfChanged(ref _shortName, value);
  }

  public string? LongName
  {
    get => _longName;
    set => this.RaiseAndSetIfChanged(ref _longName, value);
  }

  public ReactiveCommand<Unit, Unit> Save { get; }

  private async Task SaveAsync()
  {
    await _partyService.SaveAsync(new UpsertParty(
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
