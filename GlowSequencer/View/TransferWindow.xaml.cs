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

            MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;
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

        private void SelectNone_Click(object sender, RoutedEventArgs e)
        {
            transferredTracks.SelectedItems.Clear();
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            transferredTracks.SelectAll();
        }

        private void ResetAdvanced_Click(object sender, RoutedEventArgs e)
        {
            vm.ResetAdvancedSettings();
        }

        private async void RefreshList_Click(object sender, RoutedEventArgs e)
        {
            await vm.RefreshWindowListAsync();
        }
        private void StartAutomagically_CheckedChanged(object sender, RoutedEventArgs e)
        {
            bool isChecked = ((ToggleButton)sender).IsChecked ?? false;
            if (!isChecked)
                noStartMusicCb.IsChecked = true;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (vm.IsTransferInProgress)
                vm.CancelTransfer();

            // also save settings when closing the window
            vm.SaveSettings();
        }

        private async void Window_Activated(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("activated");
            await vm.RefreshWindowListAsync();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.NewSize.Height != e.PreviousSize.Height && Top + e.NewSize.Height > SystemParameters.PrimaryScreenHeight)
            {
                Top = SystemParameters.MaximizedPrimaryScreenHeight - e.NewSize.Height;
            }
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var diag = new Microsoft.Win32.OpenFileDialog();
            diag.DefaultExt = ".exe";
            diag.Filter = "Executable files (*.exe)|*.exe|All files|*.*";
            diag.FilterIndex = 0;

            // attempt to navigate to directory already present in the text field
            string dir = null;
            try { dir = System.IO.Path.GetDirectoryName(vm.AerotechAppExePath); }
            catch (ArgumentException) { }
            if (dir != null)
                diag.InitialDirectory = dir;

            if (diag.ShowDialog(this) == true)
            {
                vm.AerotechAppExePath = diag.FileName;
            }
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
