using GlowSequencer.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace GlowSequencer.View
{
    /// <summary>
    /// Interaction logic for TransferWindow.xaml
    /// </summary>
    public partial class TransferWindow : Window
    {
        private TransferViewModel vm;

        public TransferWindow(MainViewModel main)
        {
            InitializeComponent();
            DataContext = vm = new TransferViewModel(main);
        }

        private void transferredTracks_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var items = transferredTracks.SelectedItems;
            vm.SelectedTracks = items.Cast<TrackViewModel>().ToList();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            vm.CancelTransfer();
        }

        private async void Start_Click(object sender, RoutedEventArgs e)
        {
            await vm.StartTransferAsync();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ((TextBox)sender).ScrollToEnd();
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            transferredTracks.SelectAll();
        }

        private void ResetAdvanced_Click(object sender, RoutedEventArgs e)
        {
            vm.ResetAdvancedSettings();
        }

        private void RefreshList_Click(object sender, RoutedEventArgs e)
        {
            vm.RefreshWindowList();
        }
        private void StartAutomagically_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!(bool)((ToggleButton)sender).IsChecked)
                startExternalCb.IsChecked = false;

            startExternalCb.IsEnabled = (bool)((ToggleButton)sender).IsChecked;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (vm.IsTransferInProgress)
                vm.CancelTransfer();
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("activated");
            vm.RefreshWindowList();
        }

        private void CursorButton_Click(object sender, RoutedEventArgs e)
        {
            vm.SetStartTimeToCursor();
        }

        private void ZeroButton_Click(object sender, RoutedEventArgs e)
        {
            vm.ExportStartTime = TimeSpan.Zero;
        }

    }
}
