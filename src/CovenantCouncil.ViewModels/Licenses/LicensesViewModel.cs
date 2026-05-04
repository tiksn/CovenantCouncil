using System.Collections.ObjectModel;
using System.Reactive;
using CovenantCouncil.UseCases.Licenses;
using ReactiveUI;

namespace CovenantCouncil.ViewModels.Licenses;

public sealed class LicensesViewModel : ViewModelBase
{
  private readonly ILicenseCatalog licenseCatalog;
  private readonly ILicenseService licenseService;
  private bool isLoading;
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
      var changed = !EqualityComparer<LicenseDescriptorSummary?>.Default.Equals(selectedDescriptor, value);
      this.RaiseAndSetIfChanged(ref selectedDescriptor, value);
      if (changed && !isLoading)
      {
        _ = LoadLicensesAsync().ContinueWith(
          task =>
          {
            if (task.Exception is not null)
            {
              HandleException(task.Exception.GetBaseException());
            }
          },
          TaskScheduler.Default);
      }
    }
  }

  public ReactiveCommand<Unit, Unit> Load { get; }

  public ReactiveCommand<IssueLicenseRequest, Unit> Issue { get; }

  public ReactiveCommand<(Guid Id, string Path), Unit> Export { get; }

  public ReactiveCommand<string, Unit> Import { get; }

  public ReactiveCommand<Guid, Unit> Delete { get; }

  private async Task LoadAsync()
  {
    isLoading = true;
    try
    {
      Descriptors.Clear();
      foreach (var descriptor in licenseCatalog.GetDescriptors())
      {
        Descriptors.Add(descriptor);
      }

      await LoadLicensesAsync();
    }
    finally
    {
      isLoading = false;
    }
  }

  private async Task LoadLicensesAsync()
  {
    Licenses.Clear();
    foreach (var license in await licenseService.ListAsync(SelectedDescriptor?.Discriminator))
    {
      Licenses.Add(license);
    }
  }

  private async Task IssueAsync(IssueLicenseRequest request)
  {
    await licenseService.IssueAsync(request);
    await LoadLicensesAsync();
  }

  private async Task DeleteAsync(Guid id)
  {
    await licenseService.DeleteAsync(id);
    await LoadLicensesAsync();
  }
}
