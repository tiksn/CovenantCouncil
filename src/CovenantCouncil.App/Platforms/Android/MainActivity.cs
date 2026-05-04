using Android.App;
using Android.Content;
using Android.Content.PM;

namespace CovenantCouncil.App;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public sealed class MainActivity : MauiAppCompatActivity
{
  protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
  {
    base.OnActivityResult(requestCode, resultCode, data);
    (IPlatformApplication.Current?.Services.GetService(typeof(Services.IDatabaseFilePicker)) as Services.DatabaseFilePicker)
      ?.OnActivityResult(requestCode, resultCode, data);
  }
}
