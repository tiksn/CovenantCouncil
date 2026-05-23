using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace CovenantCouncil.App.Services;

public sealed class AppDiagnosticsService(
  RollingFileLogSink logSink,
  ILogger<AppDiagnosticsService> logger,
  IServiceProvider serviceProvider)
{
  private const int FlushTimeoutMilliseconds = 5_000;

  public string CurrentLogPath => logSink.CurrentPath;

  public void RecordCrash(string message, Exception exception, bool isTerminating)
  {
    logger.LogCritical(exception, "{Message}. IsTerminating={IsTerminating}", message, isTerminating);
    FlushBeforeShutdown();
  }

  public void RecordUnhandledTaskException(Exception exception)
  {
    logger.LogError(exception, "Unobserved task exception was observed and marked handled.");
    FlushBeforeShutdown();
  }

  public void FlushBeforeShutdown()
  {
    try
    {
      serviceProvider.GetService<TracerProvider>()?.ForceFlush(FlushTimeoutMilliseconds);
    }
    catch (Exception ex)
    {
      logSink.Write(LogLevel.Error, nameof(AppDiagnosticsService), default, "OpenTelemetry trace flush failed.", ex);
    }

    try
    {
      serviceProvider.GetService<MeterProvider>()?.ForceFlush(FlushTimeoutMilliseconds);
    }
    catch (Exception ex)
    {
      logSink.Write(LogLevel.Error, nameof(AppDiagnosticsService), default, "OpenTelemetry metric flush failed.", ex);
    }

    logSink.Flush();
  }

  public IReadOnlyList<string> FindCrashEntries()
  {
    return logSink.FindCrashEntries();
  }
}
