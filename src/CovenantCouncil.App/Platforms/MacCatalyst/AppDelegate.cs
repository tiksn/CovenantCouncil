using Foundation;

namespace CovenantCouncil.App;

[Register("AppDelegate")]
public sealed class AppDelegate : MauiUIApplicationDelegate
{
  protected override MauiApp CreateMauiApp()
  {
    return MauiProgram.CreateMauiApp();
  }
}
