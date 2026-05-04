using System.Collections.ObjectModel;
using System.Reactive;
using CovenantCouncil.UseCases.Licenses;
using ReactiveUI;

namespace CovenantCouncil.ViewModels.Licenses;

public sealed class LicensesViewModel : ViewModelBase
{
  private readonly ILicenseCatalog licenseCatalog;
  private readonly ILicenseService licenseService;
  private LicenseDescriptorSummary? selectedDescriptor;

  public LicensesViewModel(ILicenseCatalog licenseCatalog, ILicenseService licenseService)
  {
    this.licenseCatalog = licenseCatalog;
    this.licenseService = licenseService;
    Load = ReactiveCommand.CreateFromTask(LoadAsync, outputScheduler: RxSchedulers.MainThreadScheduler);
    Issue = ReactiveCommand.CreateFromTask<IssueLicenseRequest>(IssueAsync, outputScheduler: RxSchedulers.MainThreadScheduler);
    Export = ReactiveCommand.CreateFromTask<(Guid Id, string Path)>(request => licenseService.ExportAsync(request.Id, request.Path), outputScheduler: RxSchedulers.MainThreadScheduler);
    Import = ReactiveCommand.CreateFromTask<string>(licenseService.ImportAsync, outputScheduler: RxSchedulers.MainThreadScheduler);
    Delete = ReactiveCommand.CreateFromTask<Guid>(DeleteAsync, outputScheduler: RxSchedulers.MainThreadScheduler);
    ObserveCommandErrors(Load);
    ObserveCommandErrors(Issue);
    ObserveCommandErrors(Export);
    ObserveCommandErrors(Import);
    ObserveCommandErrors(Delete);
  }

  public ObservableCollection<LicenseDescriptorSummary> Descriptors { get; } = [];

  public ObservableCollection<LicenseSummary> Licenses { get; } = [];

  public LicenseDescriptorSummary? SelectedDescriptor
  {
    get => selectedDescriptor;
    set
    {
      this.RaiseAndSetIfChanged(ref selectedDescriptor, value);
      _ = Load.Execute().Subscribe(_ => { }, HandleException);
    }
  }

  public ReactiveCommand<Unit, Unit> Load { get; }

  public ReactiveCommand<IssueLicenseRequest, Unit> Issue { get; }

  public ReactiveCommand<(Guid Id, string Path), Unit> Export { get; }

  public ReactiveCommand<string, Unit> Import { get; }

  public ReactiveCommand<Guid, Unit> Delete { get; }

  private async Task LoadAsync()
  {
    Descriptors.Clear();
    foreach (var descriptor in licenseCatalog.GetDescriptors())
    {
      Descriptors.Add(descriptor);
    }

    Licenses.Clear();
    foreach (var license in await licenseService.ListAsync(SelectedDescriptor?.Discriminator))
    {
      Licenses.Add(license);
    }
  }

  private async Task IssueAsync(IssueLicenseRequest request)
  {
    await licenseService.IssueAsync(request);
    await LoadAsync();
  }

  private async Task DeleteAsync(Guid id)
  {
    await licenseService.DeleteAsync(id);
    await LoadAsync();
  }
}
