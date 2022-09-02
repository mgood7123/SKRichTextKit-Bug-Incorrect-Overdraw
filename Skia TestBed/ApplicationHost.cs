using SkiaSharp;
using System;
using System.Windows.Forms;

namespace Skia_TestBed
{
    public partial class SkiaHost : Form
    {
        SkiaGL skia;

        public SkiaHost(Action<SKImageInfo, GRContext, SKSurface, SKCanvas> paint)
        {
            InitializeComponent();

            skia = new(paint);

            Load += ApplicationHost_Load;
            ClientSizeChanged += ApplicationHost_Resize;
        }

        private void ApplicationHost_Resize(object? sender, EventArgs e)
        {
            Control? control = sender as Control;
            if (control != null)
            {
                skia.Size = ClientSize;
            }
        }

        private void ApplicationHost_Load(object? sender, EventArgs e)
        {
            ApplicationHost_Resize(sender!, e);
            Controls.Add(skia);
        }
    }
}
