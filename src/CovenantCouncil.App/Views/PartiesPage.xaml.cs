using CovenantCouncil.App.Services;
using CovenantCouncil.ViewModels.Parties;
using System.Reactive.Threading.Tasks;

namespace CovenantCouncil.App.Views;

public partial class PartiesPage : ContentPage
{
  public PartiesPage(PartiesViewModel viewModel)
  {
    InitializeComponent();
    BindingContext = viewModel;
    ViewModelErrorObserver.Observe(viewModel);
    Loaded += (_, _) => _ = LoadAsync(viewModel);
  }

  private static async Task LoadAsync(PartiesViewModel viewModel)
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
    if (BindingContext is PartiesViewModel viewModel)
    {
      _ = LoadAsync(viewModel);
    }
  }
}
