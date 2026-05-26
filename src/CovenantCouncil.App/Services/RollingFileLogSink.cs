using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace CovenantCouncil.App.Services;

public sealed partial class RollingFileLogSink : IDisposable
{
  private const long MaxFileBytes = 1_048_576;
  private const int MaxFiles = 10;
  private readonly Lock _gate = new();
  private readonly string _logDirectory;
  private StreamWriter? _writer;
  private string _currentPath = "";

  public RollingFileLogSink(string logDirectory)
  {
    this._logDirectory = logDirectory;
    Directory.CreateDirectory(logDirectory);
    OpenWriter();
  }

  public string CurrentPath
  {
    get
    {
      lock (_gate)
      {
        return _currentPath;
      }
    }
  }

  public void Write(LogLevel level, string category, EventId eventId, string message, Exception? exception)
  {
    lock (_gate)
    {
      RollIfNeeded();

      _writer?.Write(DateTimeOffset.UtcNow.ToString("O", System.Globalization.CultureInfo.InvariantCulture));
      _writer?.Write(" [");
      _writer?.Write(level);
      _writer?.Write("] ");
      _writer?.Write(category);
      if (eventId.Id != 0 || !string.IsNullOrWhiteSpace(eventId.Name))
      {
        _writer?.Write(" ");
        _writer?.Write(eventId);
      }

      _writer?.Write(": ");
      _writer?.WriteLine(message);

      if (exception is not null)
      {
        _writer?.WriteLine(exception);
      }

      _writer?.Flush();
    }
  }

  public void Flush()
  {
    lock (_gate)
    {
      _writer?.Flush();
      _writer?.BaseStream.Flush();
    }
  }

  public IReadOnlyList<string> FindCrashEntries()
  {
    lock (_gate)
    {
      Flush();
      return [.. Directory.EnumerateFiles(_logDirectory, "covenant-council-*.log")
        .OrderByDescending(File.GetLastWriteTimeUtc)
        .Take(MaxFiles)
        .SelectMany(path => ReadLines(path)
          .Where(line =>
            !line.Contains("Crash entry found in rolling logs", StringComparison.OrdinalIgnoreCase)
            && (line.Contains("[Critical]", StringComparison.OrdinalIgnoreCase) ||
              line.Contains("[Error]", StringComparison.OrdinalIgnoreCase) ||
              line.Contains("Unhandled exception", StringComparison.OrdinalIgnoreCase) ||
              line.Contains("Unhandled WinUI exception", StringComparison.OrdinalIgnoreCase) ||
              line.Contains("Unhandled AppDomain exception", StringComparison.OrdinalIgnoreCase)))
          .Select(line => $"{Path.GetFileName(path)}: {line}"))];
    }
  }

  public void Dispose()
  {
    lock (_gate)
    {
      _writer?.Dispose();
      _writer = null;
    }
  }

  private void OpenWriter()
  {
    _currentPath = Path.Combine(_logDirectory, $"covenant-council-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.log");
    _writer = new StreamWriter(new FileStream(_currentPath, FileMode.Append, FileAccess.Write, FileShare.Read, 4096, FileOptions.WriteThrough), Encoding.UTF8)
    {
      AutoFlush = true
    };
    TrimOldFiles();
  }

  private void RollIfNeeded()
  {
    if (_writer is null || _writer.BaseStream.Length < MaxFileBytes)
    {
      return;
    }

    _writer.Dispose();
    OpenWriter();
  }

  private void TrimOldFiles()
  {
    foreach (var oldFile in Directory.EnumerateFiles(_logDirectory, "covenant-council-*.log")
      .OrderByDescending(File.GetLastWriteTimeUtc)
      .Skip(MaxFiles))
    {
      File.Delete(oldFile);
    }
  }

  private static IReadOnlyList<string> ReadLines(string path)
  {
    try
    {
      var lines = new List<string>();
      using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
      using var reader = new StreamReader(stream, Encoding.UTF8);
      while (reader.ReadLine() is { } line)
      {
        lines.Add(line);
      }

      return lines;
    }
    catch (IOException)
    {
      return [];
    }
  }
}
