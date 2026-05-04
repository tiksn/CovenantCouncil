using CovenantCouncil.UseCases.Abstractions;
using CovenantCouncil.UseCases.Settings;
using CovenantCouncil.ViewModels.Settings;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CovenantCouncil.FunctionalTests.ViewModels;

[Collection(ReactiveUiTestCollection.Name)]
public sealed class SettingsViewModelTests
{
  [Theory]
  [InlineData(DatabaseSelectionMode.Open)]
  [InlineData(DatabaseSelectionMode.Create)]
  [InlineData(DatabaseSelectionMode.OpenOrCreate)]
  public async Task DatabaseGate_OpenOrCreateDatabase_DispatchesSelectedMode(DatabaseSelectionMode mode)
  {
    var databaseSession = Substitute.For<IDatabaseSessionService>();
    var recentDatabases = Substitute.For<IRecentDatabaseService>();
    var viewModel = new DatabaseGateViewModel(databaseSession, recentDatabases)
    {
      DatabasePath = "C:\\data\\council.ccdb",
      Password = "password",
      SelectionMode = mode
    };

    await ViewModelTestHelpers.ExecuteIgnoringCommandExceptionAsync(viewModel.OpenOrCreateDatabase);

    if (mode == DatabaseSelectionMode.Open)
    {
      await databaseSession.Received(1).OpenAsync(viewModel.DatabasePath, viewModel.Password, Arg.Any<CancellationToken>());
    }
    else if (mode == DatabaseSelectionMode.Create)
    {
      await databaseSession.Received(1).CreateAsync(viewModel.DatabasePath, viewModel.Password, Arg.Any<CancellationToken>());
    }
    else
    {
      await databaseSession.Received(1).OpenOrCreateAsync(viewModel.DatabasePath, viewModel.Password, Arg.Any<CancellationToken>());
    }
  }

  [Fact]
  public async Task DatabaseGate_LoadRecent_PopulatesRecentPathsAndRaisesChange()
  {
    var databaseSession = Substitute.For<IDatabaseSessionService>();
    var recentDatabases = Substitute.For<IRecentDatabaseService>();
    recentDatabases.GetRecentAsync(Arg.Any<CancellationToken>())
      .Returns(Task.FromResult<IReadOnlyList<string>>(["one.ccdb", "two.ccdb"]));
    var viewModel = new DatabaseGateViewModel(databaseSession, recentDatabases);
    var changes = ViewModelTestHelpers.ObserveProperties(viewModel);

    await ViewModelTestHelpers.ExecuteAsync(viewModel.LoadRecent);

    viewModel.RecentDatabasePaths.ShouldBe(["one.ccdb", "two.ccdb"]);
    changes.ShouldContain(nameof(DatabaseGateViewModel.RecentDatabasePaths));
  }

  [Fact]
  public async Task DatabaseGate_CommandFailure_SetsErrorMessageAndStopsBusy()
  {
    var databaseSession = Substitute.For<IDatabaseSessionService>();
    databaseSession.OpenAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
      .Returns(_ => Task.FromException(new InvalidOperationException("open failed")));
    var viewModel = new DatabaseGateViewModel(databaseSession, Substitute.For<IRecentDatabaseService>())
    {
      SelectionMode = DatabaseSelectionMode.Open
    };

    await ViewModelTestHelpers.ExecuteIgnoringCommandExceptionAsync(viewModel.OpenOrCreateDatabase);

    viewModel.ErrorMessage.ShouldBe("open failed");
    viewModel.IsBusy.ShouldBeFalse();
  }

  [Fact]
  public async Task ApplicationSettings_LoadAndSave_RoundTripsPublicState()
  {
    var settingsService = Substitute.For<IApplicationSettingsService>();
    settingsService.GetAsync(Arg.Any<CancellationToken>())
      .Returns(Task.FromResult(new ApplicationSettings("http://otel.example", ["a.ccdb", "b.ccdb"])));
    var viewModel = new ApplicationSettingsViewModel(settingsService);
    var changes = ViewModelTestHelpers.ObserveProperties(viewModel);

    await ViewModelTestHelpers.ExecuteAsync(viewModel.Load);

    viewModel.OtlpEndpoint.ShouldBe("http://otel.example");
    viewModel.RecentDatabasePaths.ShouldBe(["a.ccdb", "b.ccdb"]);
    changes.ShouldContain(nameof(ApplicationSettingsViewModel.OtlpEndpoint));
    changes.ShouldContain(nameof(ApplicationSettingsViewModel.RecentDatabasePaths));

    viewModel.OtlpEndpoint = "http://changed.example";
    await ViewModelTestHelpers.ExecuteAsync(viewModel.Save);

    await settingsService.Received(1).SaveAsync(
      Arg.Is<ApplicationSettings>(x =>
        x.OtlpEndpoint == "http://changed.example" &&
        x.RecentDatabasePaths.SequenceEqual(new[] { "a.ccdb", "b.ccdb" })),
      Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task ApplicationSettings_CommandFailure_SetsErrorMessageAndStopsBusy()
  {
    var settingsService = Substitute.For<IApplicationSettingsService>();
    settingsService.GetAsync(Arg.Any<CancellationToken>())
      .Returns(_ => Task.FromException<ApplicationSettings>(new InvalidOperationException("settings failed")));
    var viewModel = new ApplicationSettingsViewModel(settingsService);

    await ViewModelTestHelpers.ExecuteIgnoringCommandExceptionAsync(viewModel.Load);

    viewModel.ErrorMessage.ShouldBe("settings failed");
    viewModel.IsBusy.ShouldBeFalse();
  }
}
