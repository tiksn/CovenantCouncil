using System.Reactive.Threading.Tasks;
using CovenantCouncil.App.Services;
using CovenantCouncil.ViewModels.Licenses;
using Microsoft.Extensions.DependencyInjection;

namespace CovenantCouncil.App.Views;

public partial class LicensesPage : ContentPage
{
  private readonly IServiceProvider _serviceProvider;

  public LicensesPage(LicensesViewModel viewModel, IServiceProvider serviceProvider)
  {
    InitializeComponent();
    _serviceProvider = serviceProvider;
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

  private void IssueClicked(object? sender, EventArgs e)
  {
    _ = IssueAsync();
  }

  private async Task IssueAsync()
  {
    try
    {
      var page = _serviceProvider.GetRequiredService<IssueLicensePage>();
      page.Issued += (_, _) =>
      {
        if (BindingContext is LicensesViewModel viewModel)
        {
          _ = LoadAsync(viewModel);
        }
      };
      await Navigation.PushAsync(page);
    }
    catch (Exception ex)
    {
      AppErrorPresenter.Show(ex);
    }
  }
}
