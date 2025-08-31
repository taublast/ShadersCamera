using AppoMobi.Specials;
using DrawnUi.Camera;
using DrawnUi.Controls;
using DrawnUi.Views;
using FastPopups;
using ShadersCamera.Helpers;
using ShadersCamera.Models;
using ShadersCamera.ViewModels;
using ShadersCamera.Views.Controls;
using Canvas = DrawnUi.Views.Canvas;

namespace ShadersCamera.Views
{
    public partial class MainCameraPageFluent : BasePageReloadable, IPageWIthCamera
    {
        Canvas Canvas;
        CameraWithEffects CameraControl;

        //static for Hot Preview
        public static SkiaViewSwitcher? ViewsContainer;

        SkiaShape ButtonCapture;
        SkiaSvg SvgFlashCapture;
        SkiaSvg SvgFlashLight;
        SkiaDrawer ShaderDrawer;
        SkiaImage ImagePreview;
        SkiaScroll MainScroll;

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
#if xDEBUG
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

        SkiaLayout CreateMainLayout()
        {
            return new SkiaLayout()
            {
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill,
                Children =
                {
                    CreateCameraLayer(),

                    new SkiaDrawer()
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
                            Content = new SkiaShape()
                            {
                                Type = ShapeType.Rectangle,
                                CornerRadius = new CornerRadius(0, 12, 12, 0),
                                HorizontalOptions = LayoutOptions.Fill,
                                VerticalOptions = LayoutOptions.Fill,
                                Children =
                                {
                                    new SkiaLayout()
                                    {
                                        HorizontalOptions = LayoutOptions.Fill,
                                        VerticalOptions = LayoutOptions.Fill,
                                        Children =
                                        {
                                            new SkiaScroll()
                                                {
                                                    BackgroundColor = Colors.WhiteSmoke,
                                                    Margin = new Thickness(0, 0, 20, 0),
                                                    Orientation = ScrollOrientation.Horizontal,
                                                    HorizontalOptions = LayoutOptions.Fill,
                                                    VerticalOptions = LayoutOptions.Fill,
                                                    Padding = new Thickness(8),
                                                    Header = new SkiaLayout()
                                                    {
                                                        VerticalOptions = LayoutOptions.Fill,
                                                        WidthRequest = 8
                                                    },
                                                    Footer = new SkiaLayout()
                                                    {
                                                        VerticalOptions = LayoutOptions.Fill,
                                                        WidthRequest = 8
                                                    },
                                                    Content = CreateShaderItemsLayout()
                                                }
                                                .Assign(out MainScroll)
                                                .ObserveProperty(() => ShaderDrawer, nameof(ShaderDrawer.IsOpen),
                                                    me => { me.RespondsToGestures = ShaderDrawer.IsOpen; }),

                                            CreateDrawerHeader()
                                        }
                                    }
                                }
                            }
                        }
                        .Assign(out ShaderDrawer)
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
                .OnTapped(me => { TriggerUpdateSmallPreview = true; });
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
                                        .ObserveProperty(this, nameof(SmallPreview),
                                            (me) => { me.ImageBitmap = SmallPreview; }),

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
                    .OnLongPressing(me =>
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

        public void OpenHelp()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var popup = new HelpPopup();
                this.ShowPopup(popup);
                UserSettings.Save();
            });
        }
    }
}