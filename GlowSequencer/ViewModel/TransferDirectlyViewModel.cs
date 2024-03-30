using ContinuousLinq;
using GlowSequencer.Usb;
using GlowSequencer.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlowSequencer.ViewModel
{
    public class ConnectedDeviceViewModel : Observable
    {
        private ConnectedDevice? model;
        private string _name; // Stored seperately from the model for the persistent & disconnected case.
        private TrackViewModel _assignedTrack = null;

        public string Name { get => _name; set => SetProperty(ref _name, value); }
        public bool IsPersistent => _name.Contains("KEEP"); // TODO: Implement
        public bool IsConnected => model != null;
        public string ProgramName => model?.programName ?? "";

        public TrackViewModel AssignedTrack { get => _assignedTrack; set => SetProperty(ref _assignedTrack, value); }

        /// <summary>Creates a device VM based on a connected device.</summary>
        public ConnectedDeviceViewModel(ConnectedDevice model)
        {
            this.model = model;
            Name = model.name;
        }

        /// <summary>Creates a device VM in the disconnected state, identified only by name.</summary>
        public ConnectedDeviceViewModel(string name)
        {
            this.model = null;
            Name = name;
        }

        public ConnectedDevice? GetModel() => model;

        public void SetModel(ConnectedDevice newModel)
        {
            if (model != null && model.Value.connectedPortId != newModel.connectedPortId)
                throw new ArgumentException("Cannot change the port ID of a connected device VM.");
            if (Name != newModel.name)
                throw new ArgumentException("Cannot change the name of a connected device VM.");

            model = newModel;
            Notify(nameof(IsConnected));
            Notify(nameof(ProgramName));
        }

        public void ClearModel()
        {
            model = null;
            Notify(nameof(IsConnected));
            Notify(nameof(ProgramName));
        }

        public bool MatchesDevice(ConnectedDevice other)
        {
            // In connected state, names are not necessarily unique, so we also need to match by portId.
            if (model != null && (model.Value.connectedPortId != other.connectedPortId))
                return false;
            // In both cases, the name must match.
            return Name == other.name;
        }

        public bool MatchesTrackName(string trackName)
        {
            return trackName.IndexOf(Name, StringComparison.InvariantCultureIgnoreCase) >= 0
                || Name.IndexOf(trackName, StringComparison.InvariantCultureIgnoreCase) >= 0;
        }
    }


    public class TransferDirectlyViewModel : Observable
    {
        private const int MAX_CONCURRENT_TRANSFERS = 4;

        private readonly MainViewModel main;
        private readonly TransferDirectlyController controller;

        private float _transferProgress = 0f;
        private StringBuilder _logOutput = new StringBuilder();
        private bool _isRefreshingDevices = false;
        private ICollection<ConnectedDeviceViewModel> _selectedDevices = new List<ConnectedDeviceViewModel>(0);
        private TimeSpan _exportStartTime = TimeSpan.Zero;

        public ReadOnlyContinuousCollection<TrackViewModel> AllTracks => main.CurrentDocument.Tracks;
        public ObservableCollection<ConnectedDeviceViewModel> AllDevices { get; } = new();
        public ReadOnlyContinuousCollection<ConnectedDeviceViewModel> AllDevicesSorted { get; private set; }
        public ReadOnlyContinuousCollection<ConnectedDeviceViewModel> ConnectedDevices { get; private set; }

        public ICollection<ConnectedDeviceViewModel> SelectedDevices { get => _selectedDevices; set => SetProperty(ref _selectedDevices, value); }

        public bool HasStoredConfiguration => false;

        public TimeSpan ExportStartTime { get => _exportStartTime; set => SetProperty(ref _exportStartTime, value); }

        public float TransferProgress { get => _transferProgress; set => SetProperty(ref _transferProgress, value); }
        public string LogOutput => _logOutput.ToString();

        [Obsolete("only for designer")]
        public TransferDirectlyViewModel() : this(null)
        {
            if (DesignerProperties.GetIsInDesignMode(new System.Windows.DependencyObject()))
            {
                MergeDeviceList(new List<ConnectedDevice>
                {
                    new() { connectedPortId = "LP5", name = "Test 01", programName = "Test01.glo" },
                    new() { connectedPortId = "LP6", name = "Test 02", programName = "Test02.glo" },
                    new() { connectedPortId = "LP7", name = "Test 03", programName = "Test03.glo" },
                });
            }
        }

        public TransferDirectlyViewModel(MainViewModel main)
        {
            this.main = main;
            controller = new TransferDirectlyController();

            AllDevicesSorted = AllDevices.OrderBy(device => device.Name);
            ConnectedDevices = AllDevicesSorted.Where(device => device.IsConnected);

            ForwardPropertyEvents(nameof(main.CurrentDocument), main, nameof(AllTracks));
        }

        public async Task CheckRefreshDevicesAsync()
        {
            if (_isRefreshingDevices)
                return;
            if (!controller.HaveConnectedDevicesChanged())
                return;

            _isRefreshingDevices = true;
            try
            {
                List<ConnectedDevice> currentDevices = await controller.RefreshDevicesAsync();
                MergeDeviceList(currentDevices);
            }
            finally
            {
                _isRefreshingDevices = false;
            }
        }

        private void MergeDeviceList(List<ConnectedDevice> currentDevices)
        {
            List<ConnectedDevice> unmatchedDevices = currentDevices.ToList();

            // We try to preserve the view model identities as much as possible, so see if we can match.
            foreach (var deviceVM in AllDevices.ToList())
            {
                int index = unmatchedDevices.FindIndex(deviceVM.MatchesDevice);
                if (index >= 0)
                {
                    deviceVM.SetModel(unmatchedDevices[index]);
                    unmatchedDevices.RemoveAt(index);
                }
                else if (deviceVM.IsPersistent)
                {
                    deviceVM.ClearModel();
                }
                else
                {
                    AllDevices.Remove(deviceVM);
                }
            }

            // Whatever remains was not connected or known before --> add as non-persistent device!
            foreach (var newDevice in unmatchedDevices)
            {
                AllDevices.Add(new ConnectedDeviceViewModel(newDevice));
            }
        }

        public void StartDevices() => StartOrStopDevices(controller.StartDevices, "Started");

        public void StopDevices() => StartOrStopDevices(controller.StopDevices, "Stopped");

        private void StartOrStopDevices(Action<IEnumerable<string>> controllerAction, string logLabel)
        {
            // TODO: concurrency check
            if (SelectedDevices.Any(device => !device.IsConnected))
            {
                AppendLog("WARNING: Some of the selected devices are NOT connected!");
            }

            var selectedPorts = SelectedDevices
                .Where(vm => vm.IsConnected)
                .Select(vm => vm.GetModel().Value.connectedPortId)
                .ToList();
            try { controllerAction(selectedPorts); }
            catch (UsbOperationException e)
            {
                AppendLog($"ERROR: {e.Message}");
                return;
            }

            if (selectedPorts.Count < SelectedDevices.Count)
                AppendLog($"{logLabel} on {selectedPorts.Count} connected out of {StringUtil.Pluralize(SelectedDevices.Count, "selected device")}!");
            else
                AppendLog($"{logLabel} on {StringUtil.Pluralize(selectedPorts.Count, "device")}!");
        }

        public async Task<bool> SendProgramsAsync()
        {
            // TODO: concurrency check
            if (SelectedDevices.Any(device => !device.IsConnected))
            {
                AppendLog("WARNING: Some of the selected devices are NOT connected!");
            }
            // TODO: assigned track null check
            // TODO: refresh devices check

            var tracksByPortId = SelectedDevices
                .Where(vm => vm.IsConnected)
                .ToDictionary(
                    vm => vm.GetModel().Value.connectedPortId,
                    vm => vm.AssignedTrack.GetModel());

            var options = new TransferDirectlyController.TransferOptions
            {
                documentName = main.DocumentName,
                startTime = (float)ExportStartTime.TotalSeconds,
                progress = new Progress<float>(p => TransferProgress = p * 100),
                log = new Progress<string>(AppendLog),
                maxConcurrentTransfers = MAX_CONCURRENT_TRANSFERS,
                maxRetries = 3,
            };
            bool success = await controller.SendProgramsAsync(tracksByPortId, options);

            MergeDeviceList(await controller.RefreshDevicesAsync());
            return success;
        }

        public void AutoAssignTracks()
        {
            var devices = SelectedDevices.Count > 0 ? SelectedDevices : AllDevices;
            foreach (var device in devices)
            {
                if (device.AssignedTrack != null)
                    continue;
                
                TrackViewModel track = AllTracks.FirstOrDefault(track => device.MatchesTrackName(track.Label));
                if (track == null)
                {
                    AppendLog($"Could not find a matching track for device {device.Name}!");
                    continue;
                }
                device.AssignedTrack = track;
            }
        }

        public void SetStartTimeToCursor()
        {
            ExportStartTime = TimeSpan.FromSeconds(main.CurrentDocument.CursorPosition);
        }

        public void ClearLog()
        {
            _logOutput.Clear();
            Notify(nameof(LogOutput));
        }

        private void AppendLog(string line)
        {
            if (_logOutput.Length > 0)
                _logOutput.AppendLine();
            _logOutput.Append(line);
            Notify(nameof(LogOutput));
        }
    }
}
