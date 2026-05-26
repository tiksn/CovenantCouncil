using System.Reactive.Threading.Tasks;
using CovenantCouncil.Core;
using CovenantCouncil.Infrastructure;
using CovenantCouncil.UseCases.Abstractions;
using CovenantCouncil.ViewModels;
using CovenantCouncil.ViewModels.Licenses;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace CovenantCouncil.IntegrationTests;

public sealed class LicensesViewModelTests
{
  [Fact]
  public async Task Load_DoesNotThrowAfterDatabaseIsOpened()
  {
    var services = new ServiceCollection()
      .AddCoreServices()
      .AddInfrastructureServices()
      .AddViewModelServices()
      .BuildServiceProvider();

    var databasePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.ccdb");
    try
    {
      var session = services.GetRequiredService<IDatabaseSessionService>();
      await session.CreateAsync(databasePath, "correct horse battery staple");

      var viewModel = services.GetRequiredService<LicensesViewModel>();
      await viewModel.Load.Execute().ToTask();

      viewModel.Licenses.ShouldBeEmpty();
    }
    finally
    {
      await services.DisposeAsync();
      SqliteConnection.ClearAllPools();
      if (File.Exists(databasePath))
      {
        File.Delete(databasePath);
      }
    }
  }
}
