using Android.App;
using Android.Content;

namespace CovenantCouncil.App.Services;

public sealed partial class DatabaseFilePicker
{
  private const int CreateDocumentRequestCode = 8301;
  private TaskCompletionSource<string?>? _createTask;

  public async partial Task<string?> PickCreatePathAsync(CancellationToken cancellationToken)
  {
    var activity = Platform.CurrentActivity ?? throw new InvalidOperationException("No Android activity is available.");
    _createTask = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
    var intent = new Intent(Intent.ActionCreateDocument);
    intent.AddCategory(Intent.CategoryOpenable);
    intent.SetType("application/octet-stream");
    intent.PutExtra(Intent.ExtraTitle, "covenant.ccdb");
    activity.StartActivityForResult(intent, CreateDocumentRequestCode);
    await using var cancellationRegistration = cancellationToken.Register(() => _createTask.TrySetCanceled(cancellationToken));
#pragma warning disable VSTHRD003
    // Android returns the document picker result through OnActivityResult.
    return await _createTask.Task.ConfigureAwait(false);
#pragma warning restore VSTHRD003
  }

  internal void OnActivityResult(int requestCode, Result resultCode, Intent? data)
  {
    if (requestCode != CreateDocumentRequestCode)
    {
      return;
    }

    _createTask?.TrySetResult(resultCode == Result.Ok ? data?.Data?.ToString() : null);
    _createTask = null;
  }
}
