using Foundation;
using UIKit;

namespace ShadersCamera
{
    [Register("AppDelegate")]
    public class AppDelegate : MauiUIApplicationDelegate
    {
        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

        /// <summary>
        /// Under the UIKit scene lifecycle the scene's disconnect already destroyed the MAUI window when the system ends the
        /// app (a swipe in the app switcher, a kill in the background), and MAUI's own terminate handler destroys it again:
        /// IWindow.Destroying throws "Window was already destroyed" and the uncaught exception would abort the app on its way
        /// out. The window is gone either way, so that one exception is let pass; anything else still surfaces.
        /// </summary>
        public override void WillTerminate(UIApplication application)
        {
            try
            {
                base.WillTerminate(application);
            }
            catch (InvalidOperationException e) when (e.Message == "Window was already destroyed")
            {
                System.Diagnostics.Debug.WriteLine("[AppDelegate] WillTerminate: the window was already destroyed by the scene");
            }
        }

        /*
        [Export("application:supportedInterfaceOrientationsForWindow:")]
        public UIInterfaceOrientationMask GetSupportedInterfaceOrientations(UIApplication application, UIWindow forWindow)
        {
            return UIInterfaceOrientationMask.Portrait;
        }
        */
    }
}