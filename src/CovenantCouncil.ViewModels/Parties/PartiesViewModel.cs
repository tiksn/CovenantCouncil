using System.Collections.ObjectModel;
using System.Reactive;
using CovenantCouncil.UseCases.Parties;
using ReactiveUI;
using ReactiveUI.Primitives;

namespace CovenantCouncil.ViewModels.Parties;

public sealed class PartiesViewModel : ViewModelBase
{
  private readonly IPartyService _partyService;
  private PartyKind? _selectedKind;

  public PartiesViewModel(IPartyService partyService)
  {
    _partyService = partyService;
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
    get => _selectedKind;
    set
    {
      this.RaiseAndSetIfChanged(ref _selectedKind, value);
      System.ObservableExtensions.Subscribe(
      Load.Execute(),
      _ => { },
      HandleException);
    }
  }

  public IReadOnlyList<PartyKind?> KindFilters { get; } = [null, PartyKind.Individual, PartyKind.Organization];

  public ReactiveCommand<RxVoid, RxVoid> Load { get; }

  public ReactiveCommand<UpsertParty, RxVoid> Save { get; }

  public ReactiveCommand<Guid, RxVoid> Delete { get; }

  private async Task LoadAsync()
  {
    Parties.Clear();
    foreach (var party in await _partyService.ListAsync(SelectedKind))
    {
      Parties.Add(party);
    }
  }

  private async Task SaveAsync(UpsertParty party)
  {
    await _partyService.SaveAsync(party);
    await LoadAsync();
  }

  private async Task DeleteAsync(Guid id)
  {
    await _partyService.DeleteAsync(id);
    await LoadAsync();
  }
}
