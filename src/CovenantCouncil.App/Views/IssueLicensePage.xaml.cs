using CovenantCouncil.App.Services;
using CovenantCouncil.ViewModels.Licenses;
using System.Reactive.Threading.Tasks;

namespace CovenantCouncil.App.Views;

public partial class IssueLicensePage : ContentPage
{
  private readonly IssueLicenseViewModel viewModel;

  public IssueLicensePage(IssueLicenseViewModel viewModel)
  {
    InitializeComponent();
    this.viewModel = viewModel;
    BindingContext = this.viewModel;
    ViewModelErrorObserver.Observe(this.viewModel);
    Loaded += (_, _) => _ = LoadAsync();
  }

  public event EventHandler? Issued;

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
    _ = Navigation.PopAsync();
  }

  private void PickPfxClicked(object? sender, EventArgs e)
  {
    _ = PickPfxAsync();
  }

  private async Task PickPfxAsync()
  {
    try
    {
      var result = await FilePicker.Default.PickAsync(new PickOptions
      {
        PickerTitle = "Select PFX signing certificate"
      });

      if (result is not null)
      {
        viewModel.PfxPath = result.FullPath;
      }
    }
    catch (Exception ex)
    {
      AppErrorPresenter.Show(ex);
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
      await viewModel.Issue.Execute().ToTask();
      Issued?.Invoke(this, EventArgs.Empty);
      await Navigation.PopAsync();
    }
    catch (Exception ex)
    {
      AppErrorPresenter.Show(ex);
    }
  }
}
