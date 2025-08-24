using Foundation;
using UIKit;

namespace ShadersCamera;

public partial class NativeTasks
{
    public static void OpenSystemSettings()
    {
        var url = new NSUrl($"prefs:root=NOTIFICATIONS_ID&path={NSBundle.MainBundle.BundleIdentifier}");
        var res = UIApplication.SharedApplication.OpenUrl(url);
        if (!res)
        {
            url = new NSUrl("app-settings:");
            res = UIApplication.SharedApplication.OpenUrl(url);
        }
    }
}