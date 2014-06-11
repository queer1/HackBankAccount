using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Imaging;

namespace ScreenCapture
{
    public class ScreenCapture {

        public ScreenCapture() {
            saveBitmap("capture.png", captureScreen(0));
        }

        private static System.Drawing.Bitmap captureScreen(int id)
        {
            try
            {
                System.Drawing.Rectangle region = System.Windows.Forms.Screen.AllScreens[id].Bounds;
                System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(region.Width, region.Height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
                System.Drawing.Graphics graphic = System.Drawing.Graphics.FromImage(bitmap);
                graphic.CopyFromScreen(region.Left, region.Top, 0, 0, region.Size);
                return bitmap;
            }
            catch (Exception e) { }
            return null;
        }

        private static bool saveBitmap(string file, System.Drawing.Bitmap bitmap)
        {
            try
            {
                bitmap.Save(file, System.Drawing.Imaging.ImageFormat.Png);
                return true;
            }
            catch (Exception e) { }
            return false;
        }
    }
}
