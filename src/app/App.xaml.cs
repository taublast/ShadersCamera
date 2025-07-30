using System.Reflection;
using ShadersCamera.Views;

namespace ShadersCamera
{
    public partial class App : Application
    {
        public App()
        {
            Super.SetLocale("en");

            InitializeComponent();

            MainPage = new AppShell();
        }

        public static App Instance => App.Current as App;
    }

}
