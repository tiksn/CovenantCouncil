using CovenantCouncil.UseCases.Abstractions;
using ReactiveUI;
using System.Reactive;

namespace CovenantCouncil.ViewModels.Settings;

public sealed class DatabaseGateViewModel : ViewModelBase
{
  private readonly IDatabaseSessionService databaseSessionService;
  private readonly IRecentDatabaseService recentDatabaseService;
  private string databasePath = "";
  private string password = "";
  private DatabaseSelectionMode selectionMode = DatabaseSelectionMode.OpenOrCreate;

  public DatabaseGateViewModel(IDatabaseSessionService databaseSessionService, IRecentDatabaseService recentDatabaseService)
  {
    this.databaseSessionService = databaseSessionService;
    this.recentDatabaseService = recentDatabaseService;
    OpenOrCreateDatabase = ReactiveCommand.CreateFromTask(OpenOrCreateDatabaseAsync, outputScheduler: RxSchedulers.MainThreadScheduler);
    LoadRecent = ReactiveCommand.CreateFromTask(LoadRecentAsync, outputScheduler: RxSchedulers.MainThreadScheduler);
    ObserveCommandErrors(OpenOrCreateDatabase);
    ObserveCommandErrors(LoadRecent);
  }

  public string DatabasePath
  {
    get => databasePath;
    set => this.RaiseAndSetIfChanged(ref databasePath, value);
  }

  public string Password
  {
    get => password;
    set => this.RaiseAndSetIfChanged(ref password, value);
  }

  public DatabaseSelectionMode SelectionMode
  {
    get => selectionMode;
    set => this.RaiseAndSetIfChanged(ref selectionMode, value);
  }

  public IReadOnlyList<string> RecentDatabasePaths { get; private set; } = [];

  public ReactiveCommand<Unit, Unit> OpenOrCreateDatabase { get; }

  public ReactiveCommand<Unit, Unit> LoadRecent { get; }

  private async Task LoadRecentAsync()
  {
    RecentDatabasePaths = await recentDatabaseService.GetRecentAsync();
    this.RaisePropertyChanged(nameof(RecentDatabasePaths));
  }

  private async Task OpenOrCreateDatabaseAsync()
  {
    switch (SelectionMode)
    {
      case DatabaseSelectionMode.Open:
        await databaseSessionService.OpenAsync(DatabasePath, Password);
        break;
      case DatabaseSelectionMode.Create:
        await databaseSessionService.CreateAsync(DatabasePath, Password);
        break;
      default:
        await databaseSessionService.OpenOrCreateAsync(DatabasePath, Password);
        break;
    }
  }
}
