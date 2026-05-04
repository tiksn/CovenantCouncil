using Microsoft.Extensions.DependencyInjection;
#if !ANDROID && !IOS && !MACCATALYST
using TIKSN.DependencyInjection;
#endif

namespace CovenantCouncil.Core;

public static class CoreServiceExtensions
{
  public static IServiceCollection AddCoreServices(
    this IServiceCollection services)
  {
#if !ANDROID && !IOS && !MACCATALYST
    services.AddFrameworkCore();

    Fossa.Licensing.ServiceCollectionExtensions.AddLicense(services);
    VerdantApp.Licensing.ServiceCollectionExtensions.AddLicense(services);
#endif

    return services;
  }
}
