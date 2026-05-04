using Microsoft.EntityFrameworkCore;

namespace CovenantCouncil.Infrastructure.Data;

public sealed class CovenantCouncilDbContextFactory(IDatabasePathProvider databasePathProvider) : IDbContextFactory<CovenantCouncilDbContext>
{
  public CovenantCouncilDbContext CreateDbContext()
  {
    var databasePath = databasePathProvider.DatabasePath
      ?? throw new InvalidOperationException("No Covenant Council database is open.");

    var options = new DbContextOptionsBuilder<CovenantCouncilDbContext>()
      .UseSqlite($"Data Source={databasePath}")
      .Options;

    return new CovenantCouncilDbContext(options);
  }

  public Task<CovenantCouncilDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
  {
    return Task.FromResult(CreateDbContext());
  }
}
