namespace CovenantCouncil.App.Services;

public interface IDatabaseFilePicker
{
  Task<string?> PickOpenPathAsync(CancellationToken cancellationToken = default);

  Task<string?> PickCreatePathAsync(CancellationToken cancellationToken = default);
}
