using CovenantCouncil.App.Services;
using CovenantCouncil.ViewModels.Settings;
using System.Reactive.Threading.Tasks;

namespace CovenantCouncil.App.Views;

public partial class SettingsPage : ContentPage
{
  private readonly ApplicationSettingsViewModel viewModel;

  public SettingsPage(ApplicationSettingsViewModel viewModel)
  {
    InitializeComponent();
    this.viewModel = viewModel;
    BindingContext = this.viewModel;
    ViewModelErrorObserver.Observe(this.viewModel);
    Loaded += (_, _) => _ = LoadAsync();
  }

  private async Task LoadAsync()
  {
    try
    {
      await viewModel.Load.Execute().ToTask();
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
      await viewModel.Save.Execute().ToTask();
      await Navigation.PopModalAsync();
    }
    catch (Exception ex)
    {
      AppErrorPresenter.Show(ex);
    }
  }
}
