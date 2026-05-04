using System.Collections.ObjectModel;
using System.Globalization;
using System.Reactive;
using CovenantCouncil.UseCases.Licenses;
using CovenantCouncil.UseCases.Parties;
using ReactiveUI;

namespace CovenantCouncil.ViewModels.Licenses;

public sealed class IssueLicenseViewModel : ViewModelBase
{
  private readonly ILicenseCatalog licenseCatalog;
  private readonly ILicenseService licenseService;
  private readonly IPartyService partyService;
  private string companyId = "";
  private string environmentName = "";
  private string maximumBranchCount = "";
  private string maximumCompanyCount = "";
  private string maximumDepartmentCount = "";
  private string maximumEmployeeCount = "";
  private DateTime notAfterDate = DateTime.Today.AddYears(1);
  private TimeSpan notAfterTime = TimeSpan.Zero;
  private DateTime notBeforeDate = DateTime.Today;
  private TimeSpan notBeforeTime = TimeSpan.Zero;
  private string pfxPassword = "";
  private string pfxPath = "";
  private LicenseDescriptorSummary? selectedDescriptor;
  private PartySummary? selectedLicensee;
  private PartySummary? selectedLicensor;
  private string systemId = "";

  public IssueLicenseViewModel(
    ILicenseCatalog licenseCatalog,
    ILicenseService licenseService,
    IPartyService partyService)
  {
    this.licenseCatalog = licenseCatalog;
    this.licenseService = licenseService;
    this.partyService = partyService;
    Load = ReactiveCommand.CreateFromTask(LoadAsync, outputScheduler: RxSchedulers.MainThreadScheduler);
    Issue = ReactiveCommand.CreateFromTask(IssueAsync, outputScheduler: RxSchedulers.MainThreadScheduler);
    ObserveCommandErrors(Load);
    ObserveCommandErrors(Issue);
  }

  public ObservableCollection<CountrySelectionItem> Countries { get; } = [];

  public ObservableCollection<LicenseDescriptorSummary> Descriptors { get; } = [];

  public ObservableCollection<PartySummary> Parties { get; } = [];

  public ReactiveCommand<Unit, Unit> Load { get; }

  public ReactiveCommand<Unit, Unit> Issue { get; }

  public string SerialNumber { get; } = Ulid.NewUlid().ToString();

  public LicenseDescriptorSummary? SelectedDescriptor
  {
    get => selectedDescriptor;
    set
    {
      this.RaiseAndSetIfChanged(ref selectedDescriptor, value);
      this.RaisePropertyChanged(nameof(IsFossaCompany));
      this.RaisePropertyChanged(nameof(IsFossaSystem));
      this.RaisePropertyChanged(nameof(IsVerdantSystem));
      this.RaisePropertyChanged(nameof(RequiresCountries));
      this.RaisePropertyChanged(nameof(RequiresEnvironmentName));
    }
  }

  public PartySummary? SelectedLicensor
  {
    get => selectedLicensor;
    set => this.RaiseAndSetIfChanged(ref selectedLicensor, value);
  }

  public PartySummary? SelectedLicensee
  {
    get => selectedLicensee;
    set => this.RaiseAndSetIfChanged(ref selectedLicensee, value);
  }

  public DateTime NotBeforeDate
  {
    get => notBeforeDate;
    set => this.RaiseAndSetIfChanged(ref notBeforeDate, value);
  }

  public TimeSpan NotBeforeTime
  {
    get => notBeforeTime;
    set => this.RaiseAndSetIfChanged(ref notBeforeTime, value);
  }

  public DateTime NotAfterDate
  {
    get => notAfterDate;
    set => this.RaiseAndSetIfChanged(ref notAfterDate, value);
  }

  public TimeSpan NotAfterTime
  {
    get => notAfterTime;
    set => this.RaiseAndSetIfChanged(ref notAfterTime, value);
  }

  public string PfxPath
  {
    get => pfxPath;
    set => this.RaiseAndSetIfChanged(ref pfxPath, value);
  }

  public string PfxPassword
  {
    get => pfxPassword;
    set => this.RaiseAndSetIfChanged(ref pfxPassword, value);
  }

  public string SystemId
  {
    get => systemId;
    set => this.RaiseAndSetIfChanged(ref systemId, value);
  }

  public string EnvironmentName
  {
    get => environmentName;
    set => this.RaiseAndSetIfChanged(ref environmentName, value);
  }

  public string CompanyId
  {
    get => companyId;
    set => this.RaiseAndSetIfChanged(ref companyId, value);
  }

  public string MaximumBranchCount
  {
    get => maximumBranchCount;
    set => this.RaiseAndSetIfChanged(ref maximumBranchCount, value);
  }

  public string MaximumEmployeeCount
  {
    get => maximumEmployeeCount;
    set => this.RaiseAndSetIfChanged(ref maximumEmployeeCount, value);
  }

  public string MaximumDepartmentCount
  {
    get => maximumDepartmentCount;
    set => this.RaiseAndSetIfChanged(ref maximumDepartmentCount, value);
  }

  public string MaximumCompanyCount
  {
    get => maximumCompanyCount;
    set => this.RaiseAndSetIfChanged(ref maximumCompanyCount, value);
  }

  public bool IsFossaCompany => SelectedDescriptor?.EntitlementKind == LicenseEntitlementKinds.FossaCompany;

  public bool IsFossaSystem => SelectedDescriptor?.EntitlementKind == LicenseEntitlementKinds.FossaSystem;

  public bool IsVerdantSystem => SelectedDescriptor?.EntitlementKind == LicenseEntitlementKinds.VerdantSystem;

  public bool RequiresCountries => IsFossaSystem || IsVerdantSystem;

  public bool RequiresEnvironmentName => IsFossaSystem || IsVerdantSystem;

  public int SelectedCountryCount => Countries.Count(c => c.IsSelected);

  public string SelectedCountriesSummary
  {
    get
    {
      var selectedCodes = Countries
        .Where(c => c.IsSelected)
        .Select(c => c.Code)
        .ToArray();

      if (selectedCodes.Length == 0)
      {
        return "No countries selected";
      }

      var visibleCodes = string.Join(", ", selectedCodes.Take(8));
      var remainingCount = selectedCodes.Length - 8;
      var suffix = remainingCount > 0 ? $" +{remainingCount} more" : "";
      return $"{selectedCodes.Length} selected: {visibleCodes}{suffix}";
    }
  }

  private async Task LoadAsync()
  {
    Descriptors.Clear();
    foreach (var descriptor in licenseCatalog.GetDescriptors())
    {
      Descriptors.Add(descriptor);
    }

    SelectedDescriptor ??= Descriptors.FirstOrDefault();

    Parties.Clear();
    foreach (var party in await partyService.ListAsync(null))
    {
      Parties.Add(party);
    }

    SelectedLicensor ??= Parties.FirstOrDefault();
    SelectedLicensee ??= Parties.Skip(1).FirstOrDefault() ?? Parties.FirstOrDefault();

    Countries.Clear();
    foreach (var country in GetCountries())
    {
      var countrySelection = new CountrySelectionItem(country.Key, country.Value);
      countrySelection.PropertyChanged += (_, args) =>
      {
        if (args.PropertyName == nameof(CountrySelectionItem.IsSelected))
        {
          RaiseSelectedCountryPropertiesChanged();
        }
      };
      Countries.Add(countrySelection);
    }

    RaiseSelectedCountryPropertiesChanged();
  }

  private async Task IssueAsync()
  {
    if (SelectedDescriptor is null)
    {
      throw new InvalidOperationException("Select a license descriptor.");
    }

    if (SelectedLicensor is null)
    {
      throw new InvalidOperationException("Select a licensor party.");
    }

    if (SelectedLicensee is null)
    {
      throw new InvalidOperationException("Select a licensee party.");
    }

    if (string.IsNullOrWhiteSpace(SystemId))
    {
      throw new InvalidOperationException("System ID is required.");
    }

    if (RequiresCountries && SelectedCountryCount == 0)
    {
      throw new InvalidOperationException("Select at least one country.");
    }

    var values = new Dictionary<string, string>
    {
      [LicenseEntitlementFields.SystemId] = SystemId,
      [LicenseEntitlementFields.EnvironmentName] = EnvironmentName,
      [LicenseEntitlementFields.CompanyId] = CompanyId,
      [LicenseEntitlementFields.MaximumBranchCount] = MaximumBranchCount,
      [LicenseEntitlementFields.MaximumCompanyCount] = MaximumCompanyCount,
      [LicenseEntitlementFields.MaximumDepartmentCount] = MaximumDepartmentCount,
      [LicenseEntitlementFields.MaximumEmployeeCount] = MaximumEmployeeCount,
      [LicenseEntitlementFields.CountryCodes] = string.Join(",", Countries.Where(c => c.IsSelected).Select(c => c.Code))
    };

    await licenseService.IssueAsync(new IssueLicenseRequest(
      SelectedDescriptor.Discriminator,
      SerialNumber,
      SelectedLicensor.Id,
      SelectedLicensee.Id,
      ToDateTimeOffset(NotBeforeDate, NotBeforeTime),
      ToDateTimeOffset(NotAfterDate, NotAfterTime),
      PfxPath,
      PfxPassword,
      values));
  }

  private void RaiseSelectedCountryPropertiesChanged()
  {
    this.RaisePropertyChanged(nameof(SelectedCountryCount));
    this.RaisePropertyChanged(nameof(SelectedCountriesSummary));
  }

  private static DateTimeOffset ToDateTimeOffset(DateTime date, TimeSpan time)
  {
    var localDateTime = DateTime.SpecifyKind(date.Date + time, DateTimeKind.Local);
    return new DateTimeOffset(localDateTime).ToUniversalTime();
  }

  private static IReadOnlyDictionary<string, string> GetCountries()
  {
    return CultureInfo.GetCultures(CultureTypes.SpecificCultures)
      .Select(x => new RegionInfo(x.Name))
      .Where(x => x.TwoLetterISORegionName.Length == 2)
      .DistinctBy(x => x.TwoLetterISORegionName)
      .OrderBy(x => x.TwoLetterISORegionName)
      .ToDictionary(k => k.TwoLetterISORegionName, v => v.EnglishName);
  }
}
