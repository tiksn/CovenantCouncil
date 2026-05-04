using System.Reflection;
using Microsoft.Data.Sqlite;

namespace CovenantCouncil.Infrastructure.Data;

public static class SqliteSchemaInitializer
{
  public static async Task InitializeAsync(string databasePath, CancellationToken cancellationToken = default)
  {
    Directory.CreateDirectory(Path.GetDirectoryName(databasePath) ?? ".");

    await using var connection = new SqliteConnection($"Data Source={databasePath}");
    await connection.OpenAsync(cancellationToken);

    var script = await ReadScriptAsync(cancellationToken);
    await using var command = connection.CreateCommand();
    command.CommandText = script;
    await command.ExecuteNonQueryAsync(cancellationToken);
  }

  public static async Task<bool> HasProtectionMetadataAsync(string databasePath, CancellationToken cancellationToken = default)
  {
    if (!File.Exists(databasePath) || new FileInfo(databasePath).Length == 0)
    {
      return false;
    }

    await using var connection = new SqliteConnection($"Data Source={databasePath}");
    await connection.OpenAsync(cancellationToken);

    await using var command = connection.CreateCommand();
    command.CommandText = """
      SELECT CASE
        WHEN EXISTS (SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = 'protection_metadata')
        THEN (SELECT COUNT(1) FROM protection_metadata WHERE id = 1)
        ELSE 0
      END;
      """;

    var result = await command.ExecuteScalarAsync(cancellationToken);
    return Convert.ToInt32(result, System.Globalization.CultureInfo.InvariantCulture) > 0;
  }

  public static async Task<bool> HasApplicationDataAsync(string databasePath, CancellationToken cancellationToken = default)
  {
    if (!File.Exists(databasePath) || new FileInfo(databasePath).Length == 0)
    {
      return false;
    }

    await using var connection = new SqliteConnection($"Data Source={databasePath}");
    await connection.OpenAsync(cancellationToken);

    await using var command = connection.CreateCommand();
    command.CommandText = """
      SELECT
        CASE WHEN EXISTS (SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = 'parties')
          THEN (SELECT COUNT(1) FROM parties) ELSE 0 END
        +
        CASE WHEN EXISTS (SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = 'certificates')
          THEN (SELECT COUNT(1) FROM certificates) ELSE 0 END
        +
        CASE WHEN EXISTS (SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = 'licenses')
          THEN (SELECT COUNT(1) FROM licenses) ELSE 0 END;
      """;

    var result = await command.ExecuteScalarAsync(cancellationToken);
    return Convert.ToInt32(result, System.Globalization.CultureInfo.InvariantCulture) > 0;
  }

  private static async Task<string> ReadScriptAsync(CancellationToken cancellationToken)
  {
    var assembly = Assembly.GetExecutingAssembly();
    var resourceName = assembly.GetManifestResourceNames().Single(name => name.EndsWith("bootstrap.sql", StringComparison.Ordinal));
    await using var stream = assembly.GetManifestResourceStream(resourceName) ?? throw new InvalidOperationException("Embedded SQL bootstrap script was not found.");
    using var reader = new StreamReader(stream);
    return await reader.ReadToEndAsync(cancellationToken);
  }
}
