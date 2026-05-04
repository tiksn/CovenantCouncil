using CovenantCouncil.UseCases.Licenses;
using CovenantCouncil.UseCases.Parties;
using CovenantCouncil.ViewModels.Licenses;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CovenantCouncil.FunctionalTests.ViewModels;

[Collection(ReactiveUiTestCollection.Name)]
public sealed class LicensesViewModelTests
{
  [Fact]
  public async Task Licenses_Load_PopulatesDescriptorsAndLicenses()
  {
    var catalog = Substitute.For<ILicenseCatalog>();
    var licenseService = Substitute.For<ILicenseService>();
    var descriptor = CreateDescriptor("fossa", "Fossa", LicenseEntitlementKinds.FossaSystem);
    var license = CreateLicense("fossa");
    catalog.GetDescriptors().Returns([descriptor]);
    licenseService.ListAsync(null, Arg.Any<CancellationToken>())
      .Returns(Task.FromResult<IReadOnlyList<LicenseSummary>>([license]));
    var viewModel = new LicensesViewModel(catalog, licenseService);

    await ViewModelTestHelpers.ExecuteAsync(viewModel.Load);

    viewModel.Descriptors.ShouldBe([descriptor]);
    viewModel.Licenses.ShouldBe([license]);
    catalog.Received(1).GetDescriptors();
    await licenseService.Received(1).ListAsync(null, Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Licenses_SelectedDescriptor_ReloadsLicensesWithoutReloadingDescriptors()
  {
    var catalog = Substitute.For<ILicenseCatalog>();
    var licenseService = Substitute.For<ILicenseService>();
    var descriptor = CreateDescriptor("verdant", "Verdant", LicenseEntitlementKinds.VerdantSystem);
    var license = CreateLicense("verdant");
    catalog.GetDescriptors().Returns([descriptor]);
    licenseService.ListAsync(null, Arg.Any<CancellationToken>())
      .Returns(Task.FromResult<IReadOnlyList<LicenseSummary>>([]));
    licenseService.ListAsync("verdant", Arg.Any<CancellationToken>())
      .Returns(Task.FromResult<IReadOnlyList<LicenseSummary>>([license]));
    var viewModel = new LicensesViewModel(catalog, licenseService);
    await ViewModelTestHelpers.ExecuteAsync(viewModel.Load);

    viewModel.SelectedDescriptor = descriptor;
    await WaitUntilAsync(() => viewModel.Licenses.Count == 1);

    viewModel.Licenses.ShouldBe([license]);
    catalog.Received(1).GetDescriptors();
    await licenseService.Received(1).ListAsync("verdant", Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Licenses_IssueDeleteExportImport_DispatchAndReload()
  {
    var catalog = Substitute.For<ILicenseCatalog>();
    var licenseService = Substitute.For<ILicenseService>();
    var request = CreateIssueRequest();
    var issued = CreateLicense(request.DescriptorDiscriminator);
    licenseService.ListAsync(null, Arg.Any<CancellationToken>())
      .Returns(
        Task.FromResult<IReadOnlyList<LicenseSummary>>([issued]),
        Task.FromResult<IReadOnlyList<LicenseSummary>>([]));
    var viewModel = new LicensesViewModel(catalog, licenseService);

    await ViewModelTestHelpers.ExecuteAsync(viewModel.Issue, request);
    viewModel.Licenses.ShouldBe([issued]);

    await ViewModelTestHelpers.ExecuteAsync(viewModel.Export, (issued.Id, "license.cclic"));
    await ViewModelTestHelpers.ExecuteAsync(viewModel.Import, "license.cclic");
    await ViewModelTestHelpers.ExecuteAsync(viewModel.Delete, issued.Id);

    await licenseService.Received(1).IssueAsync(request, Arg.Any<CancellationToken>());
    await licenseService.Received(1).ExportAsync(issued.Id, "license.cclic", Arg.Any<CancellationToken>());
    await licenseService.Received(1).ImportAsync("license.cclic", Arg.Any<CancellationToken>());
    await licenseService.Received(1).DeleteAsync(issued.Id, Arg.Any<CancellationToken>());
    await licenseService.Received(2).ListAsync(null, Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Licenses_CommandFailure_SetsErrorMessageAndStopsBusy()
  {
    var licenseService = Substitute.For<ILicenseService>();
    licenseService.ImportAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
      .Returns(_ => Task.FromException(new InvalidOperationException("import failed")));
    var viewModel = new LicensesViewModel(Substitute.For<ILicenseCatalog>(), licenseService);

    await ViewModelTestHelpers.ExecuteIgnoringCommandExceptionAsync(viewModel.Import, "bad.cclic");

    viewModel.ErrorMessage.ShouldBe("import failed");
    viewModel.IsBusy.ShouldBeFalse();
  }

  [Fact]
  public async Task IssueLicense_Load_PopulatesInitialState()
  {
    var descriptor = CreateDescriptor("fossa-system", "Fossa System", LicenseEntitlementKinds.FossaSystem);
    var licensor = CreateParty("Licensor");
    var licensee = CreateParty("Licensee");
    var catalog = Substitute.For<ILicenseCatalog>();
    catalog.GetDescriptors().Returns([descriptor]);
    var partyService = Substitute.For<IPartyService>();
    partyService.ListAsync(null, Arg.Any<CancellationToken>())
      .Returns(Task.FromResult<IReadOnlyList<PartySummary>>([licensor, licensee]));
    var viewModel = new IssueLicenseViewModel(catalog, Substitute.For<ILicenseService>(), partyService);

    await ViewModelTestHelpers.ExecuteAsync(viewModel.Load);

    viewModel.Descriptors.ShouldBe([descriptor]);
    viewModel.SelectedDescriptor.ShouldBe(descriptor);
    viewModel.Parties.ShouldBe([licensor, licensee]);
    viewModel.SelectedLicensor.ShouldBe(licensor);
    viewModel.SelectedLicensee.ShouldBe(licensee);
    viewModel.SerialNumber.ShouldNotBeNullOrWhiteSpace();
    viewModel.SystemId.ShouldBe("");
    viewModel.Countries.ShouldNotBeEmpty();
    viewModel.SelectedCountryCount.ShouldBe(0);
    viewModel.SelectedCountriesSummary.ShouldBe("No countries selected");
  }

  [Theory]
  [InlineData(LicenseEntitlementKinds.FossaCompany, true, false, false, false, false)]
  [InlineData(LicenseEntitlementKinds.FossaSystem, false, true, false, true, true)]
  [InlineData(LicenseEntitlementKinds.VerdantSystem, false, false, true, true, true)]
  public void IssueLicense_SelectedDescriptor_UpdatesDerivedProperties(
    string kind,
    bool isFossaCompany,
    bool isFossaSystem,
    bool isVerdantSystem,
    bool requiresCountries,
    bool requiresEnvironment)
  {
    var viewModel = new IssueLicenseViewModel(
      Substitute.For<ILicenseCatalog>(),
      Substitute.For<ILicenseService>(),
      Substitute.For<IPartyService>());
    var changes = ViewModelTestHelpers.ObserveProperties(viewModel);

    viewModel.SelectedDescriptor = CreateDescriptor(kind, kind, kind);

    viewModel.IsFossaCompany.ShouldBe(isFossaCompany);
    viewModel.IsFossaSystem.ShouldBe(isFossaSystem);
    viewModel.IsVerdantSystem.ShouldBe(isVerdantSystem);
    viewModel.RequiresCountries.ShouldBe(requiresCountries);
    viewModel.RequiresEnvironmentName.ShouldBe(requiresEnvironment);
    changes.ShouldContain(nameof(IssueLicenseViewModel.SelectedDescriptor));
    changes.ShouldContain(nameof(IssueLicenseViewModel.RequiresCountries));
    changes.ShouldContain(nameof(IssueLicenseViewModel.RequiresEnvironmentName));
  }

  [Fact]
  public async Task IssueLicense_CountrySelection_UpdatesSummaryAndRowState()
  {
    var viewModel = CreateLoadedIssueViewModel(LicenseEntitlementKinds.FossaSystem, out _, out _, out _);
    await ViewModelTestHelpers.ExecuteAsync(viewModel.Load);
    var changes = ViewModelTestHelpers.ObserveProperties(viewModel);
    var country = viewModel.Countries.First(c => c.Code == "US");
    var countryChanges = ViewModelTestHelpers.ObserveProperties(country);

    country.IsSelected = true;

    viewModel.SelectedCountryCount.ShouldBe(1);
    viewModel.SelectedCountriesSummary.ShouldBe("1 selected: US");
    country.SelectionStateText.ShouldBe("Selected");
    country.DisplayName.ShouldContain("US - ");
    changes.ShouldContain(nameof(IssueLicenseViewModel.SelectedCountryCount));
    changes.ShouldContain(nameof(IssueLicenseViewModel.SelectedCountriesSummary));
    countryChanges.ShouldContain(nameof(CountrySelectionItem.IsSelected));
    countryChanges.ShouldContain(nameof(CountrySelectionItem.SelectionStateText));
  }

  [Theory]
  [InlineData(null, "Select a license descriptor.")]
  [InlineData("missing-licensor", "Select a licensor party.")]
  [InlineData("missing-licensee", "Select a licensee party.")]
  [InlineData("missing-system", "System ID is required.")]
  [InlineData("missing-country", "Select at least one country.")]
  public async Task IssueLicense_ValidationFailures_SetErrorMessage(string? scenario, string expectedMessage)
  {
    var viewModel = CreateLoadedIssueViewModel(LicenseEntitlementKinds.FossaSystem, out _, out _, out _);
    await ViewModelTestHelpers.ExecuteAsync(viewModel.Load);
    viewModel.SystemId = "system";
    viewModel.Countries.First(c => c.Code == "US").IsSelected = true;

    if (scenario is null)
    {
      viewModel.SelectedDescriptor = null;
    }
    else if (scenario == "missing-licensor")
    {
      viewModel.SelectedLicensor = null;
    }
    else if (scenario == "missing-licensee")
    {
      viewModel.SelectedLicensee = null;
    }
    else if (scenario == "missing-system")
    {
      viewModel.SystemId = "";
    }
    else if (scenario == "missing-country")
    {
      foreach (var country in viewModel.Countries)
      {
        country.IsSelected = false;
      }
    }

    await ViewModelTestHelpers.ExecuteIgnoringCommandExceptionAsync(viewModel.Issue);

    viewModel.ErrorMessage.ShouldBe(expectedMessage);
  }

  [Fact]
  public async Task IssueLicense_Issue_SendsCompleteRequest()
  {
    var viewModel = CreateLoadedIssueViewModel(LicenseEntitlementKinds.FossaSystem, out var licenseService, out var licensor, out var licensee);
    await ViewModelTestHelpers.ExecuteAsync(viewModel.Load);
    viewModel.SystemId = "system-1";
    viewModel.EnvironmentName = "prod";
    viewModel.CompanyId = "42";
    viewModel.MaximumBranchCount = "10";
    viewModel.MaximumCompanyCount = "20";
    viewModel.MaximumDepartmentCount = "30";
    viewModel.MaximumEmployeeCount = "40";
    viewModel.PfxPath = "signing.pfx";
    viewModel.PfxPassword = "secret";
    viewModel.NotBeforeDate = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Local);
    viewModel.NotBeforeTime = new TimeSpan(3, 4, 0);
    viewModel.NotAfterDate = new DateTime(2027, 5, 6, 0, 0, 0, DateTimeKind.Local);
    viewModel.NotAfterTime = new TimeSpan(7, 8, 0);
    viewModel.Countries.First(c => c.Code == "US").IsSelected = true;
    viewModel.Countries.First(c => c.Code == "CA").IsSelected = true;

    await ViewModelTestHelpers.ExecuteAsync(viewModel.Issue);

    await licenseService.Received(1).IssueAsync(
      Arg.Is<IssueLicenseRequest>(request =>
        request.DescriptorDiscriminator == "descriptor" &&
        request.SerialNumber == viewModel.SerialNumber &&
        request.LicensorPartyId == licensor.Id &&
        request.LicenseePartyId == licensee.Id &&
        request.PfxPath == "signing.pfx" &&
        request.PfxPassword == "secret" &&
        request.NotBeforeUtc == ToUtc(2026, 1, 2, 3, 4) &&
        request.NotAfterUtc == ToUtc(2027, 5, 6, 7, 8) &&
        request.EntitlementValues[LicenseEntitlementFields.SystemId] == "system-1" &&
        request.EntitlementValues[LicenseEntitlementFields.EnvironmentName] == "prod" &&
        request.EntitlementValues[LicenseEntitlementFields.CompanyId] == "42" &&
        request.EntitlementValues[LicenseEntitlementFields.MaximumBranchCount] == "10" &&
        request.EntitlementValues[LicenseEntitlementFields.MaximumCompanyCount] == "20" &&
        request.EntitlementValues[LicenseEntitlementFields.MaximumDepartmentCount] == "30" &&
        request.EntitlementValues[LicenseEntitlementFields.MaximumEmployeeCount] == "40" &&
        request.EntitlementValues[LicenseEntitlementFields.CountryCodes] == "CA,US"),
      Arg.Any<CancellationToken>());
  }

  private static IssueLicenseViewModel CreateLoadedIssueViewModel(
    string entitlementKind,
    out ILicenseService licenseService,
    out PartySummary licensor,
    out PartySummary licensee)
  {
    var descriptor = CreateDescriptor("descriptor", "Descriptor", entitlementKind);
    var catalog = Substitute.For<ILicenseCatalog>();
    catalog.GetDescriptors().Returns([descriptor]);
    licensor = CreateParty("Licensor");
    licensee = CreateParty("Licensee");
    var partyService = Substitute.For<IPartyService>();
    partyService.ListAsync(null, Arg.Any<CancellationToken>())
      .Returns(Task.FromResult<IReadOnlyList<PartySummary>>([licensor, licensee]));
    licenseService = Substitute.For<ILicenseService>();
    return new IssueLicenseViewModel(catalog, licenseService, partyService);
  }

  private static LicenseDescriptorSummary CreateDescriptor(string discriminator, string name, string entitlementKind)
  {
    return new LicenseDescriptorSummary(discriminator, name, entitlementKind);
  }

  private static LicenseSummary CreateLicense(string descriptor)
  {
    return new LicenseSummary(Guid.NewGuid(), descriptor, "License", Guid.NewGuid(), "Party", "thumb", DateTimeOffset.UtcNow);
  }

  private static IssueLicenseRequest CreateIssueRequest()
  {
    return new IssueLicenseRequest(
      "descriptor",
      "serial",
      Guid.NewGuid(),
      Guid.NewGuid(),
      DateTimeOffset.UtcNow,
      DateTimeOffset.UtcNow.AddDays(1),
      "signing.pfx",
      "password",
      new Dictionary<string, string>());
  }

  private static PartySummary CreateParty(string name)
  {
    return new PartySummary(Guid.NewGuid(), PartyKind.Organization, name, null, null, DateTimeOffset.UtcNow);
  }

  private static DateTimeOffset ToUtc(int year, int month, int day, int hour, int minute)
  {
    var localDateTime = new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Local);
    return new DateTimeOffset(localDateTime).ToUniversalTime();
  }

  private static async Task WaitUntilAsync(Func<bool> condition)
  {
    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
    while (!condition())
    {
      timeout.Token.ThrowIfCancellationRequested();
      await Task.Delay(10, timeout.Token);
    }
  }
}
