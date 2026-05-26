using CovenantCouncil.UseCases.Parties;
using CovenantCouncil.ViewModels.Parties;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CovenantCouncil.FunctionalTests.ViewModels;

[Collection(ReactiveUiTestCollection.Name)]
public sealed class PartiesViewModelTests
{
  [Fact]
  public void AddParty_KindSetter_UpdatesDerivedVisibilityAndRaisesNotifications()
  {
    var viewModel = new AddPartyViewModel(Substitute.For<IPartyService>());
    var changes = ViewModelTestHelpers.ObserveProperties(viewModel);

    viewModel.Kind = PartyKind.Organization;
    viewModel.Email = "mail@example.com";
    viewModel.Website = "https://example.com";
    viewModel.FirstName = "First";
    viewModel.LastName = "Last";
    viewModel.FullName = "Full Name";
    viewModel.ShortName = "Short";
    viewModel.LongName = "Long";

    viewModel.Kinds.ShouldBe([PartyKind.Individual, PartyKind.Organization]);
    viewModel.IsIndividual.ShouldBeFalse();
    viewModel.IsOrganization.ShouldBeTrue();
    changes.ShouldContain(nameof(AddPartyViewModel.Kind));
    changes.ShouldContain(nameof(AddPartyViewModel.IsIndividual));
    changes.ShouldContain(nameof(AddPartyViewModel.IsOrganization));
    viewModel.Email.ShouldBe("mail@example.com");
    viewModel.Website.ShouldBe("https://example.com");
    viewModel.FirstName.ShouldBe("First");
    viewModel.LastName.ShouldBe("Last");
    viewModel.FullName.ShouldBe("Full Name");
    viewModel.ShortName.ShouldBe("Short");
    viewModel.LongName.ShouldBe("Long");
  }

  [Fact]
  public async Task AddParty_Save_SendsAllPublicFields()
  {
    var partyService = Substitute.For<IPartyService>();
    var viewModel = new AddPartyViewModel(partyService)
    {
      Kind = PartyKind.Organization,
      Email = "mail@example.com",
      Website = "https://example.com",
      FirstName = "First",
      LastName = "Last",
      FullName = "Full Name",
      ShortName = "Short",
      LongName = "Long"
    };

    await ViewModelTestHelpers.ExecuteIgnoringCommandExceptionAsync(viewModel.Save);

    await partyService.Received(1).SaveAsync(
      Arg.Is<UpsertParty>(x =>
        x.Id == null &&
        x.Kind == PartyKind.Organization &&
        x.Email == "mail@example.com" &&
        x.Website == "https://example.com" &&
        x.FirstName == "First" &&
        x.LastName == "Last" &&
        x.FullName == "Full Name" &&
        x.ShortName == "Short" &&
        x.LongName == "Long"),
      Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task AddParty_CommandFailure_SetsErrorMessage()
  {
    var partyService = Substitute.For<IPartyService>();
    partyService.SaveAsync(Arg.Any<UpsertParty>(), Arg.Any<CancellationToken>())
      .Returns(_ => Task.FromException<Guid>(new InvalidOperationException("save failed")));
    var viewModel = new AddPartyViewModel(partyService);

    await ViewModelTestHelpers.ExecuteIgnoringCommandExceptionAsync(viewModel.Save);

    viewModel.ErrorMessage.ShouldBe("save failed");
    viewModel.IsBusy.ShouldBeFalse();
  }

  [Fact]
  public async Task Parties_Load_UsesSelectedKindAndPopulatesCollection()
  {
    var partyService = Substitute.For<IPartyService>();
    var individual = CreateParty(PartyKind.Individual, "Ada");
    partyService.ListAsync(PartyKind.Individual, Arg.Any<CancellationToken>())
      .Returns(Task.FromResult<IReadOnlyList<PartySummary>>([individual]));
    var viewModel = new PartiesViewModel(partyService);
    var collectionChanges = ViewModelTestHelpers.ObserveCollection(viewModel.Parties);

    viewModel.SelectedKind = PartyKind.Individual;
    await Task.Delay(50);

    viewModel.Parties.ShouldBe([individual]);
    viewModel.KindFilters.ShouldBe([null, PartyKind.Individual, PartyKind.Organization]);
    collectionChanges.ShouldContain(System.Collections.Specialized.NotifyCollectionChangedAction.Add);
    await partyService.Received().ListAsync(PartyKind.Individual, Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Parties_SaveAndDelete_CallServiceAndReload()
  {
    var partyService = Substitute.For<IPartyService>();
    var party = CreateParty(PartyKind.Organization, "Org");
    partyService.ListAsync(null, Arg.Any<CancellationToken>())
      .Returns(
        Task.FromResult<IReadOnlyList<PartySummary>>([party]),
        Task.FromResult<IReadOnlyList<PartySummary>>([]));
    var viewModel = new PartiesViewModel(partyService);
    var upsert = new UpsertParty(null, PartyKind.Organization, "e", "w", null, null, null, "s", "l");

    await ViewModelTestHelpers.ExecuteAsync(viewModel.Save, upsert);
    viewModel.Parties.ShouldBe([party]);

    await ViewModelTestHelpers.ExecuteAsync(viewModel.Delete, party.Id);
    viewModel.Parties.ShouldBeEmpty();
    await partyService.Received(1).SaveAsync(upsert, Arg.Any<CancellationToken>());
    await partyService.Received(1).DeleteAsync(party.Id, Arg.Any<CancellationToken>());
    await partyService.Received(2).ListAsync(null, Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Parties_CommandFailure_SetsErrorMessage()
  {
    var partyService = Substitute.For<IPartyService>();
    partyService.DeleteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
      .Returns(_ => Task.FromException(new InvalidOperationException("delete failed")));
    var viewModel = new PartiesViewModel(partyService);

    await ViewModelTestHelpers.ExecuteIgnoringCommandExceptionAsync(viewModel.Delete, Guid.NewGuid());

    viewModel.ErrorMessage.ShouldBe("delete failed");
    viewModel.IsBusy.ShouldBeFalse();
  }

  private static PartySummary CreateParty(PartyKind kind, string name)
  {
    return new PartySummary(Guid.NewGuid(), kind, name, $"{name}@example.com", "https://example.com", DateTimeOffset.UtcNow);
  }
}
