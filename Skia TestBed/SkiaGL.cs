using SkiaSharp;
using SkiaSharp.Views.Desktop;
using System;

namespace Skia_TestBed
{
    internal class SkiaGL : SKGLControl
    {
        Action<SKImageInfo, GRContext, SKSurface, SKCanvas> paint;
        public SkiaGL(Action<SKImageInfo, GRContext, SKSurface, SKCanvas> paint)
        {
            this.paint = paint;
            // VSync is disabled by default, who knows why, enable it
            VSync = true;
        }

        protected override void OnPaintSurface(SKPaintGLSurfaceEventArgs e)
        {
            paint?.Invoke(e.Info, GRContext, e.Surface, e.Surface.Canvas);
            Invalidate();
        }
    }
}