using CovenantCouncil.App.Services;
using CovenantCouncil.ViewModels.Licenses;
using System.Reactive.Threading.Tasks;

namespace CovenantCouncil.App.Views;

public partial class LicensesPage : ContentPage
{
  public LicensesPage(LicensesViewModel viewModel)
  {
    InitializeComponent();
    BindingContext = viewModel;
    ViewModelErrorObserver.Observe(viewModel);
    Loaded += (_, _) => _ = LoadAsync(viewModel);
  }

  private static async Task LoadAsync(LicensesViewModel viewModel)
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

  private void RefreshClicked(object? sender, EventArgs e)
  {
    if (BindingContext is LicensesViewModel viewModel)
    {
      _ = LoadAsync(viewModel);
    }
  }
}
