using CovenantCouncil.App.Services;
using CovenantCouncil.ViewModels.Certificates;
using System.Reactive.Threading.Tasks;

namespace CovenantCouncil.App.Views;

public partial class CertificatesPage : ContentPage
{
  public CertificatesPage(CertificatesViewModel viewModel)
  {
    InitializeComponent();
    BindingContext = viewModel;
    ViewModelErrorObserver.Observe(viewModel);
    Loaded += (_, _) => _ = LoadAsync(viewModel);
  }

  private static async Task LoadAsync(CertificatesViewModel viewModel)
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
    if (BindingContext is CertificatesViewModel viewModel)
    {
      _ = LoadAsync(viewModel);
    }
  }
}
