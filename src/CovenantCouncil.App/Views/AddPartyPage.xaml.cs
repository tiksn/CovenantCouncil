using System.Reactive.Threading.Tasks;
using CovenantCouncil.App.Services;
using CovenantCouncil.ViewModels.Parties;

namespace CovenantCouncil.App.Views;

public partial class AddPartyPage : ContentPage
{
  private readonly AddPartyViewModel _viewModel;

  public AddPartyPage(AddPartyViewModel viewModel)
  {
    InitializeComponent();
    _viewModel = viewModel;
    BindingContext = _viewModel;
    ViewModelErrorObserver.Observe(_viewModel);
  }

  public event EventHandler? Saved;

  private void CancelClicked(object? sender, EventArgs e)
  {
    _ = Navigation.PopAsync();
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
      Saved?.Invoke(this, EventArgs.Empty);
      await Navigation.PopAsync();
    }
    catch (Exception ex)
    {
      AppErrorPresenter.Show(ex);
    }
  }
}
