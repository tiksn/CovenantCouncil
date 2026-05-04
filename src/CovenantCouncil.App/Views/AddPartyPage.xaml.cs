using CovenantCouncil.App.Services;
using CovenantCouncil.ViewModels.Parties;
using System.Reactive.Threading.Tasks;

namespace CovenantCouncil.App.Views;

public partial class AddPartyPage : ContentPage
{
  private readonly AddPartyViewModel viewModel;

  public AddPartyPage(AddPartyViewModel viewModel)
  {
    InitializeComponent();
    this.viewModel = viewModel;
    BindingContext = this.viewModel;
    ViewModelErrorObserver.Observe(this.viewModel);
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
      await viewModel.Save.Execute().ToTask();
      Saved?.Invoke(this, EventArgs.Empty);
      await Navigation.PopAsync();
    }
    catch (Exception ex)
    {
      AppErrorPresenter.Show(ex);
    }
  }
}
