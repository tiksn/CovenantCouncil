using System.Collections.ObjectModel;
using System.Reactive;
using CovenantCouncil.UseCases.Certificates;
using ReactiveUI;

namespace CovenantCouncil.ViewModels.Certificates;

public sealed class CertificatesViewModel : ViewModelBase
{
  private readonly ICertificateService _certificateService;

  public CertificatesViewModel(ICertificateService certificateService)
  {
    _certificateService = certificateService;
    Load = ReactiveCommand.CreateFromTask(LoadAsync, outputScheduler: RxSchedulers.MainThreadScheduler);
    ImportChain = ReactiveCommand.CreateFromTask<IReadOnlyList<string>>(ImportChainAsync, outputScheduler: RxSchedulers.MainThreadScheduler);
    Delete = ReactiveCommand.CreateFromTask<Guid>(DeleteAsync, outputScheduler: RxSchedulers.MainThreadScheduler);
    ObserveCommandErrors(Load);
    ObserveCommandErrors(ImportChain);
    ObserveCommandErrors(Delete);
  }

  public ObservableCollection<CertificateTreeNode> Roots { get; } = [];

  public ReactiveCommand<Unit, Unit> Load { get; }

  public ReactiveCommand<IReadOnlyList<string>, Unit> ImportChain { get; }

  public ReactiveCommand<Guid, Unit> Delete { get; }

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
