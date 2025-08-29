namespace ShadersCamera.Views.Controls;

/// <summary>
/// Will apply shader to scaled source only and not to the whole control area
/// to avoid black borders when we FIT inside viewport
/// </summary>
public class ClippedShaderEffect : SkiaShaderEffect
{
    private SkiaImage _image;

    public ClippedShaderEffect(SkiaImage image)
    {
        _image = image;

    }

    protected override void OnDisposing()
    {
        _image = null;

        base.OnDisposing();
    }

    public override void Render(DrawingContext ctx)
    {
        if (_image != null)
        {
            var clipped = ctx.Destination;
            clipped.Intersect(new ((int)Math.Round(_image.DisplayRect.Left), (int)Math.Round(_image.DisplayRect.Top), (int)Math.Round(_image.DisplayRect.Right), (int)Math.Round(_image.DisplayRect.Bottom)));
            base.Render(ctx.WithDestination(clipped));
        }
    }
}