using System.Windows.Input;
using DrawnUi.Camera;
using DrawnUi.Infrastructure;
using ShadersCamera.Views.ShadersCamera;

namespace ShadersCamera.Controls
{
    public class CameraWithEffects : SkiaCamera
    {

        public CameraWithEffects()
        {
            InitializeAvailableShaders();
        }

        protected override void OnDisplayReady()
        {
            base.OnDisplayReady();

            //Display.UseCache = SkiaCacheType.GPU; 

            InitializeEffects();
        }

        protected override void Paint(DrawingContext ctx)
        {
            base.Paint(ctx);

            FrameAquired = false;
        }

        /// <summary>
        /// Initialize camera effects - sets starting effect
        /// </summary>
        public void InitializeEffects()
        {
            InitializeAvailableShaders();

            SetEffect(SkiaImageEffect.Custom); // <=== DEFAULT AT STARTUP
        }

        public void SetEffect(SkiaImageEffect effect)
        {
            if (Display == null)
            {
                return;
            }

            if (effect == SkiaImageEffect.Custom)
            {
                if (_shader == null)
                {
                    _shader = new SkiaShaderEffect()
                    {
                        ShaderSource = "Shaders/Camera/sketch.sksl",
                        //FilterMode = SKFilterMode.Linear <== it's default
                    };
                }

                if (_shader != null && !VisualEffects.Contains(_shader))
                {
                    VisualEffects.Add(_shader);
                }
            }
            else
            {
                if (_shader != null && VisualEffects.Contains(_shader))
                {
                    VisualEffects.Remove(_shader);
                }
            }
            Effect = effect;
        }

        public readonly List<SkiaImageEffect> AvailableEffects = new List<SkiaImageEffect>()
        {
            SkiaImageEffect.Custom,
            SkiaImageEffect.Sepia,
            SkiaImageEffect.BlackAndWhite,
            SkiaImageEffect.Pastel,
            SkiaImageEffect.None,
        };

        private static string path = @"Shaders\Camera";
        private static List<string> _shaders;

        static void InitializeAvailableShaders()
        {
            if (_shaders == null)
            {
                _shaders = Files.ListAssets(path);

            }
        }


        private SkiaShaderEffect _shader;

        //protected override SkiaImage CreatePreview()
        //{
        //    var display = base.CreatePreview();


        //}

        //public class CameraDisplayWrapper : SkiaImage
        //{
        //    public override SKImage CachedImage
        //    {
        //        get
        //        {
        //            return LoadedSource?.Image;
        //        }
        //    }
        //}


        public void ChangeShaderCode(string code)
        {
            if (Display == null || _shader==null)
            {
                return;
            }

            _shader.ShaderCode = code;
        }

        public void SetCustomShader(string shaderFilename)
        {
            if (Display == null)
            {
                return;
            }

            // Remove existing shader if any
            if (_shader != null && VisualEffects.Contains(_shader))
            {
                VisualEffects.Remove(_shader);
            }

            // Create new shader with the specified filename
            _shader = new SkiaShaderEffect()
            {
                ShaderSource = shaderFilename,
                //FilterMode = SKFilterMode.Linear <== it's default
            };

            // Add the new shader
            if (_shader != null && !VisualEffects.Contains(_shader))
            {
                VisualEffects.Add(_shader);
            }

            // Set effect to custom to enable shader
            Effect = SkiaImageEffect.Custom;
        }

        public ICommand CommandEditShader
        {
            get
            {
                return new Command(async (context) =>
                {
                    //just change currently running shader code, no matter what exactly we longpressed
                    if (_shader != null)
                    {
                        var code = _shader.LoadedCode;
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            var page = new ShaderEditorPage(code, CallBackSetSelectedShaderCode);
                            OpenPageInNewWindow(page, "Shader Editor");
                        });
                    }
                });
            }
        }

        void CallBackSetSelectedShaderCode(string code)
        {
            ChangeShaderCode(code);
        }

        public static void OpenPageInNewWindow(ContentPage page, string title = "New Window")
        {
#if WINDOWS || MACCATALYST
            var window = new Window(page) { Title = title };
            
            // Set window size and position for shader editor
            if (page is ShaderEditorPage)
            {
                window.Width = 800;
                window.Height = 800;
                window.MinimumWidth = 600;
                window.MinimumHeight = 400;
                
                // Position to the right of the main window
                var mainWindow = Application.Current?.Windows?.FirstOrDefault();
                if (mainWindow != null)
                {
                    window.X = mainWindow.X + mainWindow.Width + 20; // 20px gap
                    window.Y = mainWindow.Y; // Same vertical position
                }
            }
            
            Application.Current.OpenWindow(window);
#endif
        }


    }
}
