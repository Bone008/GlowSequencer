using ContinuousLinq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace GlowSequencer.ViewModel
{
    public class TransferViewModel : Observable
    {
        private MainViewModel main;

        private TransferToEquipmentSettings persistedSettings = new TransferToEquipmentSettings();
        private ICollection<TrackViewModel> _selectedTracks = new List<TrackViewModel>(0);

        private ObservableCollection<Process> _processesWithWindowsContainer = new ObservableCollection<Process>();

        private float _progress = 0;
        private StringBuilder _logOutput = new StringBuilder();
        private TransferToEquipmentController activeTransfer = null;
        private CancellationTokenSource transferCancel = new CancellationTokenSource();

        public string AerotechAppExePath { get { return persistedSettings.AerotechAppExePath; } set { persistedSettings.AerotechAppExePath = value; Notify(); } }
        public TimeSpan ExportStartTime { get { return persistedSettings.ExportStartTime; } set { persistedSettings.ExportStartTime = value; Notify(); } }
        public bool StartAutomagicallyAfterTransfer { get { return persistedSettings.StartAutomagicallyAfterTransfer; } set { persistedSettings.StartAutomagicallyAfterTransfer = value; Notify(); } }
        public bool CloseProgramAfterTransfer { get { return persistedSettings.CloseProgramAfterTransfer; } set { persistedSettings.CloseProgramAfterTransfer = value; Notify(); } }
        public int DelayBeforeStart { get { return persistedSettings.DelayBeforeStart; } set { persistedSettings.DelayBeforeStart = value; Notify(); } }
        public int DelayForUpload { get { return persistedSettings.DelayForUpload; } set { persistedSettings.DelayForUpload = value; Notify(); } }
        public int DelayBetweenKeys { get { return persistedSettings.DelayBetweenKeys; } set { persistedSettings.DelayBetweenKeys = value; Notify(); } }


        public bool StartInternalMusicAfterTransfer { get { return persistedSettings.StartInternalMusicAfterTransfer; } set { persistedSettings.StartInternalMusicAfterTransfer = value; Notify(); } }
        public bool StartExternalMusicAfterTransfer { get { return persistedSettings.StartExternalMusicAfterTransfer; } set { persistedSettings.StartExternalMusicAfterTransfer = value; Notify(); } }
        public int MusicWindowProcessId
        {
            get
            {
                return persistedSettings.GetMusicProcessId();
            }
            set
            {
                var p = Process.GetProcessById(value);
                persistedSettings.MusicWindowProcessName = p.ProcessName;
                persistedSettings.MusicWindowTitle = p.MainWindowTitle;
                Notify();
            }
        }
        public ObservableCollection<Process> ProcessesWithWindows { get { return _processesWithWindowsContainer; } }


        public ReadOnlyContinuousCollection<TrackViewModel> AllTracks { get { return main.CurrentDocument.Tracks; } }
        public ICollection<TrackViewModel> SelectedTracks { get { return _selectedTracks; } set { SetProperty(ref _selectedTracks, value); } }

        public bool CanStartTransfer { get { return File.Exists(persistedSettings.AerotechAppExePath) && (!StartExternalMusicAfterTransfer || MusicWindowProcessId != 0); } }
        public string CanStartTransferReason
        {
            get
            {
                if (CanStartTransfer) return null;
                if (!File.Exists(persistedSettings.AerotechAppExePath))
                    return "The path to Aerotech's Glo-Ultimate App is not valid!";
                else
                    return "Please select a window for the external music!";
            }
        }

        public bool IsTransferIdle { get { return activeTransfer == null; } }
        public bool IsTransferInProgress { get { return activeTransfer != null; } }
        public float TransferProgress { get { return _progress; } private set { SetProperty(ref _progress, value); } }
        public string LogOutput { get { return _logOutput.ToString(); } }


        public TransferViewModel(MainViewModel main)
        {
            this.main = main;
            ForwardPropertyEvents(nameof(main.CurrentDocument), main, nameof(AllTracks));
            ForwardPropertyEvents(nameof(AerotechAppExePath), this, nameof(CanStartTransfer), nameof(CanStartTransferReason));
            ForwardPropertyEvents(nameof(StartExternalMusicAfterTransfer), this, nameof(CanStartTransfer), nameof(CanStartTransferReason));
            ForwardPropertyEvents(nameof(MusicWindowProcessId), this, nameof(CanStartTransfer), nameof(CanStartTransferReason));

            LoadSettings();
            var _ = RefreshWindowListAsync();
        }

        private void AppendLog(string line)
        {
            if (_logOutput.Length > 0)
                _logOutput.AppendLine();
            _logOutput.Append(line);
            Notify(nameof(LogOutput));
        }

        public async Task StartTransferAsync()
        {
            if (!CanStartTransfer)
                throw new InvalidOperationException("cannot start transfer at this point");

            SaveSettings();

            var tracksList = _selectedTracks
                    .Select(t => t.GetModel())
                    .OrderBy(t => t.GetIndex())
                    .ToList();
            activeTransfer = new TransferToEquipmentController(persistedSettings, tracksList, main.CurrentDocument.Playback);
            transferCancel = new CancellationTokenSource();
            Notify(nameof(IsTransferIdle));
            Notify(nameof(IsTransferInProgress));

            await activeTransfer.RunTransferAsync(
                    new Progress<float>(p => TransferProgress = p * 100),
                    new Progress<string>(AppendLog),
                    transferCancel.Token
                );

            transferCancel = null;
            activeTransfer = null;
            Notify(nameof(IsTransferInProgress));
            Notify(nameof(IsTransferIdle));
        }

        public void CancelTransfer()
        {
            if (transferCancel != null)
                transferCancel.Cancel();
        }

        public void ResetAdvancedSettings()
        {
            var def = new TransferToEquipmentSettings();
            DelayBeforeStart = def.DelayBeforeStart;
            DelayBetweenKeys = def.DelayBetweenKeys;
            DelayForUpload = def.DelayForUpload;
        }

        public void SetStartTimeToCursor()
        {
            ExportStartTime = TimeSpan.FromSeconds(main.CurrentDocument.CursorPosition);
        }

        public async Task RefreshWindowListAsync()
        {
            int selectedId = MusicWindowProcessId;

            List<Process> procs =
                await Task.Run(() => Process.GetProcesses()
                                              .Where(p => p.MainWindowHandle != IntPtr.Zero && p.MainWindowTitle.Length > 0 && p.Id != Process.GetCurrentProcess().Id)
                                              .OrderBy(p => p.ProcessName).ToList());

            _processesWithWindowsContainer.Clear();
            foreach (var p in procs) _processesWithWindowsContainer.Add(p);

            // set the selection again
            if (procs.Any(p => p.Id == selectedId))
                MusicWindowProcessId = selectedId;
        }



        private void LoadSettings()
        {
            string settingsFile = Path.Combine(App.GetUserDataDir(false), App.FILENAME_TRANSFER_SETTINGS);
            if (File.Exists(settingsFile))
            {
                try
                {
                    XDocument doc = XDocument.Load(settingsFile);
                    persistedSettings.PopulateFromXML(doc.Root);
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine("Error while loading transfer settings: " + e);
                }
            }
        }

        public void SaveSettings()
        {
            string settingsFile = Path.Combine(App.GetUserDataDir(), App.FILENAME_TRANSFER_SETTINGS);

            XDocument doc = new XDocument(new XElement("transfer-settings"));
            persistedSettings.ToXML(doc.Root);

            try { doc.Save(settingsFile); }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Error while saving transfer settings: " + e);
            }
        }

    }
}
