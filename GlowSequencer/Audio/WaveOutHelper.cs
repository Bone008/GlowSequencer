using NAudio;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace GlowSequencer.Audio
{
    public static class WaveOutHelper
    {
        // Because NAudio didn't implement getting the volume from the system and I am a damn perfectionist, we make the syscall ourselves.
        [DllImport("winmm.dll")]
        public static extern MmResult waveOutGetVolume(IntPtr hWaveOut, out int dwVolume);

        public static float GetWaveOutVolume(WaveOut waveOut)
        {
            try
            {
                // See Waveout.SetWaveOutVolume for details.
                IntPtr hWaveOut = (IntPtr)typeof(WaveOut).GetField("hWaveOut", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(waveOut);
                object waveOutLock = typeof(WaveOut).GetField("waveOutLock", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(waveOut);

                lock (waveOutLock)
                {
                    waveOutGetVolume(hWaveOut, out int dwVolume);
                    int rawLeft = dwVolume & 0xFFFF;
                    int rawRight = (dwVolume >> 16) & 0xFFFF;

                    return Math.Max(rawLeft, rawRight) / (float)0xFFFF;
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("Failed to read volume: " + e);
                return 1.0f; // Don't let this break the entire application.
            }
        }
    }
}
