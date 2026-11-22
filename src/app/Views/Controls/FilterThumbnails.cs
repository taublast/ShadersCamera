using DrawnUi.Infrastructure;
using DrawnUi.Views;
using ShadersCamera.Models;

namespace ShadersCamera.Views.Controls
{
    /// <summary>
    /// Bakes the shader strip thumbnails: ONE clean camera frame through EVERY shader into small raster pictures the strip
    /// cells show as plain images. Replaces a live SkiaShaderEffect per cell: a control carrying a post-renderer effect never
    /// blits from its cache, so N thumbs cost N shader dispatches per frame over the live camera and the strip scroll lagged.
    /// Now the render thread only blits pictures.
    ///
    /// Baking happens on the GPU, on the render thread right after a frame (<see cref="BakeOnRenderThread"/>), a few tiles
    /// per frame under a time budget. Android bakes on the CPU worker instead (<see cref="UseGpu"/>), as does a canvas without
    /// a GPU context: there the driver compiles each shader's GPU program outside any budget.
    ///
    /// Owns the source frame and every baked picture. Latest request wins: a frame or roster that arrives mid-bake restarts
    /// the job, nothing is queued.
    /// </summary>
    public sealed class FilterThumbnails : IDisposable
    {
        /// <summary>Render-thread time spent baking per frame, after the frame was drawn.</summary>
        public static double FrameBudgetMs = 4;

        /// <summary>Bake on the canvas GPU context; off on Android, where the worker bakes on the CPU.</summary>
#if ANDROID
        public static readonly bool UseGpu = false;
#else
        public static readonly bool UseGpu = true;
#endif

        private readonly object _lock = new();
        private SKImage _source;
        private SKImage _inUse;
        private IReadOnlyList<ShaderItem> _items;
        private int _sizePx;
        private volatile bool _dirty;
        private bool _disposed;
        private int _running;
        private Dictionary<string, SKImage> _thumbs = new();
        private GpuJob _job;

        /// <summary>Raised on the main thread after a batch replaced the previous pictures.</summary>
        public event EventHandler Updated;

        /// <summary>Set by the page to ask its canvas for a frame: switches baking to the render thread (GPU). Null = CPU worker.</summary>
        public Action RequestFrame { get; set; }

        /// <summary>
        /// Frame-based disposal queue of the canvas that draws the strip, set by the page. Every picture handed out and every
        /// GPU object of a bake is released through it, never disposed directly: a cell that just swapped pictures can still
        /// be drawing the old one in a frame in flight.
        /// </summary>
        public IDisposeManager DisposeManager { get; set; }

        /// <summary>True once a frame was handed in, so the caller can decide to seed the sample.</summary>
        public bool HasSource
        {
            get
            {
                lock (_lock)
                {
                    return _source != null;
                }
            }
        }

        /// <summary>The cache key of an item: its shader file (the roster leaves Id at 0), else its title.</summary>
        public static string KeyOf(ShaderItem item) => item == null ? null : item.Filename ?? item.Title;

        /// <summary>
        /// Baked picture for an item, if the last batch had it. Any thread. Draw it in the current frame only: never dispose
        /// it and never store it, a newer batch releases it a few frames later. See <see cref="FilterThumbView"/>.
        /// </summary>
        public bool TryGet(string id, out SKImage image)
        {
            image = null;
            return id != null && _thumbs.TryGetValue(id, out image);
        }

        /// <summary>New clean frame, any thread. Takes ownership of <paramref name="frame"/>.</summary>
        public void SetSource(SKImage frame)
        {
            if (frame == null)
                return;

            lock (_lock)
            {
                if (_disposed)
                {
                    Release(frame);
                    return;
                }

                var old = _source;
                _source = frame;
                _dirty = true;

                // a bake in flight still reads the old frame; it is dropped when that bake ends
                if (old != null && old != frame && old != _inUse)
                    Release(old);
            }

            Kick();
        }

        /// <summary>Shaders to bake and the pixel size of one thumb. Any thread.</summary>
        public void SetItems(IReadOnlyList<ShaderItem> items, int sizePx)
        {
            lock (_lock)
            {
                _items = items?.Where(x => x != null).ToList();
                _sizePx = sizePx;
                _dirty = true;
            }

            Kick();
        }

        private void Kick()
        {
            var requestFrame = RequestFrame;
            if (UseGpu && requestFrame != null)
            {
                requestFrame(); // the next WasDrawn picks it up
                return;
            }

            if (Interlocked.CompareExchange(ref _running, 1, 0) == 0)
                Task.Run(Worker);
        }

        // ─── GPU path (render thread) ────────────────────────────────────────

        private sealed class GpuJob
        {
            public SKImage Source;
            public List<ShaderItem> Items;
            public int Size;
            public int Index;
            public SKSurface Surface;
            public Dictionary<string, SKImage> Result;
            public SKPaint Paint;
        }

        /// <summary>
        /// Render-thread entry, call it from the canvas WasDrawn. Bakes tiles of the pending roster on the canvas GPU context
        /// until <see cref="FrameBudgetMs"/> is spent, continues next frame, publishes when the roster is complete. A flag
        /// check when nothing is pending.
        /// </summary>
        public void BakeOnRenderThread(GRContext grContext)
        {
            if (!UseGpu || !_dirty && _job == null)
                return;

            if (grContext == null)
            {
                // no GPU context on this canvas: the worker does it on the CPU
                if (_dirty && Interlocked.CompareExchange(ref _running, 1, 0) == 0)
                    Task.Run(Worker);
                return;
            }

            if (_dirty)
            {
                lock (_lock)
                {
                    if (_disposed || _source == null || _items == null || _items.Count == 0 || _sizePx <= 0)
                        return; // stays dirty until the missing piece arrives

                    _dirty = false;
                    DiscardJob(); // latest wins
                    _job = new GpuJob
                    {
                        Source = _source,
                        Items = Distinct(_items),
                        Size = _sizePx,
                        Result = new Dictionary<string, SKImage>(_items.Count),
                    };
                    _inUse = _source;
                }
            }

            var job = _job;
            if (job == null)
                return;

            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                if (job.Surface == null)
                {
                    var info = new SKImageInfo(job.Size, job.Size, SKColorType.Rgba8888, SKAlphaType.Premul);
                    job.Surface = SKSurface.Create(grContext, false, info);
                    job.Paint = new SKPaint();
                    if (job.Surface == null)
                    {
                        // context refused a surface: CPU fallback, still correct
                        FinishJob(job, Bake(job.Source, job.Items, job.Size));
                        return;
                    }
                }

                while (job.Index < job.Items.Count)
                {
                    BakeTile(job, job.Items[job.Index]);
                    job.Index++;

                    if (sw.Elapsed.TotalMilliseconds > FrameBudgetMs)
                        break;
                }
            }
            catch (Exception e)
            {
                Super.Log(e);
                lock (_lock)
                {
                    DiscardJob();
                }
                return;
            }

            if (job.Index >= job.Items.Count)
                FinishJob(job, job.Result);
            else
                RequestFrame?.Invoke(); // more tiles next frame
        }

        /// <summary>One tile: frame, then the shader over it, then a small readback into a plain raster picture.</summary>
        private void BakeTile(GpuJob job, ShaderItem item)
        {
            var canvas = job.Surface.Canvas;
            var rect = SKRect.Create(0, 0, job.Size, job.Size);

            // plain frame first: a shader that failed to compile leaves the clean thumb
            canvas.DrawImage(job.Source, rect, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None));

            var shader = CreateShader(item);
            SKShader built = null;
            try
            {
                built = shader?.ApplyTo(job.Paint, job.Source, job.Size, job.Size);
                if (built != null)
                    canvas.DrawRect(rect, job.Paint);

                canvas.Flush();
            }
            finally
            {
                job.Paint.Shader = null;
                Release(built);
                Release(shader);
            }

            using var snapshot = job.Surface.Snapshot();
            var picture = snapshot.ToRasterImage(true); // cells cache on the CPU; a texture would not survive a context loss
            if (picture != null)
                job.Result[KeyOf(item)] = picture;
        }

        private void FinishJob(GpuJob job, Dictionary<string, SKImage> result)
        {
            lock (_lock)
            {
                if (_job == job)
                    _job = null;

                Release(job.Surface);
                Release(job.Paint);
                _inUse = null;
                if (_source != job.Source)
                    Release(job.Source); // replaced while baking
            }

            if (result != null)
                Publish(result);
        }

        /// <summary>Under _lock. Drops a job in flight, its partial pictures included.</summary>
        private void DiscardJob()
        {
            var job = _job;
            if (job == null)
                return;

            _job = null;
            Release(job.Surface);
            Release(job.Paint);
            ReleaseAll(job.Result);
            _inUse = null;
            if (_source != job.Source)
                Release(job.Source);
        }

        private static List<ShaderItem> Distinct(IReadOnlyList<ShaderItem> items)
        {
            var list = new List<ShaderItem>(items.Count);
            var seen = new HashSet<string>();
            foreach (var item in items)
            {
                if (KeyOf(item) is { } key && seen.Add(key))
                    list.Add(item);
            }

            return list;
        }

        // ─── CPU path (worker thread) ────────────────────────────────────────

        private void Worker()
        {
            try
            {
                while (true)
                {
                    SKImage source;
                    IReadOnlyList<ShaderItem> items;
                    int size;

                    lock (_lock)
                    {
                        if (!_dirty || _disposed)
                            break;

                        if (_source == null || _items == null || _items.Count == 0 || _sizePx <= 0)
                            break; // stays dirty, runs when the missing piece arrives

                        _dirty = false;
                        source = _source;
                        items = _items;
                        size = _sizePx;
                        _inUse = source;
                    }

                    Dictionary<string, SKImage> baked = null;
                    try
                    {
                        baked = Bake(source, items, size);
                    }
                    catch (Exception e)
                    {
                        Super.Log(e);
                    }

                    lock (_lock)
                    {
                        _inUse = null;
                        if (_source != source)
                            Release(source); // replaced while we were baking
                    }

                    if (baked != null)
                        Publish(baked);
                }
            }
            finally
            {
                Interlocked.Exchange(ref _running, 0);

                bool again;
                lock (_lock)
                {
                    again = _dirty && !_disposed && _source != null && _items != null;
                }

                if (again)
                    Kick(); // a request landed between the last loop check and the release
            }
        }

        private Dictionary<string, SKImage> Bake(SKImage source, IReadOnlyList<ShaderItem> items, int size)
        {
            var info = new SKImageInfo(size, size, SKColorType.Rgba8888, SKAlphaType.Premul);
            using var surface = SKSurface.Create(info);
            var canvas = surface.Canvas;
            var rect = SKRect.Create(0, 0, size, size);
            var sampling = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None);
            var result = new Dictionary<string, SKImage>(items.Count);

            foreach (var item in items)
            {
                var key = KeyOf(item);
                if (key == null || result.ContainsKey(key))
                    continue;

                canvas.DrawImage(source, rect, sampling);

                var shader = CreateShader(item);
                shader?.DrawRect(canvas, source, rect);
                Release(shader);

                result[key] = surface.Snapshot();
            }

            return result;
        }

        // ─── shared ──────────────────────────────────────────────────────────

        /// <summary>The item's shader: its edited code when the editor changed it, else its bundled file.</summary>
        private static SkiaShader CreateShader(ShaderItem item)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(item.ShaderCode))
                    return SkiaShader.FromCode(item.ShaderCode, OnShaderError);

                return string.IsNullOrWhiteSpace(item.Filename) ? null : SkiaShader.FromResource(item.Filename, true, OnShaderError);
            }
            catch (Exception e)
            {
                Super.Log(e);
                return null;
            }
        }

        private static void OnShaderError(string error)
        {
            Super.Log($"[FilterThumbnails] {error}");
        }

        private void Publish(Dictionary<string, SKImage> baked)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (_disposed)
                {
                    ReleaseAll(baked);
                    return;
                }

                var old = _thumbs;
                _thumbs = baked;
                Updated?.Invoke(this, EventArgs.Empty);

                // cells swapped to the new pictures synchronously in Updated, but a frame already in flight can still
                // draw the old ones: the canvas queue counts rendered frames
                ReleaseAll(old);
            });
        }

        /// <summary>Any thread. Hands a resource to <see cref="DisposeManager"/>.</summary>
        private void Release(IDisposable resource)
        {
            if (resource == null)
                return;

            var manager = DisposeManager;
            if (manager != null)
                manager.DisposeObject(resource);
            else
                resource.Dispose(); // not wired to a canvas yet, so nothing can be drawing it
        }

        private void ReleaseAll(Dictionary<string, SKImage> images)
        {
            if (images == null)
                return;

            foreach (var image in images.Values)
                Release(image);
        }

        public void Dispose()
        {
            SKImage source;
            lock (_lock)
            {
                if (_disposed)
                    return;
                _disposed = true;
                DiscardJob();
                source = _source != _inUse ? _source : null; // the in-use frame is dropped by the worker
                _source = null;
            }

            Release(source);

            var thumbs = _thumbs;
            _thumbs = new();
            ReleaseAll(thumbs);
        }
    }
}
