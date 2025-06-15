global using DrawnUi.Draw;
global using SkiaSharp;
using Microsoft.Extensions.Logging;

namespace ShadersCamera
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
     
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "FontText");
                    fonts.AddFont("DOM.TTF", "FontPhoto");
                    fonts.AddFont("DOMB.TTF", "FontPhotoBold");
                });

            builder.UseDrawnUi(new()
            {
                UseDesktopKeyboard = true, //will not work inside maui shell on mac
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
