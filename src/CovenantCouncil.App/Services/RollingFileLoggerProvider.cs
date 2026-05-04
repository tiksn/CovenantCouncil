using Microsoft.Extensions.Logging;

namespace CovenantCouncil.App.Services;

public sealed class RollingFileLoggerProvider(RollingFileLogSink sink) : ILoggerProvider
{
  public ILogger CreateLogger(string categoryName)
  {
    return new RollingFileLogger(sink, categoryName);
  }

  public void Dispose()
  {
    sink.Flush();
  }

  private sealed class RollingFileLogger(RollingFileLogSink sink, string categoryName) : ILogger
  {
    public IDisposable? BeginScope<TState>(TState state)
      where TState : notnull
    {
      return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
      return logLevel != LogLevel.None;
    }

    public void Log<TState>(
      LogLevel logLevel,
      EventId eventId,
      TState state,
      Exception? exception,
      Func<TState, Exception?, string> formatter)
    {
      if (!IsEnabled(logLevel))
      {
        return;
      }

      sink.Write(logLevel, categoryName, eventId, formatter(state, exception), exception);
    }
  }
}
