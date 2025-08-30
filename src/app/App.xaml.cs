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

            MainPage = new MainCameraPageFluent();
        }

        public static App Instance => App.Current as App;

        protected override void OnSleep()
        {
            UserSettings.Save();

            base.OnSleep();
        }

    }

}
