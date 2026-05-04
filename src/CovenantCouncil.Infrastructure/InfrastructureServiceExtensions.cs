using CovenantCouncil.Infrastructure.Certificates;
using CovenantCouncil.Infrastructure.Data;
using CovenantCouncil.Infrastructure.Licenses;
using CovenantCouncil.Infrastructure.Parties;
using CovenantCouncil.Infrastructure.Security;
using CovenantCouncil.Infrastructure.Settings;
using CovenantCouncil.UseCases.Abstractions;
using CovenantCouncil.UseCases.Certificates;
using CovenantCouncil.UseCases.Licenses;
using CovenantCouncil.UseCases.Parties;
using CovenantCouncil.UseCases.Settings;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CovenantCouncil.Infrastructure;

public static class InfrastructureServiceExtensions
{
  public static IServiceCollection AddInfrastructureServices(
    this IServiceCollection services)
  {
    services.AddSingleton<IDatabasePathProvider, DatabasePathProvider>();
    services.AddSingleton<IDbContextFactory<CovenantCouncilDbContext>, CovenantCouncilDbContextFactory>();
    services.AddSingleton<IPortableProtectionService, PortableProtectionService>();
    services.AddSingleton<IDataProtector, PasswordDataProtector>();
    services.AddSingleton<IDatabaseSessionService, DatabaseSessionService>();
    services.AddSingleton<IApplicationSettingsService, FileApplicationSettingsService>();
    services.AddSingleton<IRecentDatabaseService, RecentDatabaseService>();
    services.AddScoped<IPartyService, PartyService>();
    services.AddScoped<ICertificateService, CertificateService>();
    services.AddScoped<ILicenseCatalog, LicenseCatalog>();
    services.AddScoped<ILicenseService, LicenseService>();

    return services;
  }
}
