using Microsoft.Maui.Platform;
using Windows.Storage.Pickers;

namespace CovenantCouncil.App.Services;

public sealed partial class DatabaseFilePicker
{
  public partial async Task<string?> PickCreatePathAsync(CancellationToken cancellationToken)
  {
    var picker = new FileSavePicker
    {
      SuggestedFileName = "covenant",
      SuggestedStartLocation = PickerLocationId.DocumentsLibrary
    };
    picker.FileTypeChoices.Add("Covenant Council database", [".ccdb"]);

    var window = Application.Current?.Windows.FirstOrDefault()?.Handler?.PlatformView as MauiWinUIWindow;
    if (window is not null)
    {
      WinRT.Interop.InitializeWithWindow.Initialize(picker, window.WindowHandle);
    }

    var file = await picker.PickSaveFileAsync().AsTask(cancellationToken);
    return file?.Path;
  }
}
