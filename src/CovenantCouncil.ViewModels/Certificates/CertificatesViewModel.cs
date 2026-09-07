using System.Collections.ObjectModel;
using System.Reactive;
using CovenantCouncil.UseCases.Certificates;
using ReactiveUI;
using ReactiveUI.Primitives;
using TIKSN.Concurrency;

namespace CovenantCouncil.ViewModels.Certificates;

public sealed class CertificatesViewModel : ViewModelBase
{
  private readonly ICertificateService _certificateService;

  public CertificatesViewModel(ICertificateService certificateService, ISequencers sequencers) : base(sequencers)
  {
    _certificateService = certificateService;
    Load = ReactiveCommand.CreateFromTask(LoadAsync, outputScheduler: Sequencers.MainThreadSequencer);
    ImportChain = ReactiveCommand.CreateFromTask<IReadOnlyList<string>>(ImportChainAsync, outputScheduler: Sequencers.MainThreadSequencer);
    Delete = ReactiveCommand.CreateFromTask<Guid>(DeleteAsync, outputScheduler: Sequencers.MainThreadSequencer);
    ObserveCommandErrors(Load);
    ObserveCommandErrors(ImportChain);
    ObserveCommandErrors(Delete);
  }

  public ObservableCollection<CertificateTreeNode> Roots { get; } = [];

  public ReactiveCommand<RxVoid, RxVoid> Load { get; }

  public ReactiveCommand<IReadOnlyList<string>, RxVoid> ImportChain { get; }

  public ReactiveCommand<Guid, RxVoid> Delete { get; }

  private async Task LoadAsync()
  {
    Roots.Clear();
    foreach (var root in await _certificateService.GetTreeAsync())
    {
      Roots.Add(root);
    }
  }

  private async Task ImportChainAsync(IReadOnlyList<string> paths)
  {
    await _certificateService.ImportPublicChainAsync(paths);
    await LoadAsync();
  }

  private async Task DeleteAsync(Guid id)
  {
    await _certificateService.DeleteAsync(id);
    await LoadAsync();
  }
}
