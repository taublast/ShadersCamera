using ShadersCamera.Helpers;

namespace ShadersCamera.Views;

public partial class SettingsPopup 
{
    private readonly MainCameraPage _parentCameraPage;

    private bool isInitializing;

    public SettingsPopup(MainCameraPage cameraPage)
	{
		InitializeComponent();

        _parentCameraPage = cameraPage;

        isInitializing = true;

        //switch
        FullScreenSwitch.IsToggled = _parentCameraPage.IsFullScreen;

        //format
        var format = _parentCameraPage.SelectedFormat;
        FormatLabel.Text = $"{format.Width}x{format.Height}, {format.AspectRatioString}";

        isInitializing = false;
    }


    private void FullScreenSwitch_OnToggled(object sender, bool value)
    {
        if (isInitializing)
            return;

        _parentCameraPage.SetAspect(value);
    }

    private void TappedSelectFormat(object sender, ControlTappedEventArgs e)
    {
        _parentCameraPage.SelectFormat((name) =>
        {
            FormatLabel.Text = name;
        });
    }

    protected override Task OnDismissedByTappingOutsideOfPopup(CancellationToken token = new CancellationToken())
    {
        UserSettings.Save();

        return base.OnDismissedByTappingOutsideOfPopup(token);
    }
}