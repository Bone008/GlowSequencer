using ContinuousLinq;
using GlowSequencer.Model;
using GlowSequencer.Usb;
using GlowSequencer.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace GlowSequencer.ViewModel
{
    public class ConnectedDeviceViewModel : Observable
    {
        private ConnectedDevice? model;
        private string _name; // Stored seperately from the model for the persistent & disconnected case.
        private bool _isPersistent = false;
        private TrackViewModel _assignedTrack = null;
        private Color _identifyColor = TransferSettings.DEFAULT_IDENTIFY_COLOR.ToViewColor();
        private Color? _lastSentIdentifyColor = null;

        public string Name { get => _name; set => SetProperty(ref _name, value); }
        public bool IsPersistent { get => _isPersistent; set => SetProperty(ref _isPersistent, value); }
        public TrackViewModel AssignedTrack { get => _assignedTrack; set => SetProperty(ref _assignedTrack, value); }
        public Color IdentifyColor { get => _identifyColor; set => SetProperty(ref _identifyColor, value); }
        public Color? LastSentIdentifyColor { get => _lastSentIdentifyColor; set => SetProperty(ref _lastSentIdentifyColor, value); }

        public bool IsConnected => model != null;
        public string ProgramName => model != null ? model.Value.programName : "⚠️ DISCONNECTED";

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
            if (Name != newModel.name)
                throw new ArgumentException("Cannot change the name of a connected device VM.");
            if (IsConnected && (model.Value.connectedPortId != newModel.connectedPortId))
            {
                // Changing port ID is allowed for persistent entries, but resets its highlight state.
                if (IsPersistent)
                    LastSentIdentifyColor = null;
                else
                    throw new ArgumentException("Cannot change the port ID of a connected device VM.");
            }

            model = newModel;
            Notify(nameof(IsConnected));
            Notify(nameof(ProgramName));
        }

        public void ClearModel()
        {
            model = null;
            Notify(nameof(IsConnected));
            Notify(nameof(ProgramName));
            LastSentIdentifyColor = null;
        }

        public bool MatchesDevice(ConnectedDevice other)
        {
            // In connected state, names are not necessarily unique, so we also need to match by portId.
            // Persisted devices are an exception, which should aggressively match by name.
            if (!IsPersistent && IsConnected && (model.Value.connectedPortId != other.connectedPortId))
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
        private readonly MainViewModel main;
        private readonly TransferDirectlyController controller;

        private float _transferProgress = 0f;
        private StringBuilder _logOutput = new StringBuilder();
        private string _logOutputStr = "";
        private bool _isRefreshingDevices = false;
        private bool _isUsbBusy = false;
        private ICollection<ConnectedDeviceViewModel> _selectedDevices = new List<ConnectedDeviceViewModel>(0);
        private TimeSpan _exportStartTime = TimeSpan.Zero;
        private ColorTransformMode _colorMode = ColorTransformMode.None;
        private bool _enableMusic = false;
        private int _musicSystemDelayMs = TransferSettings.DEFAULT_MUSIC_SYSTEM_DELAY_MS;
        private bool _enableIdentify = false;

        // advanced settings
        private int _maxConcurrentTransfers = TransferSettings.DEFAULT_MAX_CONCURRENT_TRANSFERS;
        private int _maxTransferRetries = TransferSettings.DEFAULT_MAX_RETRIES;


        public ReadOnlyContinuousCollection<TrackViewModel> AllTracks => main.CurrentDocument.Tracks;
        public ObservableCollection<ConnectedDeviceViewModel> AllDevices { get; } = new();
        public ReadOnlyContinuousCollection<ConnectedDeviceViewModel> AllDevicesSorted { get; private set; }
        public ReadOnlyContinuousCollection<ConnectedDeviceViewModel> ConnectedDevices { get; private set; }

        public ICollection<ConnectedDeviceViewModel> SelectedDevices { get => _selectedDevices; set => SetProperty(ref _selectedDevices, value); }

        public TimeSpan ExportStartTime { get => _exportStartTime; set => SetProperty(ref _exportStartTime, value); }
        public ColorTransformMode ColorMode { get => _colorMode; set => SetProperty(ref _colorMode, value); }
        public bool EnableMusic { get => _enableMusic; set => SetProperty(ref _enableMusic, value); }
        public int MusicSystemDelayMs
        {
            get => _musicSystemDelayMs;
            set => SetProperty(ref _musicSystemDelayMs, Math.Max(0, value));
        }

        public bool HasSavedSettings => main.CurrentDocument.GetModel().TransferSettings != null;

        public bool EnableIdentify { get => _enableIdentify; set => SetProperty(ref _enableIdentify, value); }

        // only for forwarding change events
        private ReadOnlyContinuousCollection<Color> AllIdentifyColorDummies { get; set; }

        public int MaxConcurrentTransfers
        {
            get => _maxConcurrentTransfers;
            set => SetProperty(ref _maxConcurrentTransfers, Math.Max(1, value));
        }
        public int MaxTransferRetries
        {
            get => _maxTransferRetries;
            set => SetProperty(ref _maxTransferRetries, Math.Max(0, value));
        }

        /// <summary>Mutex for all USB operations to avoid concurrent access to devices.</summary>
        public bool IsUsbBusy { get => _isUsbBusy; private set => SetProperty(ref _isUsbBusy, value); }
        public float TransferProgress { get => _transferProgress; set => SetProperty(ref _transferProgress, value); }
        public string LogOutput => _logOutputStr;

        public TransferDirectlyViewModel(MainViewModel main)
        {
            this.main = main;
            controller = new TransferDirectlyController();

            AllDevicesSorted = AllDevices.OrderBy(device => device.Name);
            ConnectedDevices = AllDevicesSorted.Where(device => device.IsConnected);
            AllIdentifyColorDummies = AllDevices.Select(device => device.IdentifyColor);

            ForwardPropertyEvents(nameof(EnableIdentify), this, UpdateIdentifiedDevices);
            ForwardPropertyEvents(nameof(SelectedDevices), this, UpdateIdentifiedDevices);
            ForwardCollectionEvents(AllDevices, UpdateIdentifiedDevices);
            ForwardCollectionEvents(AllIdentifyColorDummies, UpdateIdentifiedDevices);

            ForwardPropertyEvents(nameof(main.CurrentDocument), main,
                nameof(AllTracks),
                nameof(HasSavedSettings));
            ForwardPropertyEvents(nameof(main.CurrentDocument), main, LoadSettings);
            LoadSettings();
        }

        public async Task CheckRefreshDevicesAsync()
        {
            // Since this is an invisible background operation, it gets its own mutex in addition to
            // the IsUsbBusy check. This hides the refresh operation from the user.
            if (_isRefreshingDevices || IsUsbBusy)
            {
                AppendLog("[Refresh] Skipping refresh since USB is busy.");
                return;
            }
            var sw = new Stopwatch();
            sw.Start();
            if (!await controller.HaveConnectedDevicesChangedAsync())
            {
                AppendLog($"[Refresh] No refresh, still {ConnectedDevices.Count} connected [took {sw.Elapsed.TotalSeconds:0.00} s].");
                return;
            }
            await DoRefreshDevicesAsync();
            AppendLog($"[Refresh] List refreshed, now {ConnectedDevices.Count} connected [took {sw.Elapsed.TotalSeconds:0.00} s].");

            // update identify colors after lock is released. it's already called by the change events,
            // but at that point IsUsbBusy is still true.
            await UpdateIdentifiedDevicesAsync();
        }

        private async Task DoRefreshDevicesAsync()
        {
            _isRefreshingDevices = true;
            try
            {
                List<ConnectedDevice> currentDevices = await controller.RefreshDevicesAsync();
                MergeDeviceList(currentDevices);
            }
            catch (UsbOperationException e)
            {
                AppendLog($"ERROR refreshing device list: {e.Message}");
                Debug.WriteLine(e);
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

        private void UpdateIdentifiedDevices()
        {
            // The sync version needs to exist for the property notifications.
            UpdateIdentifiedDevicesAsync().Forget();
        }

        private async Task UpdateIdentifiedDevicesAsync()
        {
            // Might result in some outdated state, but identify isn't important enough to
            // keep running while USB contention seems heavy. This can also be called in quick
            // succession, so this also avoids re-entrancy problems.
            if (IsUsbBusy)
                return;

            // Calculate a delta of which devices need to be updated.
            Dictionary<string, Color?> changedColorsByPort = new();
            Dictionary<string, ConnectedDeviceViewModel> changedDevicesByPort = new();

            var toIdentify = EnableIdentify
                ? (SelectedDevices.Count > 0 ? SelectedDevices : AllDevices)
                : Enumerable.Empty<ConnectedDeviceViewModel>();
            foreach (var device in AllDevices)
            {
                if (!device.IsConnected)
                    continue;
                string portId = device.GetModel().Value.connectedPortId;
                bool shouldHighlight = toIdentify.Contains(device);
                if (shouldHighlight && device.LastSentIdentifyColor != device.IdentifyColor)
                {
                    changedColorsByPort[portId] = device.IdentifyColor;
                    changedDevicesByPort[portId] = device;
                }
                else if (!shouldHighlight && device.LastSentIdentifyColor != null)
                {
                    changedColorsByPort[portId] = null;
                    changedDevicesByPort[portId] = device;
                }
            }

            if (changedColorsByPort.Count == 0)
                return;

            using var _ = await AcquireUsbLock();
            Dictionary<string, bool> results = await controller.SetDeviceColorsAsync(
                    changedColorsByPort, MaxConcurrentTransfers);
            // Only update LastSentIdentifyColor for successful operations.
            foreach (var kvp in results)
            {
                if (!kvp.Value)
                {
                    AppendLog($"ERROR: Failed to set color on device {kvp.Key}!");
                    continue;
                }
                changedDevicesByPort[kvp.Key].LastSentIdentifyColor = changedColorsByPort[kvp.Key];
            }
        }

        public async Task StartDevicesAsync()
        {
            using var _ = await AcquireUsbLock();

            if (EnableMusic)
            {
                if (ExportStartTime < TimeSpan.Zero)
                {
                    AppendLog("WARNING: Negative start time with music playback is not supported.");
                }
                float playTime = (float)MathUtil.Max(ExportStartTime, TimeSpan.Zero).TotalSeconds;
                main.CurrentDocument.Playback.PlayAt(playTime, updateCursor: false);
                if (MusicSystemDelayMs > 0)
                    await Task.Delay(MusicSystemDelayMs);
            }

            await StartOrStopDevicesAsync(controller.StartDevicesAsync, "Started");
        }

        public async Task StopDevicesAsync()
        {
            using var _ = await AcquireUsbLock();
            await StartOrStopDevicesAsync(controller.StopDevicesAsync, "Stopped");
            main.CurrentDocument.Playback.Stop();
        }

        private async Task StartOrStopDevicesAsync(Func<IEnumerable<string>, Task> controllerAction, string logLabel)
        {
            if (SelectedDevices.Any(device => !device.IsConnected))
            {
                AppendLog("WARNING: Some of the selected devices are NOT connected!");
            }

            // Starting/stopping resets the highlighted color, we should reflect that.
            // Do this before the actual action to make sure we reset even if there are partial errors.
            foreach (var device in SelectedDevices)
                device.LastSentIdentifyColor = null;

            var selectedPorts = SelectedDevices
                .Where(vm => vm.IsConnected)
                .Select(vm => vm.GetModel().Value.connectedPortId)
                .ToList();

            var sw = new Stopwatch();
            sw.Start();
            try
            {
                await controllerAction(selectedPorts);
            }
            catch (UsbOperationException e)
            {
                AppendLog($"ERROR: {e.Message}");
                return;
            }
            finally
            {
                sw.Stop();
            }

            string durationStr = $"USB duration: {sw.Elapsed.TotalSeconds:0.0} s";
            if (selectedPorts.Count < SelectedDevices.Count)
                AppendLog($"{logLabel} on {selectedPorts.Count} connected out of {StringUtil.Pluralize(SelectedDevices.Count, "selected device")}! ({durationStr})");
            else
                AppendLog($"{logLabel} on {StringUtil.Pluralize(selectedPorts.Count, "device")}! ({durationStr})");
        }

        public async Task<bool> SendProgramsAsync()
        {
            using var _ = await AcquireUsbLock();

            if (SelectedDevices.Any(device => device.IsConnected && device.AssignedTrack == null))
            {
                AppendLog("Cannot transfer without assigned tracks for all devices!");
                return false;
            }

            bool showHasDisconnectedWarning = SelectedDevices.Any(device => !device.IsConnected);
            var tracksByPortId = SelectedDevices
                .Where(vm => vm.IsConnected)
                .ToDictionary(
                    vm => vm.GetModel().Value.connectedPortId,
                    vm => vm.AssignedTrack.GetModel());

            var options = new TransferDirectlyController.TransferOptions
            {
                documentName = main.DocumentName,
                startTime = (float)ExportStartTime.TotalSeconds,
                colorMode = ColorMode,
                progress = new Progress<float>(p => TransferProgress = p * 100),
                log = new Progress<string>(AppendLog),
                maxConcurrentTransfers = MaxConcurrentTransfers,
                maxRetries = MaxTransferRetries,
            };
            bool success = await controller.SendProgramsAsync(tracksByPortId, options);
            await DoRefreshDevicesAsync(); // Refresh to update program names.

            if (showHasDisconnectedWarning)
            {
                AppendLog("WARNING: Some of the selected devices were NOT connected!");
            }
            return success;
        }

        public async Task RenameDeviceAsync(ConnectedDeviceViewModel device, string newName)
        {
            using var _ = await AcquireUsbLock();
            if (!device.IsConnected)
                return;
            try
            {
                await controller.RenameDeviceAsync(device.GetModel().Value.connectedPortId, newName);
                device.Name = newName;
            }
            catch (UsbOperationException e)
            {
                AppendLog($"ERROR renaming device: {e.Message}");
            }
        }

        public void AutoAssignTracks()
        {
            foreach (var device in AllDevices)
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

        public void SaveSettings()
        {
            var settings = new TransferSettings
            {
                ExportStartTime = ExportStartTime,
                ColorMode = ColorMode,
                EnableMusic = EnableMusic,
                MusicSystemDelayMs = MusicSystemDelayMs,
                // TODO: Probably don't save technical settings in the timeline but in user settings?
                MaxConcurrentTransfers = MaxConcurrentTransfers,
                MaxRetries = MaxTransferRetries,
                DeviceConfigs = Enumerable.Select(AllDevices, dev => new TransferSettings.Device
                {
                    name = dev.Name,
                    assignedTrack = dev.AssignedTrack?.GetModel(),
                    identifyColor = dev.IdentifyColor.ToGloColor(),
                }).ToList(),
            };

            main.CurrentDocument.GetModel().TransferSettings = settings;
            main.IsDirty = true;
            Notify(nameof(HasSavedSettings));

            // Rather than reloading settings (which would temporarily mark everything as disconnected),
            // we just mark everything as persistent.
            foreach (var device in AllDevices)
                device.IsPersistent = true;

            AppendLog($"Saved current configuration with {StringUtil.Pluralize(AllDevices.Count, "device")}!");
        }

        public void ClearSettings()
        {
            main.CurrentDocument.GetModel().TransferSettings = null;
            main.IsDirty = true;
            Notify(nameof(HasSavedSettings));

            foreach (var device in AllDevices.ToList())
            {
                if (device.IsConnected)
                {
                    device.IsPersistent = false;
                    device.AssignedTrack = null;
                }
                else
                {
                    AllDevices.Remove(device);
                }
            }

            AppendLog("Cleared configuration!");
        }

        private void LoadSettings()
        {
            var settings = main.CurrentDocument.GetModel().TransferSettings;
            bool emptySettings = settings == null;
            if (emptySettings)
            {
                // Keep loading anyway, to discard old settings in case of document switch.
                settings = new TransferSettings();
            }

            ExportStartTime = settings.ExportStartTime;
            ColorMode = settings.ColorMode;
            EnableMusic = settings.EnableMusic;
            MusicSystemDelayMs = settings.MusicSystemDelayMs;
            MaxConcurrentTransfers = settings.MaxConcurrentTransfers;
            MaxTransferRetries = settings.MaxRetries;

            // Preserve connected devices.
            List<ConnectedDevice> connectedDevices = Enumerable.Where(AllDevices,
                device => device.IsConnected)
                .Select(device => device.GetModel().Value).ToList();

            AllDevices.Clear();
            foreach (var config in settings.DeviceConfigs)
            {
                AllDevices.Add(new ConnectedDeviceViewModel(config.name)
                {
                    IsPersistent = true,
                    AssignedTrack = AllTracks.FirstOrDefault(t => t.GetModel() == config.assignedTrack),
                    IdentifyColor = config.identifyColor.ToViewColor(),
                });
            }
            MergeDeviceList(connectedDevices);

            if (!emptySettings)
                AppendLog("Loaded saved configuration!");
        }

        public void SetStartTimeToCursor()
        {
            ExportStartTime = TimeSpan.FromSeconds(main.CurrentDocument.CursorPosition);
        }

        public void ResetAdvancedSettings()
        {
            MaxConcurrentTransfers = TransferSettings.DEFAULT_MAX_CONCURRENT_TRANSFERS;
            MaxTransferRetries = TransferSettings.DEFAULT_MAX_RETRIES;
        }

        public void ClearLog()
        {
            _logOutput.Clear();
            _logOutputStr = "";
            Notify(nameof(LogOutput));
        }

        private void AppendLog(string line)
        {
            if (_logOutput.Length > 0)
                _logOutput.AppendLine();

            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            _logOutput.Append("[").Append(timestamp).Append("] ");
            _logOutput.Append(line);
            _logOutputStr = _logOutput.ToString();
            Notify(nameof(LogOutput));
        }

        private async Task<IDisposable> AcquireUsbLock()
        {
            // Acquire main mutex.
            while (IsUsbBusy)
                await Task.Delay(100);
            IsUsbBusy = true;

            // Wait for potentially running refresh to finish.
            while (_isRefreshingDevices)
                await Task.Delay(100);
            return new ActionDisposable(() => IsUsbBusy = false);
        }
    }
}
