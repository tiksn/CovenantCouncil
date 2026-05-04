using CovenantCouncil.ViewModels.Settings;
using CovenantCouncil.App.Services;
using CovenantCouncil.UseCases.Abstractions;
using System.Reactive.Threading.Tasks;

namespace CovenantCouncil.App.Views;

public partial class DatabaseGatePage : ContentPage
{
  private readonly DatabaseGateViewModel viewModel;
  private readonly ApplicationSettingsViewModel settingsViewModel;
  private readonly IDatabaseFilePicker databaseFilePicker;

  public DatabaseGatePage(
    DatabaseGateViewModel viewModel,
    ApplicationSettingsViewModel settingsViewModel,
    IDatabaseFilePicker databaseFilePicker)
  {
    InitializeComponent();
    this.viewModel = viewModel;
    this.settingsViewModel = settingsViewModel;
    this.databaseFilePicker = databaseFilePicker;
    BindingContext = this.viewModel;
    ViewModelErrorObserver.Observe(this.viewModel);
  }

  private void PickFileClicked(object? sender, EventArgs e)
  {
    _ = viewModel.RunWithBusyAsync(PickFileAsync);
  }

  private async Task PickFileAsync()
  {
    try
    {
      var action = await DisplayActionSheetAsync("Database file", "Cancel", null, "Open existing", "Create new");
      var mode = action switch
      {
        "Open existing" => DatabaseSelectionMode.Open,
        "Create new" => DatabaseSelectionMode.Create,
        _ => DatabaseSelectionMode.OpenOrCreate
      };
      var path = mode == DatabaseSelectionMode.OpenOrCreate ? null : await PickDatabasePathAsync(mode);

      if (!string.IsNullOrWhiteSpace(path))
      {
        viewModel.DatabasePath = path;
        viewModel.SelectionMode = mode;
      }
    }
    catch (Exception ex)
    {
      await ShowErrorAsync(ex);
    }
  }

  private async Task<string?> PickDatabasePathAsync(DatabaseSelectionMode mode)
  {
    var path = mode == DatabaseSelectionMode.Open
      ? await databaseFilePicker.PickOpenPathAsync()
      : await databaseFilePicker.PickCreatePathAsync();

    if (!string.IsNullOrWhiteSpace(path))
    {
      OpenOrCreateButton.Text = mode == DatabaseSelectionMode.Open ? "Open" : "Create";
    }

    return path;
  }

  private void OpenOrCreateClicked(object? sender, EventArgs e)
  {
    _ = OpenOrCreateAsync();
  }

  private async Task OpenOrCreateAsync()
  {
    try
    {
      await viewModel.OpenOrCreateDatabase.Execute().ToTask();
      if (Shell.Current is AppShell appShell)
      {
        await appShell.ShowDatabaseWorkspaceAsync();
      }
    }
    catch (Exception ex)
    {
      await ShowErrorAsync(ex);
    }
  }

  private void SettingsClicked(object? sender, EventArgs e)
  {
    _ = viewModel.RunWithBusyAsync(OpenSettingsAsync);
  }

  private async Task OpenSettingsAsync()
  {
    try
    {
      await Navigation.PushModalAsync(new NavigationPage(new SettingsPage(settingsViewModel)));
    }
    catch (Exception ex)
    {
      await ShowErrorAsync(ex);
    }
  }

  private static Task ShowErrorAsync(Exception exception)
  {
    AppErrorPresenter.Show(exception);
    return Task.CompletedTask;
  }
}
