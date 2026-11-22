using DrawnUi.Camera;
using DrawnUi.Views;
using ShadersCamera.Helpers;
using ShadersCamera.ViewModels;
using ShadersCamera.Views.Controls;
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
                Thumbnails.Updated += (s, e) => ThumbsVersion++;
                SeedThumbnailsWithSample();

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

        /// <summary>
        /// Fetches the location for photo metadata, never asking for it: a location prompt at camera start froze the
        /// preview on its first frame while the prompt was up. The permission is asked after the first saved photo
        /// (CameraViewModel); until then the refresh is skipped.
        /// </summary>
        void RefreshGpsLocationIfNeeded()
        {
            if (!CameraControl.InjectGpsLocation)
                return;

            _ = Task.Run(async () =>
            {
                if (!await SkiaCamera.RequestPermissionsGrantedAsync(SkiaCamera.NeedPermissions.Location))
                    return;

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _ = CameraControl.RefreshGpsLocation();
                });
            });
        }

        void Subscribe(bool subscribe)
        {
            if (subscribe)
            {
                Canvas.ViewDisposing += CanvasWillDispose;
                Canvas.WillFirstTimeDraw += WillFirstTimeDraw;
                Canvas.WasDrawn += OnCanvasDrawn;
                Thumbnails.RequestFrame = RequestThumbsFrame;
                Thumbnails.DisposeManager = Canvas; // kept on unsubscribe: a disposed canvas frees at once
                if (CameraControl != null)
                {
                    CameraControl.CaptureFlashMode = (CaptureFlashMode)UserSettings.Current.Flash;
                }
            }
            else
            {
                if (Canvas != null)
                {
                    Canvas.ViewDisposing -= CanvasWillDispose;
                    Canvas.WillFirstTimeDraw -= WillFirstTimeDraw;
                    Canvas.WasDrawn -= OnCanvasDrawn;
                }

                Thumbnails.RequestFrame = null;
            }
        }

        /// <summary>
        /// Until a camera frame is tapped in, the strip shows every shader over the bundled sample photo, as before: the
        /// sample is decoded once at thumb size and handed to the baker (no-op once a frame arrived).
        /// </summary>
        void SeedThumbnailsWithSample()
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    using var stream = await FileSystem.OpenAppPackageFileAsync("Images/8.jpg");
                    using var bitmap = SKBitmap.Decode(stream);
                    if (bitmap == null || Thumbnails.HasSource)
                        return;

                    var side = Math.Min(bitmap.Width, bitmap.Height);
                    var src = SKRect.Create((bitmap.Width - side) / 2f, (bitmap.Height - side) / 2f, side, side);
                    using var surface = SKSurface.Create(new SKImageInfo(256, 256, SKColorType.Rgba8888, SKAlphaType.Premul));
                    using var image = SKImage.FromBitmap(bitmap);
                    surface.Canvas.DrawImage(image, src, SKRect.Create(0, 0, 256, 256),
                        new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None));
                    var sample = surface.Snapshot();
                    if (Thumbnails.HasSource)
                        sample.Dispose();
                    else
                        Thumbnails.SetSource(sample);
                }
                catch (Exception e)
                {
                    Super.Log(e);
                }
            });
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

        private void OnCameraStateChanged(object sender, HardwareState state)
        {
            if (state == HardwareState.On)
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

                if (CameraControl.Display != null)
                {
                    CameraControl.Display.Blur = 0;
                }

                RefreshGpsLocationIfNeeded();
            }
            else
            {
                if (CameraControl.Display != null)
                {
                    CameraControl.Display.Blur = 10;
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

                    //the shaders menu: every shader baked once over this frame into plain pictures the strip blits
                    Thumbnails.SetSource(mlImage);

                    //for AI/ML use this:
                    //Task.Run(async () =>
                    //{
                    //    //todo use image here

                    //}).ConfigureAwait(false);
                }
            }
        }

        /// <summary>Baked strip thumbnails (see <see cref="FilterThumbnails"/>). Lives as long as the page.</summary>
        public FilterThumbnails Thumbnails { get; } = new();

        /// <summary>Pixel size of one strip thumbnail (80 points).</summary>
        static int ThumbSizePx => (int)Math.Ceiling(80 * Math.Max(1, Super.Screen.Density));

        /// <summary>Render thread, after every frame: bakes pending thumbs on the canvas GPU context (a flag check otherwise).</summary>
        void OnCanvasDrawn(object sender, SkiaDrawingContext e)
        {
            Thumbnails.BakeOnRenderThread(Canvas?.GetGRContext());
        }

        void RequestThumbsFrame()
        {
            MainThread.BeginInvokeOnMainThread(() => Canvas?.Update());
        }

        private int _thumbsVersion;

        /// <summary>Bumped on the main thread whenever a new batch of thumbnails is ready; cells observe it.</summary>
        public int ThumbsVersion
        {
            get { return _thumbsVersion; }
            private set
            {
                _thumbsVersion = value;
                OnPropertyChanged();
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


        private async void TappedSwitchCamera()
        {
            if (!CameraControl.IsOn)
                return;

            if (CameraControl.Facing == CameraPosition.Selfie)
            {
                CameraControl.Facing = CameraPosition.Default;
                return;
            }

            // no front camera on this device: switching would show black
            if (!await CameraControl.HasSelfieCameraAsync())
                return;

            if (CameraControl.IsOn)
                CameraControl.Facing = CameraPosition.Selfie;
        }

        private void TappedTurnCamera()
        {
            if (CameraControl.State == HardwareState.On)
            {
                CameraControl.IsOn = false;
            }
            else
            {
                CameraControl.IsOn = true;
            }
        }

        float step = 0.2f;
        private bool _flashOn;

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