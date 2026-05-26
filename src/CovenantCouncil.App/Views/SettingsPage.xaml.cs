using System.Reactive.Threading.Tasks;
using CovenantCouncil.App.Services;
using CovenantCouncil.ViewModels.Settings;

namespace CovenantCouncil.App.Views;

public partial class SettingsPage : ContentPage
{
  private readonly ApplicationSettingsViewModel _viewModel;

  public SettingsPage(ApplicationSettingsViewModel viewModel)
  {
    InitializeComponent();
    _viewModel = viewModel;
    BindingContext = _viewModel;
    ViewModelErrorObserver.Observe(_viewModel);
    Loaded += (_, _) => _ = LoadAsync();
  }

  private async Task LoadAsync()
  {
    try
    {
      await _viewModel.Load.Execute().ToTask();
    }
    catch (Exception ex)
    {
      AppErrorPresenter.Show(ex);
    }
  }

  private void CancelClicked(object? sender, EventArgs e)
  {
    _ = Navigation.PopModalAsync();
  }

  private void SaveClicked(object? sender, EventArgs e)
  {
    _ = SaveAsync();
  }

  private async Task SaveAsync()
  {
    try
    {
      await _viewModel.Save.Execute().ToTask();
      await Navigation.PopModalAsync();
    }
    catch (Exception ex)
    {
      AppErrorPresenter.Show(ex);
    }
  }
}
