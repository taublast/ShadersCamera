using System.Reflection;
using ShadersCamera.Helpers;
using ShadersCamera.Views;

namespace ShadersCamera
{
    public partial class App : Application
    {
        public App()
        {
            Super.SetLocale(UserSettings.Current.Lang);

            InitializeComponent();

#if ANDROID
            Super.SetNavigationBarColor(Colors.Black, Colors.Black, false);
#endif

            _mainPage = new MainCameraPageFluent();
        }

        private readonly Page _mainPage;

        /// <summary>
        /// The app's one window, made here instead of setting MainPage: under the UIKit scene lifecycle (required by the
        /// iOS 27 SDK) a window whose page was set through MainPage stays in Application.Windows after the scene destroyed
        /// it, and MAUI's iOS terminate handler then destroyed it a second time ("Window was already destroyed" on every
        /// app-switcher swipe, dotnet/maui #40438). Same behavior as MAUI's default for MainPage otherwise: an existing
        /// window is reused (Android activity recreated), else a new one shows the page made at startup.
        /// Page swaps go through Windows[0].Page, never the MainPage setter, which brings the crash back.
        /// </summary>
        protected override Window CreateWindow(IActivationState activationState)
        {
            return Windows.Count > 0 ? Windows[0] : new Window(_mainPage);
        }

        public static App Instance => App.Current as App;

        protected override void OnSleep()
        {
            UserSettings.Save();

            base.OnSleep();
        }

    }

}
