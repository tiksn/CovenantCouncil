using System.Reactive;
using CovenantCouncil.UseCases.Settings;
using ReactiveUI;

namespace CovenantCouncil.ViewModels.Settings;

public sealed class ApplicationSettingsViewModel : ViewModelBase
{
  private readonly IApplicationSettingsService _settingsService;
  private string? _otlpEndpoint;

  public ApplicationSettingsViewModel(IApplicationSettingsService settingsService)
  {
    _settingsService = settingsService;
    Load = ReactiveCommand.CreateFromTask(LoadAsync, outputScheduler: RxSchedulers.MainThreadScheduler);
    Save = ReactiveCommand.CreateFromTask(SaveAsync, outputScheduler: RxSchedulers.MainThreadScheduler);
    ObserveCommandErrors(Load);
    ObserveCommandErrors(Save);
  }

  public string? OtlpEndpoint
  {
    get => _otlpEndpoint;
    set => this.RaiseAndSetIfChanged(ref _otlpEndpoint, value);
  }

  public IReadOnlyList<string> RecentDatabasePaths { get; private set; } = [];

  public ReactiveCommand<Unit, Unit> Load { get; }

  public ReactiveCommand<Unit, Unit> Save { get; }

  private async Task LoadAsync()
  {
    var settings = await _settingsService.GetAsync();
    OtlpEndpoint = settings.OtlpEndpoint;
    RecentDatabasePaths = settings.RecentDatabasePaths;
    this.RaisePropertyChanged(nameof(RecentDatabasePaths));
  }

  private async Task SaveAsync()
  {
    await _settingsService.SaveAsync(new ApplicationSettings(OtlpEndpoint, RecentDatabasePaths));
  }
}
