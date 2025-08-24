using Android.Content;

namespace ShadersCamera;

public partial class NativeTasks
{
    public static void OpenSystemSettings()
    {
        var intent = new Intent(Android.Provider.Settings.ActionApplicationDetailsSettings,
            Android.Net.Uri.Parse("package:" + Context.PackageName));
        Context.StartActivity(intent);
    }

    public static Context Context
    {
        get
        {
            return Platform.CurrentActivity ?? Android.App.Application.Context;
        }
    }
}