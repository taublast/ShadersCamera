using AppoMobi.Specials;
using DrawnUi.Camera;
using DrawnUi.Infrastructure;
using ShadersCamera.Models;
using System.Windows.Input;
using static Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific.VisualElement;

namespace ShadersCamera.Views.Controls
{
    public class AppCamera : SkiaCamera
    {

        public AppCamera()
        {
            //NeedPermissionsSet = NeedPermissions.Camera | NeedPermissions.Gallery;
            UseRealtimeVideoProcessing = true;
#if DEBUG
            VideoDiagnosticsOn = true;
#endif
        }

        protected override void RenderPreviewForProcessing(SKCanvas canvas, SKImage frame)
        {
            var shader = GetEffectShader();
            if (shader == null)
            {
                base.RenderPreviewForProcessing(canvas, frame);
                return;
            }

            shader.DrawImage(canvas, frame, 0, 0);
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

        //private SkiaShaderEffect _shader;
        private SkiaShaderEffect _shaderGlobal;
        public void ChangeShaderCode(string code)
        {
            if (_effectShader == null)
            {
                return;
            }

            _effectShader.CompileFromCode(code, null, false, RaiseShaderError);
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

        private SkiaShader GetEffectShader()
        {
            var effect = VideoEffect;
            if (effect == null)
            {
                ReleaseEffectShader();
                return null;
            }

            if (_effectShader != null && _loadedEffect == effect)
            {
                return _effectShader;
            }

            ReleaseEffectShader();

            var filename = effect.Filename;// ShaderEffectHelper.GetFilename(effect);
            if (string.IsNullOrWhiteSpace(filename))
            {
                return null;
            }

            _effectShader = SkiaShader.FromResource(filename, true, RaiseShaderError);
            _loadedEffect = effect;

            return _effectShader;
        }

        private void ReleaseEffectShader()
        {
            _effectShader?.Dispose();
            _effectShader = null;
            _loadedEffect = null;
        }

        private SkiaShader _effectShader;
        private ShaderItem _loadedEffect;

        public static readonly BindableProperty VideoEffectProperty = BindableProperty.Create(
            nameof(VideoEffect),
            typeof(ShaderItem),
            typeof(AppCamera),
            null);

        public ShaderItem VideoEffect
        {
            get => (ShaderItem)GetValue(VideoEffectProperty);
            set => SetValue(VideoEffectProperty, value);
        }

        public override void OnWillDisposeWithChildren()
        {
            ReleaseEffectShader();

            base.OnWillDisposeWithChildren();
        }

        protected virtual void SetCustomShader(ShaderItem shader)
        {
            VideoEffect = shader;
        }

        private ShaderEditorPage _editor;

        private void RaiseShaderError(string error)
        {
            _editor?.ReportCompilationError(error);
        }

        private void OnShaderError(object sender, string error)
        {
            RaiseShaderError(error);
        }

        public ICommand CommandEditShader
        {
            get
            {
                return new Command(async (context) =>
                {
                    //just change currently running shader code, no matter what exactly we longpressed
                    if (_effectShader != null)
                    {
                        var code = _effectShader.Code;
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
            if (bindable is AppCamera control)
            {
                control.SetCustomShader(control.ShaderSource);
            }
        }

        public static readonly BindableProperty ShaderSourceProperty = BindableProperty.Create(nameof(ShaderSource),
            typeof(ShaderItem),
            typeof(AppCamera),
            null, propertyChanged: NeedChangeShader);

        public ShaderItem ShaderSource
        {
            get { return (ShaderItem)GetValue(ShaderSourceProperty); }
            set { SetValue(ShaderSourceProperty, value); }
        }


    }
}
