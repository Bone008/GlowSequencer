using GlowSequencer.ViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace GlowSequencer.View
{
    /// <summary>
    /// Interaction logic for TransferDirectlyWindow.xaml
    /// </summary>
    public partial class TransferDirectlyWindow : Window
    {
        public static readonly TimeSpan DEVICES_REFRESH_INTERVAL = TimeSpan.FromSeconds(5);

        private readonly TransferDirectlyViewModel vm;

        private DispatcherTimer _refreshTimer;
        private bool _syntheticSelectionChange = false;

        public TransferDirectlyWindow(MainViewModel main)
        {
            DataContext = vm = new TransferDirectlyViewModel(main);
            InitializeComponent();

            MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;

            // Periodically refresh devices list.
            _refreshTimer = new();
            _refreshTimer.Interval = DEVICES_REFRESH_INTERVAL;
            _refreshTimer.Tick += async (sender, e) => await vm.CheckRefreshDevicesAsync();
            _refreshTimer.Start();

            Closed += (sender, e) =>
            {
                _refreshTimer.Stop();
                vm.WriteLogsToFile();
            };

            // Help out the command manager with re-enabling the buttons.
            vm.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(vm.IsUsbBusy))
                    CommandManager.InvalidateRequerySuggested();
            };

            vm.AllDevicesSorted.CollectionChanged += (sender, e) =>
            {
                if (selectAll.IsChecked ?? false)
                    devicesList.SelectAll();
            };
        }

        private void CursorButton_Click(object sender, RoutedEventArgs e)
        {
            vm.SetStartTimeToCursor();
        }

        private void ZeroButton_Click(object sender, RoutedEventArgs e)
        {
            vm.ExportStartTime = TimeSpan.Zero;
        }

        private void Log_TextChanged(object sender, TextChangedEventArgs e)
        {
            ((TextBox)sender).ScrollToEnd();
        }

        private void devicesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var items = devicesList.SelectedItems;
            vm.SelectedDevices = items.Cast<ConnectedDeviceViewModel>().ToList();

            // Disable "select all" when the user unselects something.
            if (items.Count < devicesList.Items.Count)
            {
                _syntheticSelectionChange = true;
                selectAll.IsChecked = false;
                _syntheticSelectionChange = false;
            }
        }

        private async void devicesListItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var device = ((ListViewItem)sender).Content as ConnectedDeviceViewModel;
            if (device == null)
                return;
            if (!device.IsConnected)
                return;

            var result = Mastermind.ShowPromptString(
                this,
                "Rename device",
                device.Name,
                str => !string.IsNullOrWhiteSpace(str));

            if (result.Success)
            {
                await vm.RenameDeviceAsync(device, result.Value.Trim());
            }
        }

        private void selectAll_Checked(object sender, RoutedEventArgs e)
        {
            if (!_syntheticSelectionChange)
                devicesList.SelectAll();
        }
        private void selectAll_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!_syntheticSelectionChange)
                devicesList.UnselectAll();
        }

        private void ClearLog_Click(object sender, RoutedEventArgs e)
        {
            vm.ClearLog();
        }
        private void ShowLogDirectory_Click(object sender, RoutedEventArgs e)
        {
            vm.WriteLogsToFile();
            // start explorer in logs directory
            Process.Start("explorer.exe", vm.GetLogsDirectory());
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            vm.SaveSettings();
        }

        private void ClearSettings_Click(object sender, RoutedEventArgs e)
        {
            vm.ClearSettings();
        }

        private void ResetAdvanced_Click(object sender, RoutedEventArgs e)
        {
            vm.ResetAdvancedSettings();
        }
    }
}
