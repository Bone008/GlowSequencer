using GlowSequencer.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WindowsInput;
using WindowsInput.Native;
using GlowSequencer.Model;
using GlowSequencer.ViewModel;
using System.Runtime.InteropServices;
using System.Windows;

namespace GlowSequencer
{
    public class TransferToEquipmentController
    {
        private enum TaskStage
        {
            ExportFiles, OpenProgram, UploadSequences, Completed
        }

        private const string GLO_SUBDIR_NAME = "___tmp";
        private const int MAX_ATTEMPTS_PER_TRACK = 5;
        /// <summary>Approximate coordinates of the first letter in the status line of the Aerotech window.</summary>
        private static readonly Int32Rect WINDOW_STATUS_RECT = new Int32Rect(41, 531, 7, 10);

        private readonly TransferToEquipmentSettings settings;
        private readonly IList<Track> tracks;
        private readonly PlaybackViewModel playback;
        private readonly string documentName;

        // runtime state
        private Process aerotechProc = null;
        private InputSimulator inputSim = null;
        private bool success = false;

        public TransferToEquipmentController(TransferToEquipmentSettings settings, IList<Track> tracks, PlaybackViewModel playback, string documentName)
        {
            this.settings = settings;
            this.tracks = tracks;
            this.playback = playback;
            this.documentName = documentName;
        }

        public async Task RunTransferAsync(IProgress<float> progress, IProgress<string> log, CancellationToken cancel)
        {
            try
            {
                if (tracks.Count > 0)
                {
                    log.Report("Generating glo files ...");
                    string tmpDir = Path.Combine(Path.GetDirectoryName(settings.AerotechAppExePath), GLO_SUBDIR_NAME);
                    Directory.CreateDirectory(tmpDir);
                    string versionId = GenerateRandomString(3);

                    // clean up potential left-over files from previous runs
                    Directory.EnumerateFiles(tmpDir, ".glo").ForEach(File.Delete);

                    string sanitizedDocumentName = FileSerializer.SanitizeString(Path.GetFileNameWithoutExtension(documentName));
                    for (int i = 0; i < tracks.Count; i++)
                    {
                        await Task.Run(() =>
                        {
                            string file = Path.Combine(tmpDir, string.Format("{0:000}_{1}_{2}_{3}.glo",
                                i + 1,
                                FileSerializer.SanitizeString(tracks[i].Label),
                                versionId,
                                sanitizedDocumentName));

                            FileSerializer.ExportTrackToFile(tracks[i], file, (float)settings.ExportStartTime.TotalSeconds);
                        }, cancel);
                        ReportProgress(progress, TaskStage.ExportFiles, i + 1, tracks.Count);
                    }
                }

                // close open instances of the Aerotech program
                Process[] runningPrograms = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(settings.AerotechAppExePath));
                if (runningPrograms.Any())
                {
                    log.Report("Trying to close running instances of Glo Ultimate App ...");
                    foreach (var p in runningPrograms)
                        p.CloseMainWindow();
                }

                log.Report("Starting Glo Ultimate App ...");
                aerotechProc = await Task.Run((Func<Process>)LaunchAerotechProgram, cancel);
                await Task.Delay(settings.DelayBeforeStart, cancel);

                ReportProgress(progress, TaskStage.OpenProgram);

                inputSim = new InputSimulator();

                if (tracks.Count > 0)
                {
                    log.Report("Navigating to directory ...");
                    Press(VirtualKeyCode.F8);
                    await Task.Delay(settings.DelayBetweenKeys);
                    Press(VirtualKeyCode.DOWN);
                    await Task.Delay(settings.DelayBetweenKeys);
                    Press(VirtualKeyCode.RETURN);
                    await Task.Delay(settings.DelayBetweenKeys);
                    Press(VirtualKeyCode.ESCAPE);
                    await Task.Delay(settings.DelayBetweenKeys);

                    ReportProgress(progress, TaskStage.UploadSequences, 1, tracks.Count + 1);

                    for (int i = 0; i < tracks.Count; i++)
                    {
                        log.Report("Uploading track \"" + tracks[i].Label + "\" ...");

                        Press(VirtualKeyCode.F8);
                        await Task.Delay(settings.DelayBetweenKeys, cancel);
                        Press(VirtualKeyCode.DOWN);
                        await Task.Delay(settings.DelayBetweenKeys, cancel);
                        Press(VirtualKeyCode.RETURN);
                        await Task.Delay(settings.DelayBetweenKeys + settings.DelayForUpload, cancel);

                        // Repeat transfer until status is successful.
                        int attempts = 0;
                        while(!ReadStatusSafely(log))
                        {
                            if(++attempts >= MAX_ATTEMPTS_PER_TRACK)
                            {
                                log.Report($"  Detected failure! Skipping after {MAX_ATTEMPTS_PER_TRACK} attempts.");
                                break;
                            }
                            log.Report("  Detected failure! Retrying ...");
                            Press(VirtualKeyCode.F8);
                            await Task.Delay(settings.DelayBetweenKeys, cancel);
                            Press(VirtualKeyCode.RETURN);
                            await Task.Delay(settings.DelayBetweenKeys + settings.DelayForUpload, cancel);
                        }

                        Press(VirtualKeyCode.DOWN);
                        await Task.Delay(settings.DelayBetweenKeys, cancel);

                        ReportProgress(progress, TaskStage.UploadSequences, i + 2, tracks.Count + 1);
                    }
                }

                if (settings.StartAutomagicallyAfterTransfer)
                {
                    log.Report("Starting the sequence ...");
                    Press(VirtualKeyCode.F12);
                    await Task.Delay(settings.DelayBetweenKeys, cancel);
                    Press(VirtualKeyCode.F5);
                }

                if (settings.ExportStartTime < TimeSpan.Zero &&
                    (settings.StartInternalMusicAfterTransfer || settings.StartExternalMusicAfterTransfer))
                {
                    log.Report("Waiting " + -settings.ExportStartTime + " before starting music ...");
                    await Task.Delay(-settings.ExportStartTime, cancel);
                }

                if (settings.StartInternalMusicAfterTransfer)
                {
                    log.Report("Starting internal music ...");
                    float time = (float)MathUtil.Max(settings.ExportStartTime, TimeSpan.Zero).TotalSeconds;
                    bool result = playback.PlayAt(time);
                    if (!result)
                        log.Report("Error: Could not start internal music!");
                }

                if (settings.StartExternalMusicAfterTransfer)
                {
                    Process musicProc = settings.GetMusicProcess();
                    if (musicProc == null)
                        log.Report("Error: External music program not running, unable to start music!");
                    else
                    {
                        log.Report("Starting music ...");
                        SetForegroundWindow(musicProc.MainWindowHandle);
                        inputSim.Keyboard.KeyPress(VirtualKeyCode.SPACE);
                    }
                }

                await Task.Delay(settings.DelayBetweenKeys, cancel);

                ReportProgress(progress, TaskStage.Completed);
                success = true;
                Cleanup();
                log.Report("Done!" + Environment.NewLine);
            }
            catch (OperationCanceledException)
            {
                Cleanup();
                progress.Report(0);
                log.Report("Transfer was canceled." + Environment.NewLine);
            }
            catch (InvalidOperationException e)
            {
                Cleanup();
                progress.Report(0);
                log.Report("Error: Aerotech program was closed unexpectedly: " + e.Message);
            }
        }

        private static string GenerateRandomString(int length)
        {
            const string ALPHABET = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuwxyz";
            var r = new Random();
            return new string(Enumerable.Range(0, length).Select(_ => ALPHABET[r.Next(ALPHABET.Length)]).ToArray());
        }

        private Process LaunchAerotechProgram()
        {
            Process proc = Process.Start(new ProcessStartInfo(settings.AerotechAppExePath)
            {
                WorkingDirectory = Path.GetDirectoryName(Path.GetFullPath(settings.AerotechAppExePath))
            });
            proc.WaitForInputIdle();

            return proc;
        }

        private void Press(VirtualKeyCode key)
        {
            if (aerotechProc.HasExited)
                throw new InvalidOperationException("Process no longer available.");

            SetForegroundWindow(aerotechProc.MainWindowHandle);
            inputSim.Keyboard.KeyPress(key);
        }

        private void Cleanup()
        {
            if (aerotechProc != null)
            {
                if (settings.CloseProgramAfterTransfer || !success)
                    try { aerotechProc.CloseMainWindow(); }
                    catch (InvalidOperationException) { /* ignore: process/window already gone */ }

                aerotechProc = null;
            }

            string tmpDir = Path.Combine(Path.GetDirectoryName(settings.AerotechAppExePath), GLO_SUBDIR_NAME);
            if (Directory.Exists(tmpDir))
                Directory.Delete(tmpDir, true);
        }


        private void ReportProgress(IProgress<float> progress, TaskStage stage, int step = 1, int maxStep = 1)
        {
            float baseCompletion;
            float stageDuration;
            switch (stage)
            {
                case TaskStage.ExportFiles: baseCompletion = 0.00f; stageDuration = 0.15f; break;
                case TaskStage.OpenProgram: baseCompletion = 0.15f; stageDuration = 0.25f; break;
                case TaskStage.UploadSequences: baseCompletion = 0.4f; stageDuration = 0.6f; break;
                case TaskStage.Completed: baseCompletion = 1.0f; stageDuration = 0.0f; break;
                default: throw new NotImplementedException("unknown stage: " + stage);
            }

            progress.Report(baseCompletion + stageDuration * step / maxStep);
        }


        private bool ReadStatusSafely(IProgress<string> log)
        {
            try { return ReadStatus(); }
            catch(Exception e) {
                Console.Error.WriteLine(e);
                log.Report("Failed to read pixels from window! Assuming success.");
                return true;
            }
        }

        private bool ReadStatus()
        {
            if (aerotechProc.HasExited)
                throw new InvalidOperationException("Process no longer available.");
            IntPtr hdc = GetDC(aerotechProc.MainWindowHandle);

            // Read all pixels in the hardcoded status rectangle.
            var ys = Enumerable.Range(WINDOW_STATUS_RECT.Y, WINDOW_STATUS_RECT.Height);
            var xs = Enumerable.Range(WINDOW_STATUS_RECT.X, WINDOW_STATUS_RECT.Width);
            var statusSamples = ys.SelectMany(y => xs.Select(x => GetPixelColor(hdc, x, y))).ToList();
            ReleaseDC(aerotechProc.MainWindowHandle, hdc);

            // Return false if there are any red-looking pixels in the rectangle.
            return !statusSamples.Any(c => c.R > c.G + 10 && c.R > c.B + 10);
        }

        private System.Windows.Media.Color GetPixelColor(IntPtr hdc, int x, int y)
        {
            uint c = GetPixel(hdc, x, y);
            return System.Windows.Media.Color.FromRgb(
                (byte)(c & 0x000000FF),
                (byte)((c & 0x0000FF00) >> 8),
                (byte)((c & 0x00FF0000) >> 16));
        }




        // Activate an application window.
        [DllImport("user32.DLL")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        // Acquire device context.
        [DllImport("user32.dll")]
        static extern IntPtr GetDC(IntPtr hwnd);

        // Release device context.
        [DllImport("user32.dll")]
        static extern Int32 ReleaseDC(IntPtr hwnd, IntPtr hdc);

        // Read a pixel from a device context.
        [DllImport("gdi32.dll")]
        static extern uint GetPixel(IntPtr hdc, int nXPos, int nYPos);
    }
}
