using CovenantCouncil.UseCases.Licenses;
using Microsoft.Extensions.DependencyInjection;

namespace CovenantCouncil.Infrastructure.Licenses;

public sealed class LicenseCatalog(IServiceProvider serviceProvider) : ILicenseCatalog
{
  public IReadOnlyList<LicenseDescriptorSummary> GetDescriptors()
  {
    var descriptors = serviceProvider.GetServices<object>()
      .Where(service => service.GetType().GetInterfaces().Any(i => i.Name.StartsWith("ILicenseDescriptor", StringComparison.Ordinal)))
      .Select(service =>
      {
        var type = service.GetType();
        var name = type.GetProperty("Name")?.GetValue(service)?.ToString() ?? type.Name;
        var discriminator = type.GetProperty("Discriminator")?.GetValue(service)?.ToString() ?? type.FullName ?? type.Name;
        return new LicenseDescriptorSummary(discriminator, name);
      })
      .DistinctBy(x => x.Discriminator)
      .OrderBy(x => x.Name)
      .ToList();

    return descriptors;
  }
}
