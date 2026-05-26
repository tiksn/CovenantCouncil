using Foundation;
using UIKit;

namespace CovenantCouncil.App.Services;

public sealed partial class DatabaseFilePicker
{
  public async partial Task<string?> PickCreatePathAsync(CancellationToken cancellationToken)
  {
    var fileName = Path.Combine(FileSystem.CacheDirectory, "covenant.ccdb");
    await File.WriteAllBytesAsync(fileName, [], cancellationToken);
    var picker = new UIDocumentPickerViewController([NSUrl.FromFilename(fileName)], true);
    var controller = Platform.GetCurrentUIViewController() ?? throw new InvalidOperationException("No iOS view controller is available.");
    var completion = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
    picker.DidPickDocumentAtUrls += (_, args) => completion.TrySetResult(args.Urls.FirstOrDefault()?.Path);
    picker.WasCancelled += (_, _) => completion.TrySetResult(null);
    await controller.PresentViewControllerAsync(picker, true);
    await using var cancellationRegistration = cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken));
    return await completion.Task.ConfigureAwait(false);
  }
}
