using AppoMobi.Maui.Gestures;
using DrawnUi.Camera;
using ShadersCamera.Helpers;
using ShadersCamera.Models;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows.Input;

namespace ShadersCamera.ViewModels;

/// <summary>
/// Captured picture will land here.
/// We can get a preview for AI analysis inside this viewmodel too.
/// Could pass the captured image further using callback.
/// </summary>
public class CameraViewModel : ProjectViewModel, IQueryAttributable
{
    public enum CaptureUIMode
    {
        Photo = 0,
        Video = 1
    }

    public CameraViewModel()
    {
        ShaderItems = new ObservableCollection<ShaderItem>
        {
            new ShaderItem { Title = "Classic", Filename = "Shaders/Camera/bwclassic.sksl" },
            new ShaderItem { Title = "Street", Filename = "Shaders/Camera/bwstreet.sksl" },
            new ShaderItem { Title = "Street Zoom", Filename = "Shaders/Camera/bwstreet200.sksl" },
            new ShaderItem { Title = "Fine Art", Filename = "Shaders/Camera/bwfineart.sksl" },
            new ShaderItem { Title = "Kodak", Filename = "Shaders/Camera/kodaktmax400.sksl" },
            new ShaderItem { Title = "Fuji", Filename = "Shaders/Camera/fujineopan400.sksl" },
            new ShaderItem { Title = "Ilford", Filename = "Shaders/Camera/ilford.sksl" },
            new ShaderItem { Title = "Newspaper", Filename = "Shaders/Camera/bwprint.sksl" },
            new ShaderItem { Title = "Sin City", Filename = "Shaders/Camera/selective.sksl" },

            new ShaderItem { Title = "Raw", Filename = "Shaders/Camera/blit.sksl" },
            new ShaderItem { Title = "Zoom", Filename = "Shaders/Camera/photozoom.sksl" },
            new ShaderItem { Title = "Desaturated", Filename = "Shaders/Camera/snyder.sksl" },

            new ShaderItem { Title = "Romance", Filename = "Shaders/Camera/romance.sksl" },
            new ShaderItem { Title = "SoftPink", Filename = "Shaders/Camera/faded.sksl" },
            new ShaderItem { Title = "Soft Orange", Filename = "Shaders/Camera/insta.sksl" },
            new ShaderItem { Title = "Soft", Filename = "Shaders/Camera/orange.sksl" },
            new ShaderItem { Title = "Wes", Filename = "Shaders/Camera/wes.sksl" },

            new ShaderItem { Title = "Action", Filename = "Shaders/Camera/action.sksl" },
            new ShaderItem { Title = "Movie", Filename = "Shaders/Camera/film.sksl" },

            new ShaderItem { Title = "Mystic", Filename = "Shaders/Camera/enigma.sksl" },
            new ShaderItem { Title = "Blues", Filename = "Shaders/Camera/nolan.sksl" },
            new ShaderItem { Title = "Runner", Filename = "Shaders/Camera/blade.sksl" },
            new ShaderItem { Title = "Party", Filename = "Shaders/Camera/pink.sksl" },

            new ShaderItem { Title = "Desert", Filename = "Shaders/Camera/desert.sksl" },
            new ShaderItem { Title = "Blockbuster", Filename = "Shaders/Camera/blockbuster.sksl" },
            new ShaderItem { Title = "Kodachrome", Filename = "Shaders/Camera/kodachrome.sksl" },

            new ShaderItem { Title = "TV", Filename = "Shaders/Camera/retrotv.sksl" },
            new ShaderItem { Title = "Pixels", Filename = "Shaders/Camera/pixels.sksl" },

            new ShaderItem { Title = "Sketch", Filename = "Shaders/Camera/sketch.sksl" },
            new ShaderItem { Title = "Paint", Filename = "Shaders/Camera/sketchcolors.sksl" },

#if !ANDROID
            new ShaderItem { Title = "Poster", Filename = "Shaders/Camera/sketchcomics4.sksl" },
#endif

            new ShaderItem { Title = "Mars", Filename = "Shaders/Camera/hell.sksl" },
            new ShaderItem { Title = "Invert", Filename = "Shaders/Camera/invert.sksl" },
            new ShaderItem { Title = "Negative", Filename = "Shaders/Camera/negative.sksl" },
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

        SelectedShaderIndex = index;
        InitialIndex = index;

        Mode = CaptureUIMode.Photo;
    }

    public int InitialIndex
    {
        get => _initialIndex;
        set
        {
            if (value == _initialIndex) return;
            _initialIndex = value;
            OnPropertyChanged();
        }
    }

    public int SelectedShaderIndex
    {
        get => _selectedShaderIndex;
        set
        {
            if (value == _selectedShaderIndex) return;
            _selectedShaderIndex = value;
            OnPropertyChanged();
            if (value >= 0)
            {
                SelectedShader = ShaderItems[value];
            }
        }
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

    public ObservableCollection<ShaderItem> ShaderItems
    {
        get => _shaderItems;
        set
        {
            if (_shaderItems != value)
            {
                _shaderItems = value;
                OnPropertyChanged();
            }
        }
    }

    public ShaderItem SelectedShader
    {
        get => _selectedShader;
        set
        {
            if (_selectedShader != value)
            {
                _selectedShader = value;
                OnPropertyChanged();

                UserSettings.Current.Filter = value?.Title ?? string.Empty;
                UserSettings.Save();
            }
        }
    }

    public ICommand Callback { get; set; }

    public LoadedImageSource DisplayPreview
    {
        get => _displayPreview;
        set
        {
            if (_displayPreview != value)
            {
                _displayPreview = value;
                OnPropertyChanged();
            }
        }
    }

    public ImageSource LoadImage
    {
        get => _loadImage;
        set
        {
            if (_loadImage != value)
            {
                _loadImage = value;
                OnPropertyChanged();
            }
        }
    }

    public bool ShowResume
    {
        get => _showResume;
        set
        {
            if (_showResume != value)
            {
                _showResume = value;
                OnPropertyChanged();
            }
        }
    }

    public CaptureUIMode Mode
    {
        get => _mode;
        set
        {
            if (value == _mode) return;
            _mode = value;
            OnPropertyChanged();
        }
    }

    public TimeSpan RecordingDuration
    {
        get => _recordingDuration;
        set
        {
            if (value == _recordingDuration) return;
            _recordingDuration = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(RecordingDurationText));
        }
    }

    public string RecordingDurationText => RecordingDuration.ToString(@"mm\:ss");

    public bool IsRecording => Camera?.IsRecording ?? false;

    #endregion

    #region COMMANDS

    public ICommand CommandSelectShader => new Command((object context) =>
    {
        if (context is ShaderItem shader)
        {
            SelectedShader = shader;
            SelectedShaderIndex = ShaderItems.IndexOf(shader);
        }
    });

    public ICommand CommandCaptureStillPhoto => new Command((object context) =>
    {
        if (TouchEffect.CheckLockAndSet())
            return;

        if (Camera?.State == CameraState.On && !Camera.IsBusy)
        {
            _ = Camera.TakePicture().ConfigureAwait(false);
        }
    });

    public Command CommandPreviewTapped => new Command(() =>
    {
        if (TouchEffect.CheckLockAndSet() || string.IsNullOrEmpty(_lastSavedPath))
            return;

        MainThread.BeginInvokeOnMainThread(() => { SkiaCamera.OpenFileInGallery(_lastSavedPath); });
    });

    public ICommand CommandShutterTapped => new Command(async () =>
    {
        if (TouchEffect.CheckLockAndSet())
            return;

        if (Camera?.State != CameraState.On || Camera.IsBusy)
            return;

        if (Mode == CaptureUIMode.Photo)
        {
            CommandCaptureStillPhoto.Execute(null);
            return;
        }

        // Video mode: tap toggles start/stop
        if (!Camera.CanRecordVideo)
            return;

        try
        {
            if (Camera.IsRecording)
            {
                await Camera.StopVideoRecording().ConfigureAwait(false);
            }
            else
            {
                RecordingDuration = TimeSpan.Zero;
                await Camera.StartVideoRecording().ConfigureAwait(false);
            }
        }
        catch
        {
            // Caller can add user-facing error handling later.
        }
    });

    public ICommand CommandToggleCaptureMode => new Command(() =>
    {
        if (Camera?.IsRecording == true)
            return;

        Mode = Mode == CaptureUIMode.Photo ? CaptureUIMode.Video : CaptureUIMode.Photo;
    });

    #endregion

    public void AttachCamera(SkiaCamera camera)
    {
        if (Camera == null && camera != null)
        {
            Camera = camera;
            Camera.CaptureSuccess += OnCaptureSuccess;
            Camera.StateChanged += OnCameraStateChanged;
            Camera.NewPreviewSet += OnNewPreviewSet;
            Camera.IsRecordingVideoChanged += OnIsRecordingVideoChanged;
            Camera.RecordingProgress += OnVideoRecordingProgress;
        }
    }

    public override void OnDisposing()
    {
        if (Camera != null)
        {
            Camera.CaptureSuccess -= OnCaptureSuccess;
            Camera.StateChanged -= OnCameraStateChanged;
            Camera.NewPreviewSet -= OnNewPreviewSet;
            Camera.IsRecordingVideoChanged -= OnIsRecordingVideoChanged;
            Camera.RecordingProgress -= OnVideoRecordingProgress;
            Camera = null;
        }

        base.OnDisposing();
    }

    private void OnIsRecordingVideoChanged(object sender, bool isRecording)
    {
        OnPropertyChanged(nameof(IsRecording));
        if (!isRecording)
        {
            RecordingDuration = TimeSpan.Zero;
        }
    }

    private void OnVideoRecordingProgress(object sender, TimeSpan duration)
    {
        RecordingDuration = duration;
    }

    private void OnNewPreviewSet(object sender, LoadedImageSource source)
    {
        // was used in detection mode
    }

    private void OnCameraStateChanged(object sender, CameraState state)
    {
        if (state == CameraState.On)
        {
            if (Camera?.Display != null)
            {
                Camera.Display.Blur = 0;
            }

            ShowResume = false;
        }
        else
        {
            ShowResume = true;
            if (Camera?.Display != null)
            {
                Camera.Display.Blur = 10;
            }
        }
    }

    private async void OnCaptureSuccess(object sender, CapturedImage captured)
    {
        captured.SolveExifOrientation();

        var imageWithOverlay = await Camera.RenderCapturedPhotoAsync(captured, null, image =>
        {
            if (SelectedShader != null)
            {
                var shaderEffect = new SkiaShaderEffect()
                {
                    ShaderSource = SelectedShader.Filename,
                    TileMode = SKShaderTileMode.Mirror
                };
                image.VisualEffects.Add(shaderEffect);
            }
        }, true);

        captured.Image.Dispose();
        captured.Image = imageWithOverlay;

        captured.Meta.Vendor = MauiProgram.ExifCameraVendor;
        captured.Meta.Model = MauiProgram.ExifCameraModel;

        await Camera.SaveToGalleryAsync(captured);

        _lastSavedPath = captured.Path;

        Callback?.Execute(captured);

        var dispose = DisplayPreview;
        DisplayPreview = new LoadedImageSource(captured.Image);
        if (dispose != null)
        {
            Camera?.DisposeObject(dispose);
        }
    }

    private ObservableCollection<ShaderItem> _shaderItems;
    private ShaderItem _selectedShader;
    private LoadedImageSource _displayPreview;
    private ImageSource _loadImage;
    private bool _showResume;
    private int _selectedShaderIndex = -1;
    private int _initialIndex;
    private string _lastSavedPath;
    private CaptureUIMode _mode;
    private TimeSpan _recordingDuration;

    private readonly SemaphoreSlim semaphoreProcessingFrame = new(1, 1);
    protected SkiaCamera Camera { get; set; }
}