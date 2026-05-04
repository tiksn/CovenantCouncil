using CovenantCouncil.ViewModels.Certificates;
using CovenantCouncil.ViewModels.Licenses;
using CovenantCouncil.ViewModels.Parties;
using CovenantCouncil.ViewModels.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace CovenantCouncil.ViewModels;

public static class ViewModelServiceExtensions
{
  public static IServiceCollection AddViewModelServices(this IServiceCollection services)
  {
    services.AddTransient<DatabaseGateViewModel>();
    services.AddTransient<ApplicationSettingsViewModel>();
    services.AddTransient<PartiesViewModel>();
    services.AddTransient<AddPartyViewModel>();
    services.AddTransient<CertificatesViewModel>();
    services.AddTransient<LicensesViewModel>();
    return services;
  }
}
