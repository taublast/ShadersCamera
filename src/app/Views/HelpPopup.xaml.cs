namespace ShadersCamera.Views;

public partial class HelpPopup 
{
	public HelpPopup()
	{
		InitializeComponent();
	}

    private void TappedClosePopup(object sender, ControlTappedEventArgs e)
    {
       this.Close();
    }
}