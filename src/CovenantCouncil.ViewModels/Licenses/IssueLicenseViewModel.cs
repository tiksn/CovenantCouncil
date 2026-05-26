using System.Collections.ObjectModel;
using System.Globalization;
using System.Reactive;
using CovenantCouncil.UseCases.Licenses;
using CovenantCouncil.UseCases.Parties;
using ReactiveUI;

namespace CovenantCouncil.ViewModels.Licenses;

public sealed class IssueLicenseViewModel : ViewModelBase
{
  private readonly ILicenseCatalog _licenseCatalog;
  private readonly ILicenseService _licenseService;
  private readonly IPartyService _partyService;
  private string _companyId = "";
  private string _environmentName = "";
  private string _maximumBranchCount = "";
  private string _maximumCompanyCount = "";
  private string _maximumDepartmentCount = "";
  private string _maximumEmployeeCount = "";
  private DateTime _notAfterDate = DateTime.Today.AddYears(1);
  private TimeSpan _notAfterTime = TimeSpan.Zero;
  private DateTime _notBeforeDate = DateTime.Today;
  private TimeSpan _notBeforeTime = TimeSpan.Zero;
  private string _pfxPassword = "";
  private string _pfxPath = "";
  private LicenseDescriptorSummary? _selectedDescriptor;
  private PartySummary? _selectedLicensee;
  private PartySummary? _selectedLicensor;
  private string _systemId = "";

  public IssueLicenseViewModel(
    ILicenseCatalog licenseCatalog,
    ILicenseService licenseService,
    IPartyService partyService)
  {
    _licenseCatalog = licenseCatalog;
    _licenseService = licenseService;
    _partyService = partyService;
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
    get => _selectedDescriptor;
    set
    {
      this.RaiseAndSetIfChanged(ref _selectedDescriptor, value);
      this.RaisePropertyChanged(nameof(IsFossaCompany));
      this.RaisePropertyChanged(nameof(IsFossaSystem));
      this.RaisePropertyChanged(nameof(IsVerdantSystem));
      this.RaisePropertyChanged(nameof(RequiresCountries));
      this.RaisePropertyChanged(nameof(RequiresEnvironmentName));
    }
  }

  public PartySummary? SelectedLicensor
  {
    get => _selectedLicensor;
    set => this.RaiseAndSetIfChanged(ref _selectedLicensor, value);
  }

  public PartySummary? SelectedLicensee
  {
    get => _selectedLicensee;
    set => this.RaiseAndSetIfChanged(ref _selectedLicensee, value);
  }

  public DateTime NotBeforeDate
  {
    get => _notBeforeDate;
    set => this.RaiseAndSetIfChanged(ref _notBeforeDate, value);
  }

  public TimeSpan NotBeforeTime
  {
    get => _notBeforeTime;
    set => this.RaiseAndSetIfChanged(ref _notBeforeTime, value);
  }

  public DateTime NotAfterDate
  {
    get => _notAfterDate;
    set => this.RaiseAndSetIfChanged(ref _notAfterDate, value);
  }

  public TimeSpan NotAfterTime
  {
    get => _notAfterTime;
    set => this.RaiseAndSetIfChanged(ref _notAfterTime, value);
  }

  public string PfxPath
  {
    get => _pfxPath;
    set => this.RaiseAndSetIfChanged(ref _pfxPath, value);
  }

  public string PfxPassword
  {
    get => _pfxPassword;
    set => this.RaiseAndSetIfChanged(ref _pfxPassword, value);
  }

  public string SystemId
  {
    get => _systemId;
    set => this.RaiseAndSetIfChanged(ref _systemId, value);
  }

  public string EnvironmentName
  {
    get => _environmentName;
    set => this.RaiseAndSetIfChanged(ref _environmentName, value);
  }

  public string CompanyId
  {
    get => _companyId;
    set => this.RaiseAndSetIfChanged(ref _companyId, value);
  }

  public string MaximumBranchCount
  {
    get => _maximumBranchCount;
    set => this.RaiseAndSetIfChanged(ref _maximumBranchCount, value);
  }

  public string MaximumEmployeeCount
  {
    get => _maximumEmployeeCount;
    set => this.RaiseAndSetIfChanged(ref _maximumEmployeeCount, value);
  }

  public string MaximumDepartmentCount
  {
    get => _maximumDepartmentCount;
    set => this.RaiseAndSetIfChanged(ref _maximumDepartmentCount, value);
  }

  public string MaximumCompanyCount
  {
    get => _maximumCompanyCount;
    set => this.RaiseAndSetIfChanged(ref _maximumCompanyCount, value);
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
    foreach (var descriptor in _licenseCatalog.GetDescriptors())
    {
      Descriptors.Add(descriptor);
    }

    SelectedDescriptor ??= Descriptors.FirstOrDefault();

    Parties.Clear();
    foreach (var party in await _partyService.ListAsync(null))
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

    await _licenseService.IssueAsync(new IssueLicenseRequest(
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
