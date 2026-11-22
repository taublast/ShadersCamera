using DrawnUi.Infrastructure;

namespace ShadersCamera.Views.Controls
{
    /// <summary>
    /// Strip cell picture. Draws the baked thumb for <see cref="ItemId"/> straight out of <see cref="FilterThumbnails"/> at
    /// paint time and keeps no reference to it after the frame: the cache disposes superseded pictures through the canvas
    /// frame-based queue, which is only safe for a consumer that does not hold them (a SkiaImage holds its bitmap until it is
    /// drawn again, and a cell scrolled out of the strip may not draw for a long time).
    ///
    /// Draws nothing while the cache has no picture for the item, so the sample image below shows.
    /// The parent cell must stay on an Image cache: an Operations cache would record a reference.
    /// </summary>
    public class FilterThumbView : SkiaControl
    {
        private static readonly SKSamplingOptions Sampling = new(SKFilterMode.Linear, SKMipmapMode.None);

        /// <summary>Picture owner, set once by the cell template.</summary>
        public FilterThumbnails Thumbnails { get; set; }

        /// <summary>Item key whose picture this view draws (<see cref="FilterThumbnails.KeyOf"/>). Call Update() after changing it.</summary>
        public string ItemId { get; set; }

        protected override void Paint(DrawingContext ctx)
        {
            base.Paint(ctx);

            if (Thumbnails == null || !Thumbnails.TryGet(ItemId, out var picture) || picture == null)
                return;

            // aspect cover: centered crop of the picture that fills the destination
            var dst = ctx.Destination;
            var scale = Math.Max(dst.Width / picture.Width, dst.Height / picture.Height);
            var w = dst.Width / scale;
            var h = dst.Height / scale;
            var src = SKRect.Create((picture.Width - w) / 2f, (picture.Height - h) / 2f, w, h);

            ctx.Context.Canvas.DrawImage(picture, src, dst, Sampling);
        }
    }
}
