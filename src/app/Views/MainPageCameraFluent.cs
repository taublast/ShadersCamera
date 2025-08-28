using AppoMobi.Specials;
using DrawnUi.Camera;
using DrawnUi.Controls;
using DrawnUi.Views;
using FastPopups;
using ShadersCamera.Helpers;
using ShadersCamera.Models;
using ShadersCamera.ViewModels;
using ShadersCamera.Views.Controls;
using System.ComponentModel;
using System.Diagnostics;
using ShadersCamera.Resources.Strings;
using Canvas = DrawnUi.Views.Canvas;

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

        Canvas Canvas;
        CameraWithEffects CameraControl;

        //static for Hot Preview
        public static SkiaViewSwitcher? ViewsContainer;

        SkiaShape ButtonCapture;
        SkiaSvg SvgFlashCapture;
        SkiaSvg SvgFlashLight;
        SkiaDrawer ShaderDrawer;
        SkiaImage ImagePreview;

        //will be called by page constructor and hotreload
        public override void Build()
        {
            Subscribe(false);

            Canvas?.Dispose();

            Canvas = new Canvas()
            {
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill,
                Gestures = GesturesMode.Lock,
                RenderingMode = RenderingModeType.Accelerated,
                BackgroundColor = Colors.Black,

                Content = new SkiaLayer()
                {
                    Children =
                    {
                        //for eventual possible navigation
                        new SkiaViewSwitcher()
                        {
                            HorizontalOptions = LayoutOptions.Fill,
                            VerticalOptions = LayoutOptions.Fill,
                            SelectedIndex = 0,
                            Children =
                            {
                                CreateMainLayout()
                            }
                        }.Assign(out ViewsContainer),
#if DEBUG
                        new SkiaLabelFps()
                        {
                            Margin = new(0, 0, 4, 24),
                            VerticalOptions = LayoutOptions.End,
                            HorizontalOptions = LayoutOptions.End,
                            Rotation = -45,
                            FontSize = 11,
                            BackgroundColor = Colors.DarkRed,
                            TextColor = Colors.White,
                            ZIndex = 110,
                        }
#endif
                    }
                }.Fill()
            };

#if IOS
            this.Content =
 new Grid() //using grid wrapper to take apply safe insets on ios, other platforms use different logic
            {
                Children = { Canvas }
            };
#else
            this.Content = Canvas;
#endif

            Subscribe(true);
        }

        #region UI

        SkiaLayout CreateMainLayout()
        {
            return new SkiaLayout()
            {
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill,
                Children =
                {
                    CreateCameraLayer(),
                    CreateShaderDrawer(),
                    //CreateDebugFps()
                }
            };
        }

        SkiaLayer CreateCameraLayer()
        {
            return new SkiaLayer()
            {
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill,
                Children =
                {
                    CreateCameraControl(),
                    CreateControlsLayer(),
                    CreateZoomHotspot()
                }
            }
            .OnTapped(me =>
            {
                TriggerUpdateSmallPreview = true;
            });
        }

        CameraWithEffects CreateCameraControl()
        {
            return new CameraWithEffects()
                {
                    BackgroundColor = Colors.Black,
                    CapturePhotoQuality = CaptureQuality.Medium,
                    Facing = CameraPosition.Default,
                    HorizontalOptions = LayoutOptions.Fill,
                    VerticalOptions = LayoutOptions.Fill,
                    ZIndex = -1,
                    ZoomLimitMax = 10,
                    ZoomLimitMin = 1,
                    ConstantUpdate = false,
                    Tag = "Camera"
                }
                .Assign(out CameraControl)
                .ObserveBindingContext<CameraWithEffects, CameraViewModel>((me, vm, prop) =>
                {
                    bool attached = prop == nameof(BindingContext);
                    if (attached || prop == nameof(vm.SelectedShader))
                    {
                        me.ShaderSource = vm.SelectedShader;
                    }
                });
        }

        SkiaLayer CreateControlsLayer()
        {
            return new SkiaLayer()
            {
                UseCache = SkiaCacheType.Operations,
                VerticalOptions = LayoutOptions.Fill,
                Children =
                {
                    CreateControlsPanel(),
                    CreateResumeHotspot()
                }
            };
        }

        SkiaShape CreateControlsPanel()
        {
            return new SkiaShape()
            {
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.End,
                Margin = new Thickness(0, 0, 0, 24),
                Padding = new Thickness(8, 0),
                HeightRequest = 60,
                StrokeColor = Colors.Black,
                StrokeWidth = -1,
                BackgroundColor = Color.Parse("#66000000"),
                CornerRadius = 32,
                Children =
                {
                    CreateControlsRow()
                }
            };
        }

        SkiaRow CreateControlsRow()
        {
            return new SkiaRow()
            {
                UseCache = SkiaCacheType.GPU,
                Padding = new Thickness(1),
                Spacing = 10,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                Children =
                {
                    CreatePreviewButton(),
                    CreateSettingsButton(),
                    CreatePowerButton(),
                    CreateEffectsButton(),
                    CreateFlashCaptureButton(),
                    CreateFlashLightButton(),
                    CreateSwitchCameraButton(),
                    CreateCaptureButton()
                }
            };
        }

        SkiaShape CreatePreviewButton()
        {
            return new SkiaShape()
                {
                    StrokeColor = Color.Parse("#66CECECE"),
                    StrokeWidth = 1,
                    Type = ShapeType.Circle,
                    HeightRequest = 46,
                    LockRatio = 1,
                    BackgroundColor = Color.Parse("#66000000"),
                    IsClippedToBounds = true,
                    UseCache = SkiaCacheType.Image,
                    VerticalOptions = LayoutOptions.Start,
                    WidthRequest = 46,
                    Children =
                    {
                        new SkiaImage()
                            {
                                RescalingQuality = SKFilterQuality.None,
                                Aspect = TransformAspect.AspectCover,
                                HorizontalOptions = LayoutOptions.Fill,
                                VerticalOptions = LayoutOptions.Fill,
                                Tag = "Preview"
                            }
                            .Assign(out ImagePreview)
                            .ObserveBindingContext<SkiaImage, CameraViewModel>((me, vm, prop) =>
                            {
                                bool attached = prop == nameof(BindingContext);
                                if (attached || prop == nameof(vm.DisplayPreview))
                                {
                                    me.ImageBitmap = vm.DisplayPreview;
                                }
                            })
                    }
                }
                .ObserveBindingContext<SkiaShape, CameraViewModel>((me, vm, prop) =>
                {
                    bool attached = prop == nameof(BindingContext);
                    if (attached && vm.CommandPreviewTapped != null)
                    {
                        me.OnTapped(shape => vm.CommandPreviewTapped.Execute(null));
                    }
                });
        }

        SkiaShape CreateSettingsButton()
        {
            return new SkiaShape()
                {
                    StrokeColor = Color.Parse("#66CECECE"),
                    StrokeWidth = 1,
                    UseCache = SkiaCacheType.Image,
                    Type = ShapeType.Circle,
                    HeightRequest = 46,
                    LockRatio = 1,
                    BackgroundColor = Colors.Black,
                    Children =
                    {
                        new SkiaSvg()
                        {
                            SvgString = App.Current.Resources.Get<string>("SvgSettings"),
                            TintColor = Color.Parse("#CECECE"),
                            WidthRequest = 18,
                            LockRatio = 1,
                            HorizontalOptions = LayoutOptions.Center,
                            VerticalOptions = LayoutOptions.Center
                        }
                    }
                }
                .OnTapped(me =>
                {
                    if (SelectedFormat == null || CameraControl.PermissionsError)
                    {
                        //camera error
                        try
                        {
                            CameraControl.IsOn = true;
                        }
                        catch (Exception e)
                        {
                            Super.Log(e);
                        }

                        Tasks.StartDelayed(TimeSpan.FromMilliseconds(500), () =>
                        {
                            if (CameraControl.PermissionsError)
                            {
                                MainThread.BeginInvokeOnMainThread(async () =>
                                {
                                    await DisplayAlert("Error", $"No permissions", "OK");

#if ANDROID || IOS
                            NativeTasks.OpenSystemSettings();
#endif
                                });
                            }
                        });
                        return;
                    }

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        var popup = new SettingsPopup(this);
                        this.ShowPopup(popup);
                    });
                });
        }

        SkiaShape CreatePowerButton()
        {
            return new SkiaShape()
                {
                    UseCache = SkiaCacheType.Image,
                    IsVisible = false,
                    Type = ShapeType.Circle,
                    HeightRequest = 46,
                    LockRatio = 1,
                    BackgroundColor = Colors.Black,
                    Children =
                    {
                        new SkiaLabel("P")
                        {
                            VerticalOptions = LayoutOptions.Center,
                            HorizontalOptions = LayoutOptions.Center,
                            TextColor = Colors.White
                        }
                    }
                }
                .OnTapped(me => TappedTurnCamera());
        }

        SkiaShape CreateEffectsButton()
        {
            return new SkiaShape()
                {
                    UseCache = SkiaCacheType.Image,
                    IsVisible = false,
                    Type = ShapeType.Circle,
                    HeightRequest = 46,
                    LockRatio = 1,
                    BackgroundColor = Colors.Black,
                    Children =
                    {
                        new SkiaLabel("E")
                        {
                            VerticalOptions = LayoutOptions.Center,
                            HorizontalOptions = LayoutOptions.Center,
                            TextColor = Colors.White
                        }
                    }
                }
                .OnTapped(me =>
                {
                    var current = CameraControl.Effect;
                    var currentIndex = CameraControl.AvailableEffects.IndexOf(current);

                    // Move to next effect, wrap around to beginning if at end
                    var nextIndex = (currentIndex + 1) % CameraControl.AvailableEffects.Count;

                    CameraControl.SetEffect(CameraControl.AvailableEffects[nextIndex]);
                });
        }

        SkiaShape CreateFlashCaptureButton()
        {
            return new SkiaShape()
                {
                    StrokeColor = Color.Parse("#66CECECE"),
                    StrokeWidth = 1,
                    UseCache = SkiaCacheType.Image,
                    Type = ShapeType.Circle,
                    HeightRequest = 46,
                    LockRatio = 1,
                    BackgroundColor = Colors.Black,
                    Children =
                    {
                        new SkiaSvg()
                            {
                                SvgString = App.Current.Resources.Get<string>("SvgFlashAuto"),
                                TintColor = Color.Parse("#CECECE"),
                                WidthRequest = 19,
                                LockRatio = 1,
                                HorizontalOptions = LayoutOptions.Center,
                                VerticalOptions = LayoutOptions.Center
                            }
                            .Assign(out SvgFlashCapture)
                    }
                }
                .OnTapped(me => OnFlashClicked(this, EventArgs.Empty));
        }

        SkiaShape CreateFlashLightButton()
        {
            return new SkiaShape()
                {
                    StrokeColor = Color.Parse("#CECECE"),
                    StrokeWidth = 1,
                    IsVisible = false,
                    UseCache = SkiaCacheType.Image,
                    Type = ShapeType.Circle,
                    HeightRequest = 46,
                    LockRatio = 1,
                    BackgroundColor = Colors.Black,
                    Children =
                    {
                        new SkiaSvg()
                            {
                                SvgString = App.Current.Resources.Get<string>("SvgLightOff"),
                                TintColor = Color.Parse("#CECECE"),
                                WidthRequest = 18,
                                LockRatio = 1,
                                HorizontalOptions = LayoutOptions.Center,
                                VerticalOptions = LayoutOptions.Center
                            }
                            .Assign(out SvgFlashLight)
                    }
                }
                .OnTapped(me => TappedFlash());
        }

        SkiaShape CreateSwitchCameraButton()
        {
            return new SkiaShape()
                {
                    StrokeColor = Color.Parse("#66CECECE"),
                    StrokeWidth = 1,
                    UseCache = SkiaCacheType.Image,
                    Type = ShapeType.Circle,
                    HeightRequest = 46,
                    LockRatio = 1,
                    BackgroundColor = Colors.Black,
                    Children =
                    {
                        new SkiaSvg()
                        {
                            SvgString = App.Current.Resources.Get<string>("SvgSource"),
                            TintColor = Color.Parse("#CECECE"),
                            WidthRequest = 18,
                            LockRatio = 1,
                            HorizontalOptions = LayoutOptions.Center,
                            VerticalOptions = LayoutOptions.Center
                        }
                    }
                }
                .OnTapped(me => TappedSwitchCamera());
        }

        SkiaShape CreateCaptureButton()
        {
            return new SkiaShape()
                {
                    UseCache = SkiaCacheType.Image,
                    Type = ShapeType.Circle,
                    HeightRequest = 46,
                    LockRatio = 1,
                    StrokeWidth = 2,
                    StrokeColor = Color.Parse("#66CECECE"),
                    BackgroundColor = Colors.Black,
                    Padding = new Thickness(3),
                    Children =
                    {
                        new SkiaShape()
                            {
                                BackgroundColor = Color.Parse("#CECECE"),
                                Type = ShapeType.Circle,
                                HorizontalOptions = LayoutOptions.Fill,
                                VerticalOptions = LayoutOptions.Fill
                            }
                            .Assign(out ButtonCapture)
                    }
                }
                .ObserveBindingContext<SkiaShape, CameraViewModel>((me, vm, prop) =>
                {
                    bool attached = prop == nameof(BindingContext);
                    if (attached && vm.CommandCaptureStillPhoto != null)
                    {
                        me.OnTapped(shape => vm.CommandCaptureStillPhoto.Execute(null));
                    }
                });
        }

        SkiaHotspot CreateResumeHotspot()
        {
            return new SkiaHotspot()
                {
                    HorizontalOptions = LayoutOptions.Center,
                    LockRatio = 1,
                    VerticalOptions = LayoutOptions.Center,
                    WidthRequest = 290,
                    ZIndex = 110
                }
                .OnTapped(me => TappedResume())
                .ObserveBindingContext<SkiaHotspot, CameraViewModel>((me, vm, prop) =>
                {
                    bool attached = prop == nameof(BindingContext);
                    if (attached || prop == nameof(vm.ShowResume))
                    {
                        me.IsVisible = vm.ShowResume;
                    }
                });
        }

        SkiaHotspotZoom CreateZoomHotspot()
        {
            return new SkiaHotspotZoom()
                {
                    ZoomMax = 3,
                    ZoomMin = 1
                }
                .Initialize(hotspot => { hotspot.Zoomed += OnZoomed; });
        }

        SkiaDrawer CreateShaderDrawer()
        {
            return new SkiaDrawer()
                {
                    Margin = new Thickness(0, 0, 0, 100),
                    HeaderSize = 40,
                    Direction = DrawerDirection.FromLeft,
                    VerticalOptions = LayoutOptions.End,
                    HorizontalOptions = LayoutOptions.Fill,
                    HeightRequest = 100,
                    IsOpen = false,
                    IgnoreWrongDirection = true,
                    ZIndex = 50,
                    Content = CreateDrawerContent()
                }
                .Assign(out ShaderDrawer);
        }

        SkiaShape CreateDrawerContent()
        {
            return new SkiaShape()
            {
                Type = ShapeType.Rectangle,
                CornerRadius = new CornerRadius(0, 12, 12, 0),
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill,
                Children =
                {
                    CreateDrawerLayout()
                }
            };
        }

        SkiaLayout CreateDrawerLayout()
        {
            return new SkiaLayout()
            {
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill,
                Children =
                {
                    CreateShaderScroll(),
                    CreateDrawerHeader()
                }
            };
        }

        SkiaScroll CreateShaderScroll()
        {
            return new SkiaScroll()
                {
                    BackgroundColor = Colors.WhiteSmoke,
                    Margin = new Thickness(0, 0, 20, 0),
                    Orientation = ScrollOrientation.Horizontal,
                    HorizontalOptions = LayoutOptions.Fill,
                    VerticalOptions = LayoutOptions.Fill,
                    Padding = new Thickness(8),
                    Content = CreateShaderItemsLayout()
                }
                .ObserveProperty(() => ShaderDrawer, nameof(ShaderDrawer.IsOpen),
                    me => { me.RespondsToGestures = ShaderDrawer.IsOpen; });
        }

        SkiaLayoutWithSelector CreateShaderItemsLayout()
        {
            return new SkiaLayoutWithSelector()
                {
                    Type = LayoutType.Row,
                    VerticalOptions = LayoutOptions.Center,
                    Spacing = 8,
                    RecyclingTemplate = RecyclingTemplate.Enabled,
                    UseCache = SkiaCacheType.Operations,
                    ItemTemplate = CreateShaderItemTemplate()
                }
                .ObserveBindingContext<SkiaLayoutWithSelector, CameraViewModel>((me, vm, prop) =>
                {
                    bool attached = prop == nameof(BindingContext);
                    if (attached || prop == nameof(vm.ShaderItems))
                    {
                        me.ItemsSource = vm.ShaderItems;
                    }
                });
        }

        DataTemplate CreateShaderItemTemplate()
        {
            return new DataTemplate(() =>
            {
                return new SkiaShape()
                    {
                        Type = ShapeType.Rectangle,
                        WidthRequest = 80,
                        HeightRequest = 80,
                        CornerRadius = new CornerRadius(8),
                        BackgroundColor = Colors.White,
                        UseCache = SkiaCacheType.Image,
                        Children =
                        {
                            new SkiaLayout()
                            {
                                HorizontalOptions = LayoutOptions.Fill,
                                VerticalOptions = LayoutOptions.Fill,
                                Children =
                                {
                                    // IMAGE WITH SHADER EFFECT
                                    new SkiaImage()
                                        {
                                            RescalingQuality = SKFilterQuality.None,
                                            Aspect = TransformAspect.AspectCover,
                                            HorizontalOptions = LayoutOptions.Fill,
                                            VerticalOptions = LayoutOptions.Fill,
                                            UseCache = SkiaCacheType.Image,
                                            VisualEffects =
                                            {
                                                new SkiaShaderEffect()
                                            }
                                        }
                                        .ObserveBindingContext<SkiaImage, ShaderItem>((me, item, prop) =>
                                        {
                                            bool attached = prop == nameof(BindingContext);
                                            if (attached)
                                            {
                                                me.Source = item?.ImageSource;

                                                // Update shader effect
                                                if (me.VisualEffects.Count > 0 &&
                                                    me.VisualEffects[0] is SkiaShaderEffect shader)
                                                {
                                                    shader.ShaderSource = item?.Filename;
                                                }
                                            }
                                        })
                                        .ObserveProperty( this, nameof(SmallPreview), (me) =>
                                        {
                                            me.ImageBitmap = SmallPreview;
                                        }),

                                    // SHADOW BACKGROUND FOR TEXT
                                    new SkiaShape()
                                    {
                                        Type = ShapeType.Rectangle,
                                        BackgroundColor = Color.FromArgb("#80000000"),
                                        HorizontalOptions = LayoutOptions.Fill,
                                        VerticalOptions = LayoutOptions.End,
                                        HeightRequest = 20
                                    },

                                    // TITLE LABEL
                                    new SkiaLabel()
                                        {
                                            FontSize = 12,
                                            TextColor = Colors.White,
                                            HorizontalOptions = LayoutOptions.Center,
                                            VerticalOptions = LayoutOptions.End,
                                            Margin = new Thickness(4),
                                            HorizontalTextAlignment = DrawTextAlignment.Center,
                                            UseCache = SkiaCacheType.Operations
                                        }
                                        .ObserveBindingContext<SkiaLabel, ShaderItem>((me, item, prop) =>
                                        {
                                            bool attached = prop == nameof(BindingContext);
                                            if (attached)
                                            {
                                                me.Text = item?.Title;
                                            }
                                        })
                                }
                            }
                        }
                    }
                    .OnTapped((sender, args) =>
                    {
                        if (sender.BindingContext is ShaderItem item && BindingContext is CameraViewModel vm)
                        {
                            vm.CommandSelectShader.Execute(item);
                        }
                    })
                    .OnLongPressing(me  =>
                    {
                        if (me.BindingContext is ShaderItem item && CameraControl?.CommandEditShader != null)
                        {
                            CameraControl.CommandEditShader.Execute(item);
                        }
                    });
            });
        }

        SkiaShape CreateDrawerHeader()
        {
            return new SkiaShape()
                {
                    UseCache = SkiaCacheType.Image,
                    HorizontalOptions = LayoutOptions.End,
                    Type = ShapeType.Rectangle,
                    BackgroundColor = Colors.WhiteSmoke,
                    CornerRadius = new CornerRadius(0, 16, 0, 0),
                    VerticalOptions = LayoutOptions.Fill,
                    WidthRequest = 41,
                    Children =
                    {
                        new SkiaLayout()
                        {
                            HorizontalOptions = LayoutOptions.Fill,
                            VerticalOptions = LayoutOptions.Fill,
                            Children =
                            {
                                new SkiaShape()
                                {
                                    Type = ShapeType.Rectangle,
                                    WidthRequest = 4,
                                    HeightRequest = 40,
                                    BackgroundColor = Color.Parse("#CCCCCC"),
                                    CornerRadius = 2,
                                    HorizontalOptions = LayoutOptions.Center,
                                    VerticalOptions = LayoutOptions.Center
                                }
                            }
                        }
                    }
                }
                .OnTapped(me => TappedDrawerHeader());
        }

        SkiaLabelFps CreateDebugFps()
        {
            return new SkiaLabelFps()
                {
                    Margin = new Thickness(0, 0, 4, 24),
                    BackgroundColor = Colors.Black,
                    ForceRefresh = false,
                    HorizontalOptions = LayoutOptions.End,
                    Rotation = -45,
                    TextColor = Colors.White,
                    VerticalOptions = LayoutOptions.End,
                    ZIndex = 100
                }
                .ObserveBindingContext<SkiaLabelFps, CameraViewModel>((me, vm, prop) =>
                {
                    bool attached = prop == nameof(BindingContext);
                    if (attached || prop == nameof(vm.IsDebug))
                    {
                        me.IsVisible = vm.IsDebug;
                    }
                });
        }

        #endregion

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
                    CameraControl.CaptureFormatIndex = format;
                    CameraControl.CapturePhotoQuality = CaptureQuality.Manual;
                }
                else
                {
                    CameraControl.CapturePhotoQuality = CaptureQuality.Medium;
                }
            }
        }

        public void OpenHelp()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var popup = new HelpPopup();
                this.ShowPopup(popup);
                UserSettings.Save();
            });
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
                var color = CameraControl.IsBusy ? Colors.DarkRed : Color.Parse("#CECECE");
                ButtonCapture.BackgroundColor = color;
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
                                CameraControl.CaptureFormatIndex = selectedIndex;
                                CameraControl.CapturePhotoQuality = CaptureQuality.Manual;
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

            CameraControl.IsMirrored = IsMirrored;

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

        void SyncUi()
        {
            ApplyAspect();

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