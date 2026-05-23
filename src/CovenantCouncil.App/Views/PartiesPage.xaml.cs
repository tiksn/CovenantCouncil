using System.Reactive.Threading.Tasks;
using CovenantCouncil.App.Services;
using CovenantCouncil.ViewModels.Parties;
using Microsoft.Extensions.DependencyInjection;

namespace CovenantCouncil.App.Views;

public partial class PartiesPage : ContentPage
{
  private readonly IServiceProvider _serviceProvider;

  public PartiesPage(PartiesViewModel viewModel, IServiceProvider serviceProvider)
  {
    InitializeComponent();
    _serviceProvider = serviceProvider;
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

  private void AddClicked(object? sender, EventArgs e)
  {
    _ = AddAsync();
  }

  private async Task AddAsync()
  {
    try
    {
      var page = _serviceProvider.GetRequiredService<AddPartyPage>();
      page.Saved += (_, _) =>
      {
        if (BindingContext is PartiesViewModel viewModel)
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

  private void DeleteClicked(object? sender, EventArgs e)
  {
    _ = DeleteAsync(sender);
  }

  private async Task DeleteAsync(object? sender)
  {
    if (BindingContext is not PartiesViewModel viewModel ||
        sender is not Button button ||
        button.CommandParameter is not Guid id)
    {
      return;
    }

    try
    {
      var confirmed = await DisplayAlertAsync("Delete party", "Delete this party?", "Delete", "Cancel");
      if (!confirmed)
      {
        return;
      }

      await viewModel.Delete.Execute(id).ToTask();
    }
    catch (Exception ex)
    {
      AppErrorPresenter.Show(ex);
    }
  }
}
