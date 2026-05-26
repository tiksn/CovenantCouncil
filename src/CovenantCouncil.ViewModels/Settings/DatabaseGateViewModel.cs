using System.Reactive;
using CovenantCouncil.UseCases.Abstractions;
using ReactiveUI;

namespace CovenantCouncil.ViewModels.Settings;

public sealed class DatabaseGateViewModel : ViewModelBase
{
  private readonly IDatabaseSessionService _databaseSessionService;
  private readonly IRecentDatabaseService _recentDatabaseService;
  private string _databasePath = "";
  private string _password = "";
  private DatabaseSelectionMode _selectionMode = DatabaseSelectionMode.OpenOrCreate;

  public DatabaseGateViewModel(IDatabaseSessionService databaseSessionService, IRecentDatabaseService recentDatabaseService)
  {
    _databaseSessionService = databaseSessionService;
    _recentDatabaseService = recentDatabaseService;
    OpenOrCreateDatabase = ReactiveCommand.CreateFromTask(OpenOrCreateDatabaseAsync, outputScheduler: RxSchedulers.MainThreadScheduler);
    LoadRecent = ReactiveCommand.CreateFromTask(LoadRecentAsync, outputScheduler: RxSchedulers.MainThreadScheduler);
    ObserveCommandErrors(OpenOrCreateDatabase);
    ObserveCommandErrors(LoadRecent);
  }

  public string DatabasePath
  {
    get => _databasePath;
    set => this.RaiseAndSetIfChanged(ref _databasePath, value);
  }

  public string Password
  {
    get => _password;
    set => this.RaiseAndSetIfChanged(ref _password, value);
  }

  public DatabaseSelectionMode SelectionMode
  {
    get => _selectionMode;
    set => this.RaiseAndSetIfChanged(ref _selectionMode, value);
  }

  public IReadOnlyList<string> RecentDatabasePaths { get; private set; } = [];

  public ReactiveCommand<Unit, Unit> OpenOrCreateDatabase { get; }

  public ReactiveCommand<Unit, Unit> LoadRecent { get; }

  private async Task LoadRecentAsync()
  {
    RecentDatabasePaths = await _recentDatabaseService.GetRecentAsync();
    this.RaisePropertyChanged(nameof(RecentDatabasePaths));
  }

  private async Task OpenOrCreateDatabaseAsync()
  {
    switch (SelectionMode)
    {
      case DatabaseSelectionMode.Open:
        await _databaseSessionService.OpenAsync(DatabasePath, Password);
        break;
      case DatabaseSelectionMode.Create:
        await _databaseSessionService.CreateAsync(DatabasePath, Password);
        break;
      default:
        await _databaseSessionService.OpenOrCreateAsync(DatabasePath, Password);
        break;
    }
  }
}
