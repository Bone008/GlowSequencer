using ContinuousLinq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        // transient data
        private float _progress = 0;
        private StringBuilder _logOutput = new StringBuilder();
        private TransferToEquipmentController activeTransfer = null;
        private CancellationTokenSource transferCancel = new CancellationTokenSource();

        public string AerotechAppExePath { get { return persistedSettings.AerotechAppExePath; } set { persistedSettings.AerotechAppExePath = value; Notify(); } }
        public bool StartAutomagicallyAfterTransfer { get { return persistedSettings.StartAutomagicallyAfterTransfer; } set { persistedSettings.StartAutomagicallyAfterTransfer = value; Notify(); } }
        public bool CloseProgramAfterTransfer { get { return persistedSettings.CloseProgramAfterTransfer; } set { persistedSettings.CloseProgramAfterTransfer = value; Notify(); } }

        public ReadOnlyContinuousCollection<TrackViewModel> AllTracks { get { return main.CurrentDocument.Tracks; } }
        public ICollection<TrackViewModel> SelectedTracks { get { return _selectedTracks; } set { SetProperty(ref _selectedTracks, value); } }

        public bool CanStartTransfer { get { return File.Exists(persistedSettings.AerotechAppExePath) && _selectedTracks.Count > 0; } }
        public bool IsTransferIdle { get { return activeTransfer == null; } }
        public bool IsTransferInProgress { get { return activeTransfer != null; } }
        public float TransferProgress { get { return _progress; } private set { SetProperty(ref _progress, value); } }
        public string LogOutput { get { return _logOutput.ToString(); } }

        public TransferViewModel(MainViewModel main)
        {
            this.main = main;
            ForwardPropertyEvents("CurrentDocument", main, "AllTracks");
            ForwardPropertyEvents("AerotechAppExePath", this, "CanStartTransfer");
            ForwardPropertyEvents("SelectedTracks", this, "CanStartTransfer");

            LoadSettings();
        }

        private void AppendLog(string line)
        {
            if (_logOutput.Length > 0)
                _logOutput.AppendLine();
            _logOutput.Append(line);
            Notify("LogOutput");
        }

        public async Task StartTransferAsync()
        {
            if (!CanStartTransfer)
                throw new InvalidOperationException("cannot start transfer at this point");

            SaveSettings();

            activeTransfer = new TransferToEquipmentController(persistedSettings, _selectedTracks.Select(t => t.GetModel()).ToList());
            transferCancel = new CancellationTokenSource();
            Notify("IsTransferIdle");
            Notify("IsTransferInProgress");

            await activeTransfer.RunTransferAsync(
                    new Progress<float>(p => TransferProgress = p * 100),
                    new Progress<string>(AppendLog),
                    transferCancel.Token
                );

            transferCancel = null;
            activeTransfer = null;
            Notify("IsTransferInProgress");
            Notify("IsTransferIdle");
        }

        public void CancelTransfer()
        {
            if (transferCancel != null)
                transferCancel.Cancel();
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

        private void SaveSettings()
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
