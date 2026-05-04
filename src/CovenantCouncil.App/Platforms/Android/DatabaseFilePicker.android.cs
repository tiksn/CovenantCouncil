using Android.App;
using Android.Content;

namespace CovenantCouncil.App.Services;

public sealed partial class DatabaseFilePicker
{
  private const int CreateDocumentRequestCode = 8301;
  private TaskCompletionSource<string?>? createTask;

  public partial Task<string?> PickCreatePathAsync(CancellationToken cancellationToken)
  {
    var activity = Platform.CurrentActivity ?? throw new InvalidOperationException("No Android activity is available.");
    createTask = new TaskCompletionSource<string?>();
    var intent = new Intent(Intent.ActionCreateDocument);
    intent.AddCategory(Intent.CategoryOpenable);
    intent.SetType("application/octet-stream");
    intent.PutExtra(Intent.ExtraTitle, "covenant.ccdb");
    activity.StartActivityForResult(intent, CreateDocumentRequestCode);
    cancellationToken.Register(() => createTask.TrySetCanceled(cancellationToken));
    return createTask.Task;
  }

  internal void OnActivityResult(int requestCode, Result resultCode, Intent? data)
  {
    if (requestCode != CreateDocumentRequestCode)
    {
      return;
    }

    createTask?.TrySetResult(resultCode == Result.Ok ? data?.Data?.ToString() : null);
    createTask = null;
  }
}
