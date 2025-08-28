using Newtonsoft.Json.Linq;
using ShadersCamera.Helpers;

namespace ShadersCamera.Views;

public partial class SettingsPopup 
{
    private readonly IPageWIthCamera _parentCameraPage;

    private bool isInitializing;

    public SettingsPopup(IPageWIthCamera cameraPage)
	{
		InitializeComponent();

        _parentCameraPage = cameraPage;

        isInitializing = true;

        //switches
        FullScreenSwitch.IsToggled = _parentCameraPage.IsFullScreen;
        MirrorSwitch.IsToggled = _parentCameraPage.IsMirrored;

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

    private void TappedSelectHelp(object sender, ControlTappedEventArgs e)
    {
        _parentCameraPage.OpenHelp();
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

    private void MirrorSwitch_OnToggled(object sender, bool value)
    {
        if (isInitializing)
            return;

        _parentCameraPage.SetMirrored(value);
    }
}