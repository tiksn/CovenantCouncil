using Microsoft.Extensions.Logging;

namespace CovenantCouncil.App;

public partial class App : Application
{
  private readonly AppShell _shell;

  public App(AppShell shell, Services.AppDiagnosticsService diagnostics, ILogger<App> logger)
  {
    InitializeComponent();
    _shell = shell;
    Services.AppDiagnostics.Current = diagnostics;
    logger.LogInformation("Covenant Council app started. LogFile={LogFile}", diagnostics.CurrentLogPath);
    foreach (var crashEntry in diagnostics.FindCrashEntries())
    {
      var sanitizedCrashEntry = crashEntry
        .Replace("[Critical]", "Critical", StringComparison.OrdinalIgnoreCase)
        .Replace("[Error]", "Error", StringComparison.OrdinalIgnoreCase)
        .Replace("Unhandled WinUI exception", "Prior WinUI exception", StringComparison.OrdinalIgnoreCase)
        .Replace("Unhandled AppDomain exception", "Prior AppDomain exception", StringComparison.OrdinalIgnoreCase);
      logger.LogWarning("Crash entry found in rolling logs: {CrashEntry}", sanitizedCrashEntry);
    }

    AppDomain.CurrentDomain.UnhandledException += (_, args) =>
    {
      if (args.ExceptionObject is Exception exception)
      {
        diagnostics.RecordCrash("Unhandled AppDomain exception", exception, args.IsTerminating);
        Services.AppErrorPresenter.Show(exception);
      }
    };
    TaskScheduler.UnobservedTaskException += (_, args) =>
    {
      args.SetObserved();
      diagnostics.RecordUnhandledTaskException(args.Exception);
      Services.AppErrorPresenter.Show(args.Exception);
    };
  }

  protected override Window CreateWindow(IActivationState? activationState)
  {
    return new Window(_shell);
  }
}
