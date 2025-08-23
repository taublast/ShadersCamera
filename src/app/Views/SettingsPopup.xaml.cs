namespace ShadersCamera.Views;

public partial class SettingsPopup 
{
    private readonly MainPage _parentPage;

    private bool isInitializing;

    public SettingsPopup(MainPage page)
	{
		InitializeComponent();

        _parentPage = page;

        isInitializing = true;

        //switch
        FullScreenSwitch.IsToggled = _parentPage.IsFullScreen;

        //format
        var format = _parentPage.SelectedFormat;
        FormatLabel.Text = $"{format.Width}x{format.Height}, {format.AspectRatioString}";

        isInitializing = false;
    }


    private void FullScreenSwitch_OnToggled(object sender, bool value)
    {
        if (isInitializing)
            return;

        _parentPage.SetAspect(value);
    }

    private void TappedSelectFormat(object sender, ControlTappedEventArgs e)
    {
        _parentPage.SelectFormat((name) =>
        {
            FormatLabel.Text = name;
        });
    }
}