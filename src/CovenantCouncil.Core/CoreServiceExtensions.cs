using Microsoft.Extensions.DependencyInjection;
using TIKSN.DependencyInjection;

namespace CovenantCouncil.Core;

public static class CoreServiceExtensions
{
  public static IServiceCollection AddCoreServices(
    this IServiceCollection services)
  {
    services.AddFrameworkCore();

    return services;
  }
}
