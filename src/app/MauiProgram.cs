global using DrawnUi.Draw;
global using SkiaSharp;
using FastPopups;
using Microsoft.Extensions.Logging;

namespace ShadersCamera
{
    public static class MauiProgram
    {

        public static string ExifCameraVendor = "DrawnUI";
        public static string ExifCameraModel = "Shaders Camera";

        public static MauiApp CreateMauiApp()
        {
     
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("BalsamiqSans-Bold.ttf", "FontTextBold");
                    fonts.AddFont("BalsamiqSans-Regular.ttf", "FontText");
                });

            builder
                // https://github.com/taublast/FastPopups
                .AddPopups()

                // https://github.com/taublast/DrawnUi
                .UseDrawnUi(new()
                {
                    MobileIsFullscreen = true,
                    DesktopWindow = new()
                    {
                        Width = 500,
                        Height = 700,
                        //IsFixedSize = true //user cannot resize window
                    }
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }

    }
}
