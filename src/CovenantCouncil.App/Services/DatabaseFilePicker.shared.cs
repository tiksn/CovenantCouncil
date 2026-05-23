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
        [DevicePlatform.WinUI] = [".ccdb"],
        [DevicePlatform.MacCatalyst] = ["ccdb"],
        [DevicePlatform.iOS] = ["public.data"],
        [DevicePlatform.Android] = ["application/octet-stream"]
      })
    });

    return result?.FullPath;
  }

  public partial Task<string?> PickCreatePathAsync(CancellationToken cancellationToken = default);
}
