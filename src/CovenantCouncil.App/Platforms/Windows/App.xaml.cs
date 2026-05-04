using Microsoft.UI.Xaml;

namespace CovenantCouncil.App.WinUI;

public partial class App : MauiWinUIApplication
{
  public App()
  {
    InitializeComponent();
    UnhandledException += (_, args) =>
    {
      args.Handled = true;
      global::CovenantCouncil.App.Services.AppDiagnostics.Current?.RecordCrash(
        "Unhandled WinUI exception",
        args.Exception,
        isTerminating: false);
      global::CovenantCouncil.App.Services.AppErrorPresenter.Show(args.Exception);
    };
  }

  protected override MauiApp CreateMauiApp()
  {
    return MauiProgram.CreateMauiApp();
  }
}
