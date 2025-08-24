using AppoMobi.Maui.Gestures;
using DrawnUi.Camera;
 using ShadersCamera.Models;
using System.Collections.ObjectModel;
using System.Windows.Input;
using ShadersCamera.Helpers;

namespace ShadersCamera.ViewModels
{
    /// <summary>
    /// Captured picture will land here.
    /// We can get a preview for AI analysis inside this viewmodel too.
    /// Could pass the captured image further using callback.
    /// </summary>
    public class CameraViewModel : ProjectViewModel, IQueryAttributable
    {
        public CameraViewModel()
        {
            ShaderItems = new ObservableCollection<ShaderItem>
            {
                new ShaderItem { Title = "Classic", Filename = "Shaders/Camera/bwclassic.sksl" },
                new ShaderItem { Title = "Street", Filename = "Shaders/Camera/bwstreet.sksl" },
                new ShaderItem { Title = "Street Zoom", Filename = "Shaders/Camera/bwstreet200.sksl" },
                new ShaderItem { Title = "Fine Art", Filename = "Shaders/Camera/bwfineart.sksl" },
                new ShaderItem { Title = "Newspaper", Filename = "Shaders/Camera/bwprint.sksl" },
                new ShaderItem { Title = "Portrait", Filename = "Shaders/Camera/bwportrait.sksl" },
                new ShaderItem { Title = "Sin City", Filename = "Shaders/Camera/selective.sksl" },
                
                new ShaderItem { Title = "Negative", Filename = "Shaders/Camera/invert.sksl" },

                new ShaderItem { Title = "Raw", Filename = "Shaders/Camera/blit.sksl" },
                new ShaderItem { Title = "Zoom", Filename = "Shaders/Camera/photozoom.sksl" },
                new ShaderItem { Title = "Desaturated", Filename = "Shaders/Camera/snyder.sksl" },
                new ShaderItem { Title = "Action", Filename = "Shaders/Camera/action.sksl" },
                new ShaderItem { Title = "Romance", Filename = "Shaders/Camera/romance.sksl" },
                new ShaderItem { Title = "Film", Filename = "Shaders/Camera/film.sksl" },

                new ShaderItem { Title = "Mystic", Filename = "Shaders/Camera/enigma.sksl" },
                new ShaderItem { Title = "Blade Runner", Filename = "Shaders/Camera/blade.sksl" },
                new ShaderItem { Title = "Blues", Filename = "Shaders/Camera/nolan.sksl" },
                new ShaderItem { Title = "Drive", Filename = "Shaders/Camera/pink.sksl" },
                new ShaderItem { Title = "Desert", Filename = "Shaders/Camera/desert.sksl" },
                new ShaderItem { Title = "Blockbuster", Filename = "Shaders/Camera/blockbuster.sksl" },
                new ShaderItem { Title = "Pastels", Filename = "Shaders/Camera/wes.sksl" },
                new ShaderItem { Title = "Kodachrome", Filename = "Shaders/Camera/kodachrome.sksl" },

                new ShaderItem { Title = "Palette", Filename = "Shaders/Camera/old-palette.sksl" },
                new ShaderItem { Title = "Neon", Filename = "Shaders/Camera/popart.sksl" },

                new ShaderItem { Title = "TV", Filename = "Shaders/Camera/retrotv.sksl" },
                new ShaderItem { Title = "Toon", Filename = "Shaders/Camera/toon.sksl" },
                new ShaderItem { Title = "Sketch", Filename = "Shaders/Camera/sketch.sksl" },
                
                new ShaderItem { Title = "Pixels", Filename = "Shaders/Camera/pixels.sksl" }
            };

            var index = 0;
            if (!string.IsNullOrEmpty(UserSettings.Current.Filter))
            {
                var shaderNb = 0;
                foreach (var shader in ShaderItems)
                {
                    if (shader.Title == UserSettings.Current.Filter)
                    {
                        index = shaderNb;
                    }
                    shaderNb++;
                }
            }
            SelectedShader = ShaderItems[index];
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query != null)
            {
                query.TryGetValue("callback", out var command);
                if (command != null)
                {
                    Callback = command as ICommand;
                }
            }
        }

        #region PROPERTIES

        private ObservableCollection<ShaderItem> _shaderItems;

        public ObservableCollection<ShaderItem> ShaderItems
        {
            get { return _shaderItems; }
            set
            {
                if (_shaderItems != value)
                {
                    _shaderItems = value;
                    OnPropertyChanged();
                }
            }
        }

        private ShaderItem _selectedShader;

        public ShaderItem SelectedShader
        {
            get { return _selectedShader; }
            set
            {
                if (_selectedShader != value)
                {
                    _selectedShader = value;
                    OnPropertyChanged();
                    if (value == null)
                    {
                        UserSettings.Current.Filter = string.Empty;
                    }
                    else
                    {
                        UserSettings.Current.Filter = value.Title;
                    }
                    UserSettings.Save();
                }
            }
        }

        #endregion

        #region COMMANDS

        public ICommand CommandSelectShader
        {
            get
            {
                return new Command(async (object context) =>
                {
                    if (context is ShaderItem shader)
                    {
                        SelectedShader = shader;
                    }
                });
            }
        }


        public ICommand CommandCaptureStillPhoto
        {
            get
            {
                return new Command( (object context) =>
                {
                    if (TouchEffect.CheckLockAndSet())
                        return;

                    if (Camera.State == CameraState.On && !Camera.IsBusy)
                    {
                        //Camera.FlashScreen(Color.Parse("#EE000000"));
                        _ = Camera.TakePicture().ConfigureAwait(false);
                    }
                });
            }
        }

        public Command CommandPreviewTapped => new Command(async () =>
        {
            if (TouchEffect.CheckLockAndSet() || string.IsNullOrEmpty(_lastSavedPath))
                return;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                SkiaCamera.OpenFileInGallery(_lastSavedPath);
            });
        });

        #endregion

        /// <summary>
        /// Cat be set from arguments by shell
        /// </summary>
        public ICommand Callback { get; set; }

        private LoadedImageSource _displayPreview;

        public LoadedImageSource DisplayPreview
        {
            get { return _displayPreview; }
            set
            {
                if (_displayPreview != value)
                {
                    _displayPreview = value;
                    OnPropertyChanged();
                }
            }
        }

        private ImageSource _LoadImage;

        public ImageSource LoadImage
        {
            get { return _LoadImage; }
            set
            {
                if (_LoadImage != value)
                {
                    _LoadImage = value;
                    OnPropertyChanged();
                }
            }
        }

        SemaphoreSlim semaphoreProcessingFrame = new(1, 1);

        private string _lastSavedPath;

        public void AttachCamera(SkiaCamera camera)
        {
            if (Camera == null && camera != null)
            {
                Camera = camera;
                Camera.CaptureSuccess += OnCaptureSuccess;
                Camera.StateChanged += OnCameraStateChanged;
                Camera.NewPreviewSet += OnNewPreviewSet;
            }
        }

        public override void OnDisposing()
        {
            if (Camera != null)
            {
                Camera.CaptureSuccess -= OnCaptureSuccess;
                Camera.StateChanged -= OnCameraStateChanged;
                Camera.NewPreviewSet -= OnNewPreviewSet;
                Camera = null;
            }

            base.OnDisposing();
        }

        private void OnNewPreviewSet(object sender, LoadedImageSource source)
        {
            //Task.Run(async () =>
            //{

            //    ProcessPreviewFrame(source); was used in detection mode

            //}).ConfigureAwait(false);
        }

        private void OnCameraStateChanged(object sender, CameraState state)
        {
            if (state == CameraState.On)
            {
                if (Camera != null && Camera.Display != null)
                {
                    Camera.Display.Blur = 0;
                }

                ShowResume = false;
            }
            else
            {
                ShowResume = true;
                if (Camera != null && Camera.Display != null)
                {
                    Camera.Display.Blur = 10;
                }
            }
        }

        private bool _ShowResume;

        public bool ShowResume
        {
            get { return _ShowResume; }
            set
            {
                if (_ShowResume != value)
                {
                    _ShowResume = value;
                    OnPropertyChanged();
                }
            }
        }

        bool _loadOnce;

        protected SkiaCamera Camera { get; set; }

        private async void OnCaptureSuccess(object sender, CapturedImage captured)
        {

            captured.SolveExifOrientation();

            var imageWithOverlay = Camera.RenderCapturedPhoto(captured, null, image =>
            {
                if (SelectedShader != null)
                {
                    var shaderEffect = new SkiaShaderEffect()
                    {
                        ShaderSource = SelectedShader.Filename,
                    };
                    image.VisualEffects.Add(shaderEffect);
                }
            });

            //going to use the newly created bitmap with effects 
            //to save to gallery, so need to dispose the original one
            captured.Image.Dispose();
            captured.Image = imageWithOverlay;

            captured.Meta.Vendor = MauiProgram.ExifCameraVendor;
            captured.Meta.Model = MauiProgram.ExifCameraModel;

            //save to device, can use custom album name if needed
            await Camera.SaveToGalleryAsync(captured, false); 

            //display preview
            //captured.Bitmap will be disposed by image ImagePreview when it
            //changes source to a new one, or when ImagePreview is disposed
            //ImagePreview.SetBitmapInternal(captured.Bitmap);
            //DisplayPreview = new(captured.Image);  //not using this in this screen

            _lastSavedPath = captured.Path;

            if (Callback != null)
            {
                Callback.Execute(captured);
            }

            var dispose = DisplayPreview;
            DisplayPreview = new LoadedImageSource(captured.Image);
            if (dispose != null)
            {
                Camera?.DisposeObject(dispose);
            }
        }

 
    }
}