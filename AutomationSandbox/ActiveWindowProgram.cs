
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WindowsInput;

namespace AutomationSandbox
{
    class ActiveWindowProgram
    {

        static void Main(string[] args)
        {
            Console.WriteLine("Open windows:");
            foreach (var p in Process.GetProcesses().Where(p => p.MainWindowHandle != IntPtr.Zero && p.Id != Process.GetCurrentProcess().Id))
                Console.WriteLine("- {0} - {1}  ({2})", p.ProcessName, p.MainWindowTitle, p.MainWindowHandle);

            var vlcProc = Process.GetProcesses().First(p => p.ProcessName == "vlc");
            Program.SetForegroundWindow(vlcProc.MainWindowHandle);

            new InputSimulator().Keyboard.KeyPress(WindowsInput.Native.VirtualKeyCode.SPACE);

            Console.ReadKey(true);
        }

    }
}
