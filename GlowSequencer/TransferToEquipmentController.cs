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

namespace GlowSequencer
{
    public class TransferToEquipmentController
    {
        private enum TaskStage
        {
            ExportFiles, OpenProgram, UploadSequences, Completed
        }

        private const string GLO_SUBDIR_NAME = "___tmp";

        private TransferToEquipmentSettings settings;
        private IList<Model.Track> tracks;

        // runtime state
        private Process aerotechProc = null;
        private InputSimulator inputSim = null;
        private bool success = false;

        public TransferToEquipmentController(TransferToEquipmentSettings persistedSettings, IList<Model.Track> tracks)
        {
            this.settings = persistedSettings;
            this.tracks = tracks;
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

                    for (int i = 0; i < tracks.Count; i++)
                    {
                        await Task.Run(() =>
                        {
                            string file = Path.Combine(tmpDir, string.Format("{0:0000}_" + FileSerializer.SanitizeString(tracks[i].Label) + ".glo", i));
                            FileSerializer.ExportTrack(tracks[i], file, (float)settings.ExportStartTime.TotalSeconds);
                        }, cancel);
                        ReportProgress(progress, TaskStage.ExportFiles, i + 1, tracks.Count);
                    }
                }

                // close open instances of the Aerotech program
                Process[] runningPrograms = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(settings.AerotechAppExePath));
                if(runningPrograms.Any())
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
                        Press(VirtualKeyCode.DOWN);
                        await Task.Delay(settings.DelayBetweenKeys, cancel);

                        ReportProgress(progress, TaskStage.UploadSequences, i + 2, tracks.Count + 1);
                    }
                }

                if(settings.StartAutomagicallyAfterTransfer)
                {
                    log.Report("Starting the sequence ...");
                    Press(VirtualKeyCode.F12);
                    await Task.Delay(settings.DelayBetweenKeys, cancel);
                    Press(VirtualKeyCode.F5);
                }

                if (settings.StartMusicAfterTransfer)
                {
                    Process musicProc = settings.GetMusicProcess();
                    if (musicProc == null)
                        log.Report("External music program not running, unable to start music!");
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
            catch(InvalidOperationException e)
            {
                Cleanup();
                progress.Report(0);
                log.Report("Error: Aerotech program was closed unexpectedly: " + e.Message);
            }
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
            switch(stage)
            {
                case TaskStage.ExportFiles: baseCompletion = 0.00f; stageDuration = 0.15f; break;
                case TaskStage.OpenProgram: baseCompletion = 0.15f; stageDuration = 0.25f; break;
                case TaskStage.UploadSequences: baseCompletion = 0.4f; stageDuration = 0.6f; break;
                case TaskStage.Completed: baseCompletion = 1.0f; stageDuration = 0.0f; break;
                default: throw new NotImplementedException("unknown stage: " + stage);
            }

            progress.Report(baseCompletion + stageDuration * step / maxStep);
        }




        // Activate an application window.
        [System.Runtime.InteropServices.DllImport("USER32.DLL")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
    }
}
