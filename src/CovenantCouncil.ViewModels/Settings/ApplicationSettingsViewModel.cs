using System.Reactive;
using CovenantCouncil.UseCases.Settings;
using ReactiveUI;
using ReactiveUI.Primitives;
using TIKSN.Concurrency;

namespace CovenantCouncil.ViewModels.Settings;

public sealed class ApplicationSettingsViewModel : ViewModelBase
{
  private readonly IApplicationSettingsService _settingsService;
  private string? _otlpEndpoint;

  public ApplicationSettingsViewModel(IApplicationSettingsService settingsService, ISequencers sequencers) : base(sequencers)
  {
    _settingsService = settingsService;
    Load = ReactiveCommand.CreateFromTask(LoadAsync, outputScheduler: Sequencers.MainThreadSequencer);
    Save = ReactiveCommand.CreateFromTask(SaveAsync, outputScheduler: Sequencers.MainThreadSequencer);
    ObserveCommandErrors(Load);
    ObserveCommandErrors(Save);
  }

  public string? OtlpEndpoint
  {
    get => _otlpEndpoint;
    set => this.RaiseAndSetIfChanged(ref _otlpEndpoint, value);
  }

  public IReadOnlyList<string> RecentDatabasePaths { get; private set; } = [];

  public ReactiveCommand<RxVoid, RxVoid> Load { get; }

  public ReactiveCommand<RxVoid, RxVoid> Save { get; }

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
