using System.Text;
using Microsoft.Extensions.Logging;

namespace CovenantCouncil.App.Services;

public sealed class RollingFileLogSink : IDisposable
{
  private const long MaxFileBytes = 1_048_576;
  private const int MaxFiles = 10;
  private readonly object gate = new();
  private readonly string logDirectory;
  private StreamWriter? writer;
  private string currentPath = "";

  public RollingFileLogSink(string logDirectory)
  {
    this.logDirectory = logDirectory;
    Directory.CreateDirectory(logDirectory);
    OpenWriter();
  }

  public string CurrentPath
  {
    get
    {
      lock (gate)
      {
        return currentPath;
      }
    }
  }

  public void Write(LogLevel level, string category, EventId eventId, string message, Exception? exception)
  {
    lock (gate)
    {
      RollIfNeeded();

      writer?.Write(DateTimeOffset.UtcNow.ToString("O", System.Globalization.CultureInfo.InvariantCulture));
      writer?.Write(" [");
      writer?.Write(level);
      writer?.Write("] ");
      writer?.Write(category);
      if (eventId.Id != 0 || !string.IsNullOrWhiteSpace(eventId.Name))
      {
        writer?.Write(" ");
        writer?.Write(eventId);
      }

      writer?.Write(": ");
      writer?.WriteLine(message);

      if (exception is not null)
      {
        writer?.WriteLine(exception);
      }

      writer?.Flush();
    }
  }

  public void Flush()
  {
    lock (gate)
    {
      writer?.Flush();
      writer?.BaseStream.Flush();
    }
  }

  public IReadOnlyList<string> FindCrashEntries()
  {
    lock (gate)
    {
      Flush();
      return Directory.EnumerateFiles(logDirectory, "covenant-council-*.log")
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
          .Select(line => $"{Path.GetFileName(path)}: {line}"))
        .ToList();
    }
  }

  public void Dispose()
  {
    lock (gate)
    {
      writer?.Dispose();
      writer = null;
    }
  }

  private void OpenWriter()
  {
    currentPath = Path.Combine(logDirectory, $"covenant-council-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.log");
    writer = new StreamWriter(new FileStream(currentPath, FileMode.Append, FileAccess.Write, FileShare.Read, 4096, FileOptions.WriteThrough), Encoding.UTF8)
    {
      AutoFlush = true
    };
    TrimOldFiles();
  }

  private void RollIfNeeded()
  {
    if (writer is null || writer.BaseStream.Length < MaxFileBytes)
    {
      return;
    }

    writer.Dispose();
    OpenWriter();
  }

  private void TrimOldFiles()
  {
    foreach (var oldFile in Directory.EnumerateFiles(logDirectory, "covenant-council-*.log")
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
