using CovenantCouncil.UseCases.Settings;
using ReactiveUI;
using System.Reactive;

namespace CovenantCouncil.ViewModels.Settings;

public sealed class ApplicationSettingsViewModel : ViewModelBase
{
  private readonly IApplicationSettingsService settingsService;
  private string? otlpEndpoint;

  public ApplicationSettingsViewModel(IApplicationSettingsService settingsService)
  {
    this.settingsService = settingsService;
    Load = ReactiveCommand.CreateFromTask(LoadAsync, outputScheduler: RxSchedulers.MainThreadScheduler);
    Save = ReactiveCommand.CreateFromTask(SaveAsync, outputScheduler: RxSchedulers.MainThreadScheduler);
    ObserveCommandErrors(Load);
    ObserveCommandErrors(Save);
  }

  public string? OtlpEndpoint
  {
    get => otlpEndpoint;
    set => this.RaiseAndSetIfChanged(ref otlpEndpoint, value);
  }

  public IReadOnlyList<string> RecentDatabasePaths { get; private set; } = [];

  public ReactiveCommand<Unit, Unit> Load { get; }

  public ReactiveCommand<Unit, Unit> Save { get; }

  private async Task LoadAsync()
  {
    var settings = await settingsService.GetAsync();
    OtlpEndpoint = settings.OtlpEndpoint;
    RecentDatabasePaths = settings.RecentDatabasePaths;
    this.RaisePropertyChanged(nameof(RecentDatabasePaths));
  }

  private async Task SaveAsync()
  {
    await settingsService.SaveAsync(new ApplicationSettings(OtlpEndpoint, RecentDatabasePaths));
  }
}
