using Android.App;
using Android.Content.PM;

namespace ShadersCamera
{
    [Activity(Theme = "@style/MainTheme", MainLauncher = true,
        LaunchMode = LaunchMode.SingleInstance,
        ConfigurationChanges = ConfigChanges.ScreenSize
                               | ConfigChanges.Orientation
                               | ConfigChanges.UiMode
                               | ConfigChanges.ScreenLayout
                               | ConfigChanges.SmallestScreenSize
                               | ConfigChanges.Density,
        ScreenOrientation = ScreenOrientation.SensorPortrait)]
    public class MainActivity : MauiAppCompatActivity
    {

    }
}
