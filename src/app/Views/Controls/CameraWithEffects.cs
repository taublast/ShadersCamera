using DrawnUi.Camera;
using DrawnUi.Infrastructure;
using ShadersCamera.Models;
using System.Windows.Input;
using AppoMobi.Specials;

namespace ShadersCamera.Views.Controls
{
    public class CameraWithEffects : SkiaCamera
    {

        public CameraWithEffects()
        {
            NeedPermissionsSet = NeedPermissions.Camera | NeedPermissions.Gallery | NeedPermissions.Microphone;
        }

        protected override void OnDisplayReady()
        {
            base.OnDisplayReady();

            Tasks.StartDelayed(TimeSpan.FromMilliseconds(10), () =>
            {
                //do not block startup by this
                InitializeEffects();
            });
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
        private SkiaShaderEffect _shaderGlobal;
        public void ChangeShaderCode(string code)
        {
            if (Display == null || _shader==null)
            {
                return;
            }

            _shader.ShaderCode = code;
        }

        public void SetEffect(SkiaImageEffect effect)
        {
            if (Display == null)
            {
                return;
            }

            Effect = effect;
            SetCustomShader(ShaderSource);
        }


        protected virtual void SetCustomShader(ShaderItem shader)
        {
            return;

            if (Display == null)
            {
                return;
            }

            //just having fun, add ripples to preview
/*
            if (_shaderGlobal == null)
            {
                _shaderGlobal = new MultiRippleWithTouchEffect() 
                {
                    SecondarySource="Images/logo.png"
                };
                VisualEffects.Add(_shaderGlobal);
            }
*/

            // Remove existing shader if any
            if (_shader != null && VisualEffects.Contains(_shader))
            {
                _shader.OnCompilationError -= OnShaderError;
                VisualEffects.Remove(_shader);
            }

            if (Effect == SkiaImageEffect.Custom && shader != null)
            {

                // Create new shader with the specified filename
                _shader = new ClippedShaderEffect(Display)
                {
                    ShaderSource = shader.Filename,
                    //FilterMode = SKFilterMode.Linear <== it's default
                };

                // Add the new shader
                if (_shader != null && !VisualEffects.Contains(_shader))
                {
                    _shader.OnCompilationError += OnShaderError;
                    VisualEffects.Add(_shader);
                }
            }
        }

        private ShaderEditorPage _editor;

        private void OnShaderError(object sender, string error)
        {
            _editor?.ReportCompilationError(error);
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
                            _editor = new ShaderEditorPage(code, CallBackSetSelectedShaderCode);
                            OpenPageInNewWindow(_editor, "Shader Editor");
                        });
                    }
                });
            }
        }

        void CallBackSetSelectedShaderCode(string code)
        {
            ChangeShaderCode(code);
        }

        public static void OpenPageInNewWindow(ContentPage page, 
            string title = "Editor")
        {
#if WINDOWS || MACCATALYST
            var window = new Window(page) { Title = title };
            
            if (page is ShaderEditorPage)
            {
                window.Width = 800;
                window.Height = 800;
                window.MinimumWidth = 600;
                window.MinimumHeight = 400;
                
                var mainWindow = Application.Current?.Windows?.FirstOrDefault();
                if (mainWindow != null)
                {
                    window.X = mainWindow.X + mainWindow.Width + 20; 
                    window.Y = mainWindow.Y;  
                }
            }
            
            Application.Current.OpenWindow(window);
#endif
        }

        private static void NeedChangeShader(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is CameraWithEffects control)
            {
                control.SetCustomShader(control.ShaderSource);
            }
        }

        public static readonly BindableProperty ShaderSourceProperty = BindableProperty.Create(nameof(ShaderSource),
            typeof(ShaderItem),
            typeof(CameraWithEffects),
            null, propertyChanged: NeedChangeShader);

        public ShaderItem ShaderSource
        {
            get { return (ShaderItem)GetValue(ShaderSourceProperty); }
            set { SetValue(ShaderSourceProperty, value); }
        }


    }
}
