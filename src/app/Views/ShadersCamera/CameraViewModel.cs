﻿using DrawnUi.Camera;
using System.Windows.Input;
using AppoMobi.Maui.Gestures;
using System.Collections.ObjectModel;
using System.Globalization;
using AppoMobi.Maui.DrawnUi.Demo.Views;
using ShadersCamera.Views.ShadersCamera;

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
                new ShaderItem { Title = "Negative", ShaderFilename = "Shaders/Camera/invert.sksl" },
                new ShaderItem { Title = "TV", ShaderFilename = "Shaders/Camera/retrotv.sksl" },
                new ShaderItem { Title = "Gazette", ShaderFilename = "Shaders/Camera/newspaper.sksl" },
                new ShaderItem { Title = "Palette", ShaderFilename = "Shaders/Camera/old-palette.sksl" },
                new ShaderItem { Title = "Neon", ShaderFilename = "Shaders/Camera/popart.sksl" },
                new ShaderItem { Title = "Sketch", ShaderFilename = "Shaders/Camera/sketch.sksl" },
                new ShaderItem { Title = "Pixels", ShaderFilename = "Shaders/Camera/pixels.sksl" }
            };

            SelectedShader = ShaderItems[5];
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
                        if (shader != null && Camera is CameraWithEffects cameraWithEffects)
                        {
                            SelectedShader = shader;
                            cameraWithEffects.SetCustomShader(shader.ShaderFilename);
                        }
                    }
                });
            }
        }

     
        public ICommand CommandCaptureStillPhoto
        {
            get
            {
                return new Command(async (object context) =>
                {
                    if (TouchEffect.CheckLockAndSet())
                        return;

                    if (Camera.State == CameraState.On && !Camera.IsBusy)
                    {
                        Camera.FlashScreen(Color.Parse("#EEFFFFFF"));
                        await Camera.TakePicture().ConfigureAwait(false);
                    }
                });
            }
        }

        public Command CommandPreviewTapped => new Command(async () =>
        {
            if (TouchEffect.CheckLockAndSet() || string.IsNullOrEmpty(_lastSavedPath))
                return;

            Camera.OpenFileInGallery(_lastSavedPath);
        });

        #endregion

        /// <summary>
        /// Cat be set from arguments by shell
        /// </summary>
        public ICommand Callback { get; set; }

        private LoadedImageSource _displayPreview;
        public LoadedImageSource DisplayPreview
        {
            get
            {
                return _displayPreview;
            }
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
            get
            {
                return _LoadImage;
            }
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
            get
            {
                return _ShowResume;
            }
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
            // normaly you would have several way to display a captured preview after you receive a large capture
            // 1 - create small bitmap rotated according to device orientation
            // 2 - just display the large bitmap but set preview UseCache="Bitmap"
            // 3 - create a new rotated large bitmap with any overlays etc and display it, dont forget UseCache="Bitmap"

            //this demonstrates case 3

            //creating an overlay to be rendered over the captured photo
            var overlay = new SkiaLayout()
            {
                Type = LayoutType.Column,
                Spacing = 10,
                BackgroundColor = Color.Parse("#46000000"),
                VerticalOptions = LayoutOptions.End,
                HorizontalOptions = LayoutOptions.Fill,
                Padding = new Thickness(16),
            };

            overlay.AddSubView(new SkiaLabel()
            {
                Text = $"DrawnUi Camera Demo",
                FontFamily = "FontPhotoBold",
                FontSize = 26,
                TextColor = Colors.White,
                HorizontalOptions = LayoutOptions.Center,
                HorizontalTextAlignment = DrawTextAlignment.Center,
                VerticalOptions = LayoutOptions.Start
            });

            //overlay some info over bitmap
            var newBitmap = Camera.RenderCapturedPhoto(captured, overlay, image =>
            {
                if (SelectedShader != null)
                {
                    var shaderEffect = new SkiaShaderEffect()
                    {
                        ShaderSource = SelectedShader.ShaderFilename,
                    };
                    image.VisualEffects.Add(shaderEffect);
                }
            });

            //going to use the newly created bitmap with effects applied and overlay info
            //to save to gallery, so need to dispose the original one
            captured.Image.Dispose();
            captured.Image = newBitmap;

            //save to device, can use custom album name if needed
            Camera.CameraDevice.Meta.Orientation = 1; //since we rotate the capture to overlay info the orientation will always be 1 (default)
            await Camera.SaveToGallery(captured, false);

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
        }

    }
}
