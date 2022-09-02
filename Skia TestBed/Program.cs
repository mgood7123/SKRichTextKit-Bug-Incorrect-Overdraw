using Microsoft.VisualBasic.Logging;
using SkiaSharp;
using System;
using System.Windows.Forms;

namespace Skia_TestBed
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            Application.Run(
                new SkiaHost(
                    (info, context, surface, canvas) => {
                        // to improve overdraw quality we only apply overdraw to non transparent final output pixels
                        // this means we need to draw twice, once in full color, another in alpha
                        // if the full color pixel has an alpha of zero we discard the result
                        var sksl = SKRuntimeEffect.Create(
                            "uniform shader input;\n" +
                            "uniform shader inputAlpha;\n" +
                            "\n" +
                            "half4 main() {\n" +
                            "    half4 color = sample(input);\n" +
                            // return zero if alpha is zero
                            "    if (color.a == 0) return vec4(0,0,0,0);\n" +
                            "    int alpha = 255.0 * sample(inputAlpha).a;\n" +
                            // return color if input alpha is 0, this means we only drawn this pixel once
                            // Skia's overdraw canvas increases the alpha of a pixel each time it drawn touched
                            // R G B A
                            "    if (alpha == 0) {\n" +
                            // apply greyscale to the overdraw canvas in order to isolate the overdraw colors
                            "       return half4(vec3((color.r + color.g + color.b) / 3), 1);\n" +
                            "    }\n" +
                            "    return half4(1,0,0,1);\n" +
                            "}\n",
                            out string err
                        );

                        if (err != null)
                        {
                            Console.WriteLine("SHADER: runtime effect compiled with errors: " + err);
                            return;
                        }

                        int w = info.Width;
                        int h = info.Height;
                        SKImageInfo offscreenInfo = new(w, h);
                        SKImageInfo offscreenAlphaInfo = new(w, h, SKColorType.Alpha8);
                        using var offscreenSurface = SKSurface.Create(offscreenInfo);
                        using var offscreenAlphaSurface = SKSurface.Create(offscreenAlphaInfo);
                        using SKCanvas imageCanvas = offscreenSurface.Canvas;
                        using SKOverdrawCanvas overdrawCanvas = new(offscreenAlphaSurface.Canvas);
                        using SKNWayCanvas nWayCanvas = new(w, h);
                        nWayCanvas.AddCanvas(overdrawCanvas);
                        nWayCanvas.AddCanvas(imageCanvas);

                        using SKPaint colorPaint = new();

                        void drawText_(SKCanvas canvas, int n, int x, int y)
                        {
                            for (int i = 0; i < n; i++)
                            {
                                string text = "drawn " + n + " time";
                                if (i != 0) text += "s";
                                Topten.RichTextKit.TextBlock block = new();
                                Topten.RichTextKit.Style style = new();
                                style.TextColor = SKColors.Silver;
                                style.FontFamily = "Arial";
                                style.FontSize = 20;
                                block.AddText(text, style);
                                var t = new Topten.RichTextKit.TextPaintOptions();
                                t.Edging = SKFontEdging.SubpixelAntialias;
                                block.Paint(canvas, new SKPoint(x, y), t);
                            }
                        }

                        void drawText(SKCanvas canvas, int n, int x, int y)
                        {
                            SKTypeface t = SKTypeface.FromFamilyName("Arial");
                            SKFont f = t.ToFont();

                            using var paint = new SKPaint(f);
                            paint.Color = SKColors.Silver;
                            paint.TextSize = 20;

                            for (int i = 0; i < n; i++)
                            {
                                string text = "drawn " + n + " time";
                                if (i != 0) text += "s";
                                canvas.Save();
                                canvas.Translate(x, y);
                                canvas.DrawText(text, 0, 0, paint);
                                canvas.Restore();
                            }
                        }

                        void drawMatrix(SKCanvas canvas, int count, int max_lines, int spacing)
                        {
                            max_lines++;
                            int n = 0;
                            int column = 0;
                            int line = 1;
                            for (int i = 0; i < count; i++)
                            {
                                n = i + 1;
                                if (line == max_lines)
                                {
                                    line = 1;
                                    column += spacing;
                                }
                                //int s = canvas.Save();
                                drawText(canvas, n, column, 20 * line);
                                //canvas.RestoreToCount(s);
                                line++;
                            }
                        }

                        drawMatrix(nWayCanvas, 20, 20, 50);

                        nWayCanvas.Flush();

                        using var imageAlpha = offscreenAlphaSurface.Snapshot();
                        using var image = offscreenSurface.Snapshot();
                        var imageAlphaShader = imageAlpha.ToShader();
                        var imageShader = image.ToShader();

                        SKRuntimeEffectChildren children = new(sksl) {
                            { "input", imageShader },
                            { "inputAlpha", imageAlphaShader },
                        };

                        var ourShader = sksl.ToShader(false, new(sksl), children);

                        sksl.Dispose();
                        imageAlphaShader.Dispose();
                        imageShader.Dispose();

                        // we only want to write our paint shader's output pixel to the canvas
                        // this is the same as if the canvas was cleared before painting the shader
                        colorPaint.BlendMode = SKBlendMode.Src;
                        colorPaint.Shader = ourShader;
                        canvas.DrawPaint(colorPaint);
                        ourShader.Dispose();
                    }
                )
            );
        }
    }
}
