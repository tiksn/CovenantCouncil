using CovenantCouncil.ViewModels;

namespace CovenantCouncil.App.Services;

public static class ViewModelErrorObserver
{
  public static void Observe(ViewModelBase viewModel)
  {
    viewModel.PropertyChanged += (_, args) =>
    {
      if (args.PropertyName == nameof(ViewModelBase.ErrorMessage)
          && !string.IsNullOrWhiteSpace(viewModel.ErrorMessage))
      {
        AppErrorPresenter.ShowMessage(viewModel.ErrorMessage);
      }
    };
  }
}
