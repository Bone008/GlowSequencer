using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using WindowsInput;
using WindowsInput.Native;

namespace GlowPlayer
{
    public class AutomationTool
    {
        // Get a handle to an application window.
        [DllImport("USER32.DLL", CharSet = CharSet.Unicode)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        // Activate an application window.
        [DllImport("USER32.DLL")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        private static IKeyboardSimulator ksim = null;

        public static void SendStuff()
        {
            string progPath = @"D:\0_User\Lukas\BWK\LED\USB Sequencer\Version 0.8\Windows 64\glo_ultimate_app.exe";

            Process p = Process.Start(new ProcessStartInfo(progPath) { WorkingDirectory = @"D:\0_User\Lukas\BWK\LED\USB Sequencer\Version 0.8\Windows 64" });
            p.WaitForInputIdle();

            Thread.Sleep(5000);


            //IntPtr windowHandle = FindWindow("SDL_app", "Glo-Ultimate app");
            IntPtr windowHandle = p.MainWindowHandle;

            // Verify that Calculator is a running process.
            if (windowHandle == IntPtr.Zero)
            {
                MessageBox.Show("Glo-Ultimate did not start properly.");
                return;
            }

            //SetForegroundWindow(windowHandle);
            //SendKeys.SendWait("111");
            //SendKeys.SendWait("*");
            //SendKeys.SendWait("11");
            //SendKeys.SendWait("=");

            var sim = new InputSimulator();
            ksim = sim.Keyboard;

            // navigate to first subdirectory
            k(VirtualKeyCode.F8);
            k(VirtualKeyCode.DOWN);
            k(VirtualKeyCode.RETURN);
            k(VirtualKeyCode.ESCAPE);

            for (int i = 0; i < 6; i++)
            {
                k(VirtualKeyCode.F8);
                k(VirtualKeyCode.DOWN);
                k(VirtualKeyCode.RETURN);
                ksim.Sleep(1500);
                k(VirtualKeyCode.DOWN);
            }

            ksim.Sleep(1000);
            k(VirtualKeyCode.F12);
            k(VirtualKeyCode.F5);

            Process.Start("notepad.exe");

            ksim.Sleep(5000);
            k(VirtualKeyCode.F6);

            p.CloseMainWindow();

            ksim = null;
        }

        private static void k(VirtualKeyCode key)
        {
            ksim.KeyPress(key);
            ksim.Sleep(150);
        }
    }
}
