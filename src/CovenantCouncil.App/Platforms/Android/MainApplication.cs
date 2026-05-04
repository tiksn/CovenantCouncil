using Android.App;
using Android.Runtime;

namespace CovenantCouncil.App;

[Application]
public sealed class MainApplication(nint handle, JniHandleOwnership ownership) : MauiApplication(handle, ownership)
{
  protected override MauiApp CreateMauiApp()
  {
    return MauiProgram.CreateMauiApp();
  }
}
