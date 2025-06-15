using AppoMobi.Specials;
using DrawnUi;
using DrawnUi.Camera;
using ShadersCamera;
using ShadersCamera.ViewModels;

namespace AppoMobi.Maui.DrawnUi.Demo.Views;

public partial class MainPageShadersCamera
{
#if DEBUG

    public MainPageShadersCamera()
    {
        try
        {
            _vm = new CameraViewModel();

            BindingContext = _vm;

            InitializeComponent();

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

            try
            {
                CameraControl.InitializeEffects();

                CameraControl.IsOn = true;
            }
            catch (Exception e)
            {
                Super.Log(e);
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


    public MainPageShadersCamera(CameraViewModel vm)
    {
        _vm = vm;

        BindingContext = _vm;

        InitializeComponent();
    }

    private void TappedSwitchCamera(object sender, SkiaControl.ControlTappedEventArgs controlTappedEventArgs)
    {
        if (CameraControl.IsOn)
        {
            CameraControl.Facing = CameraControl.Facing == CameraPosition.Selfie
                ? CameraPosition.Default
                : CameraPosition.Selfie;
        }
    }

    private void TappedTurnCamera(object sender, SkiaControl.ControlTappedEventArgs controlTappedEventArgs)
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
    private void TappedCycleEffects(object sender, SkiaControl.ControlTappedEventArgs controlTappedEventArgs)
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

    private void TappedResume(object sender, SkiaControl.ControlTappedEventArgs controlTappedEventArgs)
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

    private void OnShaderDrawerHandleTapped(object sender, SkiaControl.ControlTappedEventArgs e)
    {
        //if (ShaderDrawer != null)
        //{
        //    ShaderDrawer.IsOpen = !ShaderDrawer.IsOpen;
        //}
    }

    private void OnZoomed(object sender, ZoomEventArgs e)
    {
        CameraControl.Zoom = e.Value;
    }

    private void TappedFlash(object sender, SkiaControl.ControlTappedEventArgs e)
    {
        _flashOn = !_flashOn;

        if (_flashOn)
        {
            CameraControl.TurnOnFlash();
        }
        else
        {
            CameraControl.TurnOffFlash();
        }
    }

    private void SkiaControl_OnChildTapped(object sender, SkiaControl.ControlTappedEventArgs e)
    {
        var stop = 1;
    }

    private void TappedBackground(object sender, SkiaControl.ControlTappedEventArgs e)
    {
        if (ShaderDrawer.IsOpen)
        {
            ShaderDrawer.IsOpen = false;
        }
    }


    private void DrawnView_OnWillFirstTimeDraw(object sender, SkiaDrawingContext e)
    {
        AttachCamera();
    }
}
