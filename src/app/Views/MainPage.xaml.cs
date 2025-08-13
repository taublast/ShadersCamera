using System.ComponentModel;
using System.Diagnostics;
using DrawnUi.Camera;
using ShadersCamera.ViewModels;

namespace ShadersCamera.Views;

public partial class MainPage : IDisposable
{
#if DEBUG

    public MainPage()
    {
        try
        {
            _vm = new CameraViewModel();

            BindingContext = _vm;

            InitializeComponent();

            CameraControl.PropertyChanged += OnContextPropertyChanged;

#if ANDROID
            Super.SetNavigationBarColor(Colors.Black, Colors.Black, true);
#endif

            Loaded += OnPageLoaded;
        }
        catch (Exception e)
        {
            Super.DisplayException(this, e);
        }
    }


    void AttachCamera()
    {
        if (BindingContext is CameraViewModel vm)
        {
            vm.AttachCamera(CameraControl);

            CameraControl.NewPreviewSet += OnPreviewSet;

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
    /// 
    private bool TriggerUpdateSmallPreview;

    SemaphoreSlim semaphoreProcessingFrame = new(1, 1);
    private object lockCatchFrame = new();

    private void OnPreviewSet(object sender, LoadedImageSource source)
    {
        lock (lockCatchFrame)
        {
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

    private void OnPageLoaded(object sender, EventArgs e)
    {
        //AttachCamera();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        _sVisible = true;
    }

    volatile bool _sVisible;

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        _sVisible = false;

        CameraControl.IsOn = false;
    }

#endif

    private readonly CameraViewModel _vm;


    public MainPage(CameraViewModel vm)
    {
        _vm = vm;

        BindingContext = _vm;

        InitializeComponent();
    }

    private void TappedSwitchCamera(object sender, ControlTappedEventArgs controlTappedEventArgs)
    {
        if (CameraControl.IsOn)
        {
            CameraControl.Facing = CameraControl.Facing == CameraPosition.Selfie
                ? CameraPosition.Default
                : CameraPosition.Selfie;
        }
    }

    private void TappedTurnCamera(object sender, ControlTappedEventArgs controlTappedEventArgs)
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

    /// <summary>
    /// Cycle through effects in order: Sepia -> BlackAndWhite -> Pastel -> None -> Sepia...
    /// </summary>
    private void TappedCycleEffects(object sender, ControlTappedEventArgs controlTappedEventArgs)
    {
        var current = CameraControl.Effect;
        var currentIndex = CameraControl.AvailableEffects.IndexOf(current);

        // Move to next effect, wrap around to beginning if at end
        var nextIndex = (currentIndex + 1) % CameraControl.AvailableEffects.Count;

        CameraControl.SetEffect(CameraControl.AvailableEffects[nextIndex]);
    }

    private async void TappedTakePicture(object sender, SkiaGesturesParameters skiaGesturesParameters)
    {
        if (CameraControl.State == CameraState.On && !CameraControl.IsBusy)
        {
            CameraControl.FlashScreen(Color.Parse("#EEFFFFFF"));
            await CameraControl.TakePicture().ConfigureAwait(false);
        }
    }

    private void TappedResume(object sender, ControlTappedEventArgs controlTappedEventArgs)
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

    private void TappedFlash(object sender, ControlTappedEventArgs e)
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
    }

    private void TappedDrawerHeader(object sender, ControlTappedEventArgs e)
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
            var color = CameraControl.IsBusy ? Colors.DarkRed : Color.Parse("#CECECE");
            ButtonCapture.BackgroundColor = color;
        }
    }

    public void Dispose()
    {
        CameraControl.PropertyChanged -= OnContextPropertyChanged;
    }

    void SyncUi()
    {
        // CaptureFlashMode
        var currentMode = CameraControl.CaptureFlashMode;
        switch (currentMode)
        {
            case CaptureFlashMode.Off:
                SvgFlashCapture.SvgString = App.Current.Resources.Get<string>("SvgFlashOff");
                break;
            case CaptureFlashMode.Auto:
                SvgFlashCapture.SvgString = App.Current.Resources.Get<string>("SvgFlashAuto");
                break;
            case CaptureFlashMode.On:
                SvgFlashCapture.SvgString = App.Current.Resources.Get<string>("SvgFlashOn");
                break;
        }

        //FlashMode
        var torch = CameraControl.FlashMode;
        switch (torch)
        {
            case FlashMode.On:
                SvgFlashLight.SvgString = App.Current.Resources.Get<string>("SvgLightOn");
                break;
            case FlashMode.Off:
            default:
                SvgFlashLight.SvgString = App.Current.Resources.Get<string>("SvgLightOff");
                break;
        }
    }

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
        }
        catch (Exception ex)
        {
            Super.Log($"[CameraTestPage] OnFlashClicked error: {ex}");
        }
    }
}