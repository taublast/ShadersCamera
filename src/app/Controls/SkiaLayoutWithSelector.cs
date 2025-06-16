using static Android.Preferences.PreferenceActivity;

namespace ShadersCamera.Controls;

/// <summary>
/// Subclassed Hstack to be able to draw a custom selector below/in-between children
/// </summary>
public class SkiaLayoutWithSelector : SkiaLayout
{
    public static readonly BindableProperty SelectorProperty = BindableProperty.Create(
        nameof(Selector),
        typeof(SkiaControl),
        typeof(SkiaScroll),
        null, propertyChanged: NeedDraw);

    public SkiaControl Selector
    {
        get { return (SkiaControl)GetValue(SelectorProperty); }
        set { SetValue(SelectorProperty, value); }
    }

    /// <summary>
    /// Can be set from anywhere. WIll be set by our page cell tap handler maybe.
    /// </summary>
    public int SelectedIndex { get; set; } = -1;

    protected override void OnBeforeDrawingVisibleChildren(DrawingContext ctx, LayoutStructure structure,
        List<ControlInStack> visibleElements)
    {
        base.OnBeforeDrawingVisibleChildren(ctx, structure, visibleElements);

        if (Selector != null && SelectedIndex >= 0 && visibleElements.Any(x => x.ControlIndex == SelectedIndex))
        {
            //todo draw selector

        }
    }
}