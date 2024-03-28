using ContinuousLinq;
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
        public string Name { get; set; }
        public string ProgramName { get; set; }

        public bool IsConnected { get; set; }

        public TrackViewModel AssignedTrack { get; set; }
    }

    public class TransferDirectlyViewModel : Observable
    {
        private MainViewModel main;

        public ReadOnlyContinuousCollection<TrackViewModel> AllTracks => main.CurrentDocument.Tracks;
        public ObservableCollection<ConnectedDeviceViewModel> ConnectedDevices { get; } = new ObservableCollection<ConnectedDeviceViewModel>();

        public TimeSpan ExportStartTime { get; set; }

        public float TransferProgress => 42.0f;
        public string LogOutput => "Test log output";

        public TransferDirectlyViewModel()
        {
            ConnectedDevices.Add(new ConnectedDeviceViewModel { Name = "Test 01", ProgramName = "Test01.glo", IsConnected = true });
            ConnectedDevices.Add(new ConnectedDeviceViewModel { Name = "Test 02", ProgramName = "Test02.glo", IsConnected = true });
            ConnectedDevices.Add(new ConnectedDeviceViewModel { Name = "Test 03", ProgramName = "Test03.glo", IsConnected = false });
        }

        public TransferDirectlyViewModel(MainViewModel main) : this()
        {
            this.main = main;
        }
    }
}
