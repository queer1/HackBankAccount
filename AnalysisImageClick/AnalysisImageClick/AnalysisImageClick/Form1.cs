using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

namespace AnalysisImageClick
{
    public partial class Form1 : Form
    {
        public Form1(){
            InitializeComponent();
            /*Bitmap bmp = ScreenCapture.loadBitmap("red_zones.png");
            bmp = OCRTools.extractImage(bmp);
            pictureBox.Image = bmp;*/
        }

        public void updateMouseDown(int x, int y){
            Bitmap bmp = ScreenCapture.captureScreen(0);
            if (bmp != null) {//40x35
                Bitmap zone = new Bitmap(40 * 2, 35 * 2);
                Graphics g = Graphics.FromImage(zone);
                g.DrawImage(bmp, (x * -1) + 40, (y * -1) + 35);
                g.Dispose();
                bmp = null;
                zone = OCRTools.toGrayscale(zone);
                zone = OCRTools.extractImage(zone);
                pictureBox.Image = zone;
            }
        }

        public void updateKey(string key) {
            /*Console.WriteLine(key);
             Debug.WriteLine(key);
             Trace.WriteLine(key);*/
        }

        public void updateMouseMove(int x, int y) {
            /*Console.WriteLine(x + ", " + y);
            Debug.WriteLine(x + ", " + y);
            Trace.WriteLine(x + ", " + y);*/
        }
    }
}
