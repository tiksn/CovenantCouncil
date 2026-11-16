namespace CovenantCouncil.App.Services;

public sealed partial class DatabaseFilePicker : IDatabaseFilePicker
{
  public async Task<string?> PickOpenPathAsync(CancellationToken cancellationToken = default)
  {
    var result = await FilePicker.Default.PickAsync(new PickOptions
    {
      PickerTitle = "Pick Covenant Council database",
      FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
      {
        [DevicePlatform.WinUI] = new[] { ".ccdb" },
        [DevicePlatform.MacCatalyst] = new[] { "ccdb" },
        [DevicePlatform.iOS] = new[] { "public.data" },
        [DevicePlatform.Android] = new[] { "application/octet-stream" }
      })
    });

    return result?.FullPath;
  }

  public partial Task<string?> PickCreatePathAsync(CancellationToken cancellationToken = default);
}
