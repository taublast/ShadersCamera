using Foundation;

namespace ShadersCamera
{
    /// <summary>
    /// The UIKit scene lifecycle that the iOS / Mac Catalyst 27 SDK requires (UIApplicationSceneManifest in Info.plist names
    /// it): MAUI's own scene delegate creates the window; the app's lifecycle stays in the cross-platform Window events.
    /// </summary>
    [Register("SceneDelegate")]
    public class SceneDelegate : MauiUISceneDelegate
    {
    }
}
