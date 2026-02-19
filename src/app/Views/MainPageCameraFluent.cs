using DrawnUi.Camera;
using DrawnUi.Views;
using ShadersCamera.Helpers;
using ShadersCamera.ViewModels;
using System.ComponentModel;
using System.Diagnostics;
using ShadersCamera.Resources.Strings;

namespace ShadersCamera.Views
{
    public partial class MainCameraPageFluent : BasePageReloadable, IPageWIthCamera
    {
        public MainCameraPageFluent()
        {
            BackgroundColor = Colors.Black; //iOS statusbar and bottom insets

            try
            {
                _vm = new CameraViewModel();

                BindingContext = _vm;

                IsFullScreen = UserSettings.Current.Fill;
                IsMirrored = UserSettings.Current.Mirror;

#if ANDROID
            Super.SetNavigationBarColor(Colors.Black, Colors.Black, true);
#endif
            }
            catch (Exception e)
            {
                Super.DisplayException(this, e);
            }
        }

        void Subscribe(bool subscribe)
        {
            if (subscribe)
            {
                Canvas.ViewDisposing += CanvasWillDispose;
                Canvas.WillFirstTimeDraw += WillFirstTimeDraw;
                if (CameraControl != null)
                {
                    CameraControl.CaptureFlashMode = (CaptureFlashMode)UserSettings.Current.Flash;
                    CameraControl.PropertyChanged += OnContextPropertyChanged;
                }
            }
            else
            {
                if (Canvas != null)
                {
                    Canvas.ViewDisposing -= CanvasWillDispose;
                    Canvas.WillFirstTimeDraw -= WillFirstTimeDraw;
                }

                if (CameraControl != null)
                {
                    CameraControl.PropertyChanged -= OnContextPropertyChanged;
                }
            }
        }

        void AttachCamera()
        {
            if (BindingContext is CameraViewModel vm && CameraControl != null)
            {
                vm.AttachCamera(CameraControl);

                CameraControl.NewPreviewSet += OnPreviewSet;
                CameraControl.StateChanged += OnCameraStateChanged;

                SyncUi();

                try
                {
                    CameraControl.IsOn = true;
                }
                catch (Exception e)
                {
                    Super.Log(e);
                }
            }
        }

        /// <summary>
        /// Flag to capture a small ML/other small image from the current camera preview
        /// </summary>
        private bool TriggerUpdateSmallPreview;

        SemaphoreSlim semaphoreProcessingFrame = new(1, 1);
        private object lockCatchFrame = new();

        /// <summary>
        /// Flag to check first ever preview frame received ok
        /// </summary>
        private bool StartupSuccessChecked;

        private void OnCameraStateChanged(object sender, CameraState state)
        {
            if (state == CameraState.On)
            {
                Debug.WriteLine($"[CameraApp] State in ON!");

                if (UserSettings.Current.Formats.TryGetValue(CameraControl.CameraDevice.Id, out var format))
                {
                    CameraControl.PhotoFormatIndex = format;
                    CameraControl.PhotoQuality = CaptureQuality.Manual;
                }
                else
                {
                    CameraControl.PhotoQuality = CaptureQuality.Medium;
                }
            }
        }



        /// <summary>
        /// We have captured a preview frame.
        /// Will use it for shaders menu. Same mechanics could be used to send this to AI etc.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="source"></param>
        private void OnPreviewSet(object sender, LoadedImageSource source)
        {
            lock (lockCatchFrame)
            {
                if (!StartupSuccessChecked)
                {
                    StartupSuccessChecked = true;
                    try
                    {
                        if (!UserSettings.Current.ShownWelcome)
                        {
                            UserSettings.Current.ShownWelcome = true;
                            OpenHelp();
                        }
                    }
                    catch (Exception e)
                    {
                        Super.Log(e);
                    }

                    return;
                }

                if (TriggerUpdateSmallPreview && semaphoreProcessingFrame.CurrentCount != 0)
                {
                    TriggerUpdateSmallPreview = false;

                    var image = source.Image;
                    if (image == null)
                    {
                        image = SKImage.FromBitmap(source.Bitmap);
                    }

                    //full copy from gpu preview surface NOT USED
                    //var info = image.Info;
                    //var pixelData = new byte[info.BytesSize];
                    //unsafe
                    //{
                    //    fixed (byte* ptr = pixelData)
                    //    {
                    //        bool success = image.ReadPixels(info, (nint)ptr);
                    //        if (success)
                    //        {
                    //            var copiedImage = SKImage.FromPixels(info, (nint)ptr, info.RowBytes);
                    //        }
                    //    }
                    //}

                    var marginFactor = 0.2f; // Crop from sides to focus on core center
                    var targetSize = 256;

                    var originalInfo = image.Info;
                    var maxCropSize = Math.Min(originalInfo.Width, originalInfo.Height);
                    var actualCropSize = maxCropSize * (1.0f - marginFactor);

                    var cropX = (originalInfo.Width - actualCropSize) / 2;
                    var cropY = (originalInfo.Height - actualCropSize) / 2;

                    var newInfo = new SKImageInfo(targetSize, targetSize, SKColorType.Rgb888x, SKAlphaType.Opaque);
                    using var surface = SKSurface.Create(newInfo);
                    surface.Canvas.DrawImage(image,
                        new SKRect(cropX, cropY, cropX + actualCropSize, cropY + actualCropSize),
                        new SKRect(0, 0, targetSize, targetSize));
                    var mlImage = surface.Snapshot();

                    //use in UI - our preview for shaders menu
                    var dispose = SmallPreview;
                    SmallPreview = new LoadedImageSource(mlImage)
                    {
                        ProtectFromDispose = true
                    };
                    if (dispose != null)
                    {
                        CameraControl.DisposeObject(dispose);
                    }

                    //for AI/ML use this:
                    //Task.Run(async () =>
                    //{
                    //    //todo use image here

                    //}).ConfigureAwait(false);
                }
            }
        }

        private LoadedImageSource _displayPreview;

        public LoadedImageSource SmallPreview
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


        private readonly CameraViewModel _vm;


        private void TappedSwitchCamera()
        {
            if (CameraControl.IsOn)
            {
                CameraControl.Facing = CameraControl.Facing == CameraPosition.Selfie
                    ? CameraPosition.Default
                    : CameraPosition.Selfie;
            }
        }

        private void TappedTurnCamera()
        {
            if (CameraControl.State == CameraState.On)
            {
                CameraControl.IsOn = false;
            }
            else
            {
                CameraControl.IsOn = true;
            }
        }


        private async void TappedTakePicture(object sender, SkiaGesturesParameters skiaGesturesParameters)
        {
            if (CameraControl.State == CameraState.On && !CameraControl.IsBusy)
            {
                CameraControl.FlashScreen(Color.Parse("#EEFFFFFF"));
                await CameraControl.TakePicture().ConfigureAwait(false);
            }
        }

        private void TappedResume()
        {
            CameraControl.IsOn = true;
        }

        float step = 0.2f;
        private bool _flashOn;

        private void Tapped_ZoomOut(object sender, SkiaGesturesParameters skiaGesturesParameters)
        {
            CameraControl.Zoom -= step;
        }

        private void Tapped_ZoomIn(object sender, SkiaGesturesParameters skiaGesturesParameters)
        {
            CameraControl.Zoom += step;
        }

        private void OnZoomed(object sender, ZoomEventArgs e)
        {
            CameraControl.Zoom = e.Value;
        }

        private void TappedFlash()
        {
            _flashOn = !_flashOn;

            if (_flashOn)
            {
                CameraControl.FlashMode = FlashMode.On;
            }
            else
            {
                CameraControl.FlashMode = FlashMode.Off;
            }

            SyncUi();
        }

        private void TappedBackground(object sender, ControlTappedEventArgs e)
        {
            TriggerUpdateSmallPreview = true;
        }


        private void WillFirstTimeDraw(object sender, SkiaDrawingContext e)
        {
            AttachCamera();
        }

        private void CanvasWillDispose(object sender, EventArgs e)
        {
            CameraControl.NewPreviewSet -= OnPreviewSet;
            CameraControl.StateChanged -= OnCameraStateChanged;
        }

        private void TappedDrawerHeader()
        {
            ShaderDrawer.IsOpen = !ShaderDrawer.IsOpen;
        }


        /// <summary>
        /// Observing SkiaCamera props
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnContextPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SkiaCamera.IsBusy))
            {
                if (_vm.IsRecording)
                    return;

                ButtonCapture.BackgroundColor = CameraControl.IsBusy
                    ? Colors.DarkRed
                    : Color.Parse("#CECECE");
            }
        }


        #region SELECT FORMAT

        public CaptureFormat SelectedFormat
        {
            get { return CameraControl.CurrentStillCaptureFormat; }
        }

        public void SelectFormat(Action<string> changed)
        {
            if (CameraControl.IsOn)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        var formats = await CameraControl.GetAvailableCaptureFormatsAsync();

                        if (!formats.Any())
                        {
                            await App.Current.MainPage.DisplayAlert("Error", "No capture formats available", "OK");
                            return;
                        }

                        // Create picker with detailed format info
                        var options = formats.Select((format, index) =>
                            $"{format.Width}x{format.Height}, {format.AspectRatioString}"
                        ).ToArray();

                        var result = await App.Current.MainPage.DisplayActionSheet(
                            "Capture Photo Quality",
                            ResStrings.BtnCancel,
                            null,
                            options);

                        if (!string.IsNullOrEmpty(result))
                        {
                            var selectedIndex = Array.IndexOf(options, result);
                            if (selectedIndex >= 0)
                            {
                                // Set manual capture mode with selected format
                                CameraControl.PhotoFormatIndex = selectedIndex;
                                CameraControl.PhotoQuality = CaptureQuality.Manual;
                                OnPropertyChanged(nameof(SelectedFormat));
                                changed?.Invoke(result);

                                Debug.WriteLine(
                                    $"[CameraApp] Format selection: {selectedIndex} for {CameraControl.CameraDevice.Id}");

                                UserSettings.Current.Formats[CameraControl.CameraDevice.Id] = selectedIndex;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        await App.Current.MainPage.DisplayAlert("Error", $"Failed to get capture formats: {ex.Message}",
                            "OK");
                        Debug.WriteLine($"[CameraApp] Format selection error: {ex}");
                    }
                });
            }
        }

        #endregion

        #region SELECT ASPECT

        public void SetAspect(bool fullScreen)
        {
            IsFullScreen = fullScreen;
            ApplyAspect();
        }

        void ApplyAspect()
        {
            if (CameraControl == null)
            {
                return;
            }

            if (IsFullScreen)
            {
                CameraControl.Aspect = TransformAspect.AspectCover;
            }
            else
            {
                CameraControl.Aspect = TransformAspect.AspectFitFill;
            }

            CameraControl.MirrorPreviewX = IsMirrored;

            UserSettings.Current.Mirror = IsMirrored;
            UserSettings.Current.Fill = CameraControl.Aspect == TransformAspect.AspectCover;
        }

        public bool IsFullScreen { get; set; }

        public bool IsMirrored { get; set; } 

        public void SetMirrored(bool value)
        {
            IsMirrored = value;
            ApplyAspect();
        }

        #endregion

        private void OnFlashClicked(object sender, object e)
        {
            try
            {
                var currentMode = CameraControl.CaptureFlashMode;
                var nextMode = currentMode switch
                {
                    CaptureFlashMode.Off => CaptureFlashMode.Auto,
                    CaptureFlashMode.Auto => CaptureFlashMode.On,
                    CaptureFlashMode.On => CaptureFlashMode.Off,
                    _ => CaptureFlashMode.Auto
                };

                CameraControl.CaptureFlashMode = nextMode;

                SyncUi();

                Debug.WriteLine($"Camera Status: Capture flash mode set to {nextMode}");

                UserSettings.Current.Flash = (int)nextMode;
                UserSettings.Save();
            }
            catch (Exception ex)
            {
                Super.Log($"[CameraTestPage] OnFlashClicked error: {ex}");
            }
        }
    }
}