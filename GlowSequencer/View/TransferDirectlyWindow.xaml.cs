using GlowSequencer.ViewModel;
using System;
using System.Collections.Generic;
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
        public static readonly TimeSpan DEVICES_REFRESH_INTERVAL = TimeSpan.FromSeconds(1);

        private readonly TransferDirectlyViewModel vm;

        private bool _syntheticSelectionChange = false;

        public TransferDirectlyWindow(MainViewModel main)
        {
            DataContext = vm = new TransferDirectlyViewModel(main);
            InitializeComponent();

            // Periodically refresh devices list.
            DispatcherTimer timer = new();
            timer.Interval = DEVICES_REFRESH_INTERVAL;
            timer.Tick += async (sender, e) => await vm.CheckRefreshDevicesAsync();
            timer.Start();

            Closed += (sender, e) => timer.Stop();
        }

        private void CursorButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Reimplement
            //vm.SetStartTimeToCursor();
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

            bool allSelected = devicesList.Items.Count == items.Count;
            _syntheticSelectionChange = true;
            selectAll.IsChecked = allSelected;
            _syntheticSelectionChange = false;
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
    }
}
