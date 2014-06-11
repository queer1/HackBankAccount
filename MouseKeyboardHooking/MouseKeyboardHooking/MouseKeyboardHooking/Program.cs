using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace MouseKeyboardHooking
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
                    if (startForm) form.updateKey(keyName);
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
                    if(startForm) form.updateMouse(hookStruct.pt.x + ", " + hookStruct.pt.y);
                }
            }
            return CallNextHookEx(_hookMouseID, nCode, wParam, lParam);
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        public static Form1 form = new Form1();
        public static bool startForm = true;
    }

    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            
            HookTools.StartKeyboardHook();
            HookTools.StartMouseHook();

            Application.Run(HookTools.form);

            HookTools.StopKeyboardHook();
            HookTools.StopMouseHook();
        }
    }
}
