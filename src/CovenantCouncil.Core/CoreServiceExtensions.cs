using Microsoft.Extensions.DependencyInjection;
using TIKSN.DependencyInjection;

namespace CovenantCouncil.Core;

public static class CoreServiceExtensions
{
  public static IServiceCollection AddCoreServices(
    this IServiceCollection services)
  {
    services.AddFrameworkCore();

    Fossa.Licensing.ServiceCollectionExtensions.AddLicense(services);
    VerdantApp.Licensing.ServiceCollectionExtensions.AddLicense(services);

    return services;
  }
}
