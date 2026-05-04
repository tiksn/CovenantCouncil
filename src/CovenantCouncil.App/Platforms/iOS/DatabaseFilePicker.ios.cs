using Foundation;
using UIKit;

namespace CovenantCouncil.App.Services;

public sealed partial class DatabaseFilePicker
{
  public partial async Task<string?> PickCreatePathAsync(CancellationToken cancellationToken)
  {
    var fileName = Path.Combine(FileSystem.CacheDirectory, "covenant.ccdb");
    await File.WriteAllBytesAsync(fileName, [], cancellationToken);
    var picker = new UIDocumentPickerViewController([NSUrl.FromFilename(fileName)], true);
    var controller = Platform.GetCurrentUIViewController();
    var completion = new TaskCompletionSource<string?>();
    picker.DidPickDocumentAtUrls += (_, args) => completion.TrySetResult(args.Urls.FirstOrDefault()?.Path);
    picker.WasCancelled += (_, _) => completion.TrySetResult(null);
    controller?.PresentViewController(picker, true, null);
    cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken));
    return await completion.Task;
  }
}
