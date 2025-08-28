using DrawnUi.Camera;

namespace ShadersCamera.Views;

/// <summary>
/// For external control
/// </summary>
public interface IPageWIthCamera
{
    bool IsFullScreen { get; }
    
    void SetAspect(bool fullScreen);

    bool IsMirrored { get; }

    void SetMirrored(bool value);

    CaptureFormat SelectedFormat { get; }
    
    void SelectFormat(Action<string> changed);

    void OpenHelp();
}