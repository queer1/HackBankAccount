using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Drawing;
using System.IO;

namespace AnalysisImageClick
{
    class HookTools
    {

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private static LowLevelKeyboardProc _procKeyboard = HookKeyboardCallback;
        private static LowLevelMouseProc _procMouse = HookMouseCallback;
        private static IntPtr _hookKeyboardID = IntPtr.Zero;
        private static IntPtr _hookMouseID = IntPtr.Zero;
        private static bool isKeyboardHooked = false;
        private static bool isMouseHooked = false;
        private const int WM_KEYDOWN = 0x0100;
        private const int WH_KEYBOARD_LL = 13;
        private const int WH_MOUSE_LL = 14;

        private enum MouseMessages
        {
            WM_LBUTTONDOWN = 0x0201,
            WM_LBUTTONUP = 0x0202,
            WM_MOUSEMOVE = 0x0200,
            WM_MOUSEWHEEL = 0x020A,
            WM_RBUTTONDOWN = 0x0204,
            WM_RBUTTONUP = 0x0205
        }

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        public static void StopKeyboardHook()
        {
            if (isKeyboardHooked)
            {
                UnhookWindowsHookEx(_hookKeyboardID);
                isKeyboardHooked = false;
            }
        }

        public static void StartKeyboardHook()
        {
            if (!isKeyboardHooked)
            {
                _hookKeyboardID = SetHookKeyboard(_procKeyboard);
                isKeyboardHooked = true;
            }
        }

        public static void StopMouseHook()
        {
            if (isMouseHooked)
            {
                UnhookWindowsHookEx(_hookMouseID);
                isMouseHooked = false;
            }
        }

        public static void StartMouseHook()
        {
            if (!isMouseHooked)
            {
                _hookMouseID = SetHookMouse(_procMouse);
                isMouseHooked = true;
            }
        }

        private static IntPtr SetHookMouse(LowLevelMouseProc proc)
        {
            using (System.Diagnostics.Process curProcess = System.Diagnostics.Process.GetCurrentProcess())
            using (System.Diagnostics.ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_MOUSE_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private static IntPtr SetHookKeyboard(LowLevelKeyboardProc proc)
        {
            using (System.Diagnostics.Process curProcess = System.Diagnostics.Process.GetCurrentProcess())
            using (System.Diagnostics.ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        public static string getPressChar(char c)
        {
            int k = (int)c;
            string chr = c.ToString().ToUpper();
            if (k == 13) return "ENTER";
            else if (k == 27) return "ESC";
            else if (k == 8) return "BACKSPACE";
            else if (k >= 48 && k <= 57) return chr;
            else if (k >= 65 && k <= 90) return chr;
            else if (k >= 97 && k <= 122) return chr;
            else if (k == 209 || k == 241) return chr;
            return string.Empty;
        }

        private static IntPtr HookKeyboardCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = System.Runtime.InteropServices.Marshal.ReadInt32(lParam);
                string keyName = getPressChar((char)vkCode);
                if (keyName.Length > 0)
                {
                    Console.WriteLine(keyName);
                    System.Diagnostics.Trace.WriteLine(keyName);
                    form.updateKey(keyName);
                }
            }
            return CallNextHookEx(_hookKeyboardID, nCode, wParam, lParam);
        }

        private static IntPtr HookMouseCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                if (MouseMessages.WM_LBUTTONDOWN == (MouseMessages)wParam || MouseMessages.WM_RBUTTONDOWN == (MouseMessages)wParam)
                {
                    MSLLHOOKSTRUCT hookStruct = (MSLLHOOKSTRUCT)System.Runtime.InteropServices.Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
                    Console.WriteLine(hookStruct.pt.x + ", " + hookStruct.pt.y);
                    form.updateMouseDown(hookStruct.pt.x, hookStruct.pt.y);
                }
                else if (MouseMessages.WM_MOUSEMOVE == (MouseMessages)wParam)
                {
                    MSLLHOOKSTRUCT hookStruct = (MSLLHOOKSTRUCT)System.Runtime.InteropServices.Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
                    Console.WriteLine(hookStruct.pt.x + ", " + hookStruct.pt.y);
                    form.updateMouseMove(hookStruct.pt.x, hookStruct.pt.y);
                }
            }
            return CallNextHookEx(_hookMouseID, nCode, wParam, lParam);
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        public static Form1 form = new Form1();

    }

    class ScreenCapture
    {

        public ScreenCapture(){
        }

        public static System.Drawing.Bitmap captureScreen(int id)
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

        public static bool saveBitmap(string file, System.Drawing.Bitmap bitmap)
        {
            try
            {
                bitmap.Save(file, System.Drawing.Imaging.ImageFormat.Png);
                return true;
            }
            catch (Exception e) { }
            return false;
        }

        public static Bitmap loadBitmap(string file)
        {
            Bitmap bmp = null;
            try
            {
                bmp = new Bitmap(file);
            }
            catch (Exception e) { }
            return bmp;
        }
    }

    class OCRTools {
        
        public static Bitmap toGrayscale(Bitmap original)
        {
            Bitmap newBitmap = new Bitmap(original.Width, original.Height);
            for (int i = 0; i < original.Width; i++)
            {
                for (int j = 0; j < original.Height; j++)
                {
                    Color originalColor = original.GetPixel(i, j);
                    int grayScale = (int)((originalColor.R * .3) + (originalColor.G * .59) + (originalColor.B * .11));
                    Color newColor = Color.FromArgb(grayScale, grayScale, grayScale);
                    newBitmap.SetPixel(i, j, newColor);
                }
            }
            return newBitmap;
        }

        public static Bitmap extractImage(Bitmap bmp)
        {
            try {
                for (int w = 0; w < bmp.Width; w++) {
                    for (int h = 0; h < bmp.Height; h++){
                        Color c = bmp.GetPixel(w, h);
                        if (c.R == 226 && c.G == 226 && c.B == 226){
                            Color c2 = Color.Red;
                            bmp.SetPixel(w, h, c2);
                        }
                    }
                }

                //ScreenCapture.saveBitmap("red_zones.png", bmp);
                int topRedY = detectFirstRedLine(bmp);
                topRedY = detectEndFirtRedLine(bmp, topRedY);
                if (topRedY != -1) {
                    bmp = cutTopImage(bmp, 0, topRedY);
                    //ScreenCapture.saveBitmap("top_red_zones.png", bmp);
                    int bottomRedY = detectFirstRedLine(bmp);
                    if (bottomRedY != -1){
                        int startY = (bottomRedY > 0) ? (bottomRedY * -1) : 0;
                        bmp = cutImage(bmp, bmp.Width, bottomRedY, 0, 0);
                        //ScreenCapture.saveBitmap("bottom_red_zones.png", bmp);
                        int leftRedX = detectSecondRedLine(bmp);
                        leftRedX = detectEndSecondRedLine(bmp, leftRedX);
                        bmp = cutTopImage(bmp, leftRedX, 0);
                        //ScreenCapture.saveBitmap("left_red_zones.png", bmp);
                        leftRedX = detectSecondRedLine(bmp);
                        bmp = cutImage(bmp, leftRedX, bmp.Height, 0, 0);
                        //ScreenCapture.saveBitmap("rigth_red_zones.png", bmp);
                    }
                }

                for (int w = 0; w < bmp.Width; w++)
                {
                    for (int h = 0; h < bmp.Height; h++)
                    {
                        Color c = bmp.GetPixel(w, h);
                        if (c.R == 255)
                        {
                            Color c2 = Color.FromArgb(0, 226, 226, 226);
                            bmp.SetPixel(w, h, c2);
                        }
                    }
                }

            }catch(Exception e){}
            return bmp;
        }

        private static Bitmap cutTopImage(Bitmap bmp, int startX, int startY) {
            Bitmap cut = null;
            try {
                if (startX < 0) startX = 0;
                else startX++;
                if (startY < 0) startY = 0;
                else startY++;

                cut = new Bitmap(bmp.Width - startX, bmp.Height - startY);
                Graphics g = Graphics.FromImage(cut);

                int paintX = (startX > 0) ? (startX * -1) : 0;
                int paintY = (startY > 0) ? (startY * -1) : 0;
                
                g.DrawImage(bmp, paintX, paintY);
                g.Dispose();

            }catch(Exception e){}
            return cut;
        }

        private static Bitmap cutImage(Bitmap bmp, int w, int h, int x, int y)
        {
            Bitmap cut = null;
            try
            {
                cut = new Bitmap(w, h);
                Graphics g = Graphics.FromImage(cut);
                g.DrawImage(bmp, x, y);
                g.Dispose();

            }
            catch (Exception e) { }
            return cut;
        }

        private static int detectFirstRedLine(Bitmap bmp){
            int redY = -1;
            try {
                for (int h = 0; h < bmp.Height; h++){
                    for (int w = 0; w < bmp.Width; w++){
                        Color c = bmp.GetPixel(w, h);
                        if (c.R == 255/* && w == 0*/){
                            bool isRedLine = true;
                            for (int wTest = w; wTest < bmp.Width; wTest++){
                                c = bmp.GetPixel(wTest, h);
                                if (c.R != 255)
                                {
                                    isRedLine = false;
                                    break;
                                }
                            }
                            if (isRedLine) {
                                redY = h;
                                return redY;
                            }
                        }
                    }
                }
            }catch(Exception e){}
            return redY;
        }

        private static int detectEndFirtRedLine(Bitmap bmp, int initRedY) {
            int redY = -1;
            try {
                for (int h = initRedY; h < bmp.Height; h++){
                    Color c = bmp.GetPixel(0, h);
                    if (c.R != 255){
                        bool isRedLine = true;
                        for (int w = 0; w < bmp.Width; w++) {
                            c = bmp.GetPixel(w, h-1);
                            if (c.R != 255) {
                                isRedLine = false;
                                break;
                            }
                        }
                        if (isRedLine){
                            redY = h - 1;
                            break;
                        }
                    }
                }
            }catch(Exception e){}
            return redY;
        }

        private static int detectSecondRedLine(Bitmap bmp)
        {
            int redX = -1;
            try
            {
                for (int w = 0; w < bmp.Width; w++) {
                    for (int h = 0; h < bmp.Height; h++){
                        Color c = bmp.GetPixel(w, h);
                        if (c.R == 255 && h == 0){
                            bool isRedLine = true;
                            for (int hTest = h; hTest < bmp.Height; hTest++){
                                c = bmp.GetPixel(w, hTest);
                                if (c.R != 255)
                                {
                                    isRedLine = false;
                                    break;
                                }
                            }
                            if (isRedLine)
                            {
                                redX = w;
                                return redX;
                            }
                        }
                    }
                }
            }
            catch (Exception e) { }
            return redX;
        }

        private static int detectEndSecondRedLine(Bitmap bmp, int initRedX)
        {
            int redX = -1;
            try {
                for (int w = initRedX; w < bmp.Width; w++){
                    Color c = bmp.GetPixel(w, 0);
                    if (c.R != 255){
                        bool isRedLine = true;
                        for (int h = 0; h < bmp.Height; h++){
                            c = bmp.GetPixel(w-1, h);
                            if (c.R != 255)
                            {
                                isRedLine = false;
                                break;
                            }
                        }
                        if (isRedLine)
                        {
                            redX = w - 1;
                            break;
                        }
                    }
                }
            }
            catch (Exception e) { }
            return redX;
        }
    }

    static class Program {
        
        [STAThread]
        static void Main()
        {
            HookTools.StartKeyboardHook();
            HookTools.StartMouseHook();

            Application.Run(HookTools.form);

            HookTools.StopKeyboardHook();
            HookTools.StopMouseHook();
        }
    }
}
