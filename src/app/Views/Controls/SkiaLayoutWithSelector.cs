namespace ShadersCamera.Views.Controls;

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

    public static readonly BindableProperty SelectedIndexProperty = BindableProperty.Create(
        nameof(SelectedIndex),
        typeof(int),
        typeof(SkiaScroll),
        -1, propertyChanged: NeedDraw);

    /// <summary>
    /// Can be set from anywhere. WIll be set by our page cell tap handler maybe.
    /// </summary>
    public int SelectedIndex
    {
        get { return (int)GetValue(SelectedIndexProperty); }
        set { SetValue(SelectedIndexProperty, value); }
    }


    //drawn selector on top
    protected override void OnAfterDrawingVisibleChildren(DrawingContext ctx, LayoutStructure structure, List<ControlInStack> visibleElements)
    {
        base.OnAfterDrawingVisibleChildren(ctx, structure, visibleElements);

        if (visibleElements.Count>0 && Selector != null && SelectedIndex >= 0)
        {
            //todo draw selector overlay
            var current = visibleElements.FirstOrDefault(x => x.ControlIndex == SelectedIndex);
            if (current != null)
            {
                var area = new SKRect(current.Drawn.Left, current.Drawn.Top, current.Drawn.Right, current.Drawn.Bottom);
                area.Inflate(new (2 * RenderingScale, 2*RenderingScale));
                Selector.Render(ctx.WithDestination(new(current.Drawn.Left, current.Drawn.Top, current.Drawn.Right, current.Drawn.Bottom)));
            }
        }
    }

    //can draw selector below
    /*
    protected override void OnBeforeDrawingVisibleChildren(DrawingContext ctx, LayoutStructure structure,
        List<ControlInStack> visibleElements)
    {
        base.OnBeforeDrawingVisibleChildren(ctx, structure, visibleElements);

        if (visibleElements.Count > 0 && Selector != null && SelectedIndex >= 0)
        {
            //...
        }
    }
    */
}