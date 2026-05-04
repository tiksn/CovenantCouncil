using System.Collections.ObjectModel;
using System.Reactive;
using CovenantCouncil.UseCases.Parties;
using ReactiveUI;

namespace CovenantCouncil.ViewModels.Parties;

public sealed class PartiesViewModel : ViewModelBase
{
  private readonly IPartyService partyService;
  private PartyKind? selectedKind;

  public PartiesViewModel(IPartyService partyService)
  {
    this.partyService = partyService;
    Load = ReactiveCommand.CreateFromTask(LoadAsync, outputScheduler: RxSchedulers.MainThreadScheduler);
    Save = ReactiveCommand.CreateFromTask<UpsertParty>(SaveAsync, outputScheduler: RxSchedulers.MainThreadScheduler);
    Delete = ReactiveCommand.CreateFromTask<Guid>(DeleteAsync, outputScheduler: RxSchedulers.MainThreadScheduler);
    ObserveCommandErrors(Load);
    ObserveCommandErrors(Save);
    ObserveCommandErrors(Delete);
  }

  public ObservableCollection<PartySummary> Parties { get; } = [];

  public PartyKind? SelectedKind
  {
    get => selectedKind;
    set
    {
      this.RaiseAndSetIfChanged(ref selectedKind, value);
      _ = Load.Execute().Subscribe(_ => { }, HandleException);
    }
  }

  public IReadOnlyList<PartyKind?> KindFilters { get; } = [null, PartyKind.Individual, PartyKind.Organization];

  public ReactiveCommand<Unit, Unit> Load { get; }

  public ReactiveCommand<UpsertParty, Unit> Save { get; }

  public ReactiveCommand<Guid, Unit> Delete { get; }

  private async Task LoadAsync()
  {
    Parties.Clear();
    foreach (var party in await partyService.ListAsync(SelectedKind))
    {
      Parties.Add(party);
    }
  }

  private async Task SaveAsync(UpsertParty party)
  {
    await partyService.SaveAsync(party);
    await LoadAsync();
  }

  private async Task DeleteAsync(Guid id)
  {
    await partyService.DeleteAsync(id);
    await LoadAsync();
  }
}
