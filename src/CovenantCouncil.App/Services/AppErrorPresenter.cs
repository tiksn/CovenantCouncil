namespace CovenantCouncil.App.Services;

public static class AppErrorPresenter
{
  private static int isShowing;

  public static void Show(Exception exception)
  {
    ShowMessage(exception.Message);
  }

  public static void ShowMessage(string message)
  {
    if (Interlocked.Exchange(ref isShowing, 1) == 1)
    {
      return;
    }

    MainThread.BeginInvokeOnMainThread(() => _ = ShowMessageAsync(message));
  }

  private static async Task ShowMessageAsync(string message)
  {
    try
    {
      var page = Application.Current?.Windows.FirstOrDefault()?.Page;
      if (page is not null)
      {
        await page.DisplayAlertAsync("Error", message, "OK");
      }
    }
    finally
    {
      Interlocked.Exchange(ref isShowing, 0);
    }
  }
}
