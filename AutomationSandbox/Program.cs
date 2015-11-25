using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WindowsInput;
using WindowsInput.Native;

namespace AutomationSandbox
{
    class Program
    {
        private const int DELAY_BETWWEN_KEYS = 150;
        private const int DELAY_FOR_UPLOAD = 1500;
        private const int DELAY_BEFORE_START = 1000;

        private static IKeyboardSimulator ksim = null;
        private static IntPtr windowHandle = IntPtr.Zero;

        static void Main(string[] args)
        {
            string progPath = @"D:\0_User\Lukas\BWK\LED\USB Sequencer\Version 0.8\Windows 64\glo_ultimate_app.exe";

            Console.WriteLine("Starting Glo-Ultimate ...");
            Process p = Process.Start(new ProcessStartInfo(progPath) { WorkingDirectory = @"D:\0_User\Lukas\BWK\LED\USB Sequencer\Version 0.8\Windows 64" });
            p.WaitForInputIdle();

            Thread.Sleep(5000);


            //IntPtr windowHandle = FindWindow("SDL_app", "Glo-Ultimate app");
            windowHandle = p.MainWindowHandle;

            // Verify that Calculator is a running process.
            if (windowHandle == IntPtr.Zero)
            {
                Console.Error.WriteLine("Glo-Ultimate did not start properly.");
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
            Console.WriteLine("Navigating to directory ...");
            k(VirtualKeyCode.F8);
            k(VirtualKeyCode.DOWN);
            k(VirtualKeyCode.RETURN);
            k(VirtualKeyCode.ESCAPE);

            for (int i = 0; i < 6; i++)
            {
                Console.WriteLine("Processing club #" + (i+1) + " ...");
                k(VirtualKeyCode.F8);
                k(VirtualKeyCode.DOWN);
                k(VirtualKeyCode.RETURN);
                ksim.Sleep(DELAY_FOR_UPLOAD);
                k(VirtualKeyCode.DOWN);
            }

            Console.WriteLine("Press any key to start the program ...");
            Console.ReadKey(true);

            Console.WriteLine("Starting program ...");
            k(VirtualKeyCode.F12);
            k(VirtualKeyCode.F5);

            Console.WriteLine("Press any key to stop the program ...");
            Console.ReadKey(true);

            Console.WriteLine("Stopping program ...");
            k(VirtualKeyCode.F6);

            p.CloseMainWindow();

            Console.WriteLine("Done!");
        }

        private static void k(VirtualKeyCode key)
        {
            SetForegroundWindow(windowHandle);
            ksim.KeyPress(key);
            ksim.Sleep(DELAY_BETWWEN_KEYS);
        }



        // Get a handle to an application window.
        [DllImport("USER32.DLL", CharSet = CharSet.Unicode)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        // Activate an application window.
        [DllImport("USER32.DLL")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
    }
}
