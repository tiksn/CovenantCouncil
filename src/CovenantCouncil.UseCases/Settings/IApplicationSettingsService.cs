namespace CovenantCouncil.UseCases.Settings;

public interface IApplicationSettingsService
{
  Task<ApplicationSettings> GetAsync(CancellationToken cancellationToken = default);

  Task SaveAsync(ApplicationSettings settings, CancellationToken cancellationToken = default);
}
