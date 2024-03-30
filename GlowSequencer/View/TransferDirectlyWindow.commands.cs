using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace GlowSequencer.View
{
    public static class TransferDirectlyCommands
    {
        public static readonly RoutedUICommand ToggleSelectAll = new RoutedUICommand(
            "", "ToggleSelectAll", typeof(TransferDirectlyCommands), new InputGestureCollection { new KeyGesture(Key.F12) });

        public static readonly RoutedUICommand Start = new RoutedUICommand(
            "", "Start", typeof(TransferDirectlyCommands), new InputGestureCollection { new KeyGesture(Key.F5) });
        public static readonly RoutedUICommand Stop = new RoutedUICommand(
            "", "Stop", typeof(TransferDirectlyCommands), new InputGestureCollection { new KeyGesture(Key.F6) });
        public static readonly RoutedUICommand Identify = new RoutedUICommand(
            "", "Identify", typeof(TransferDirectlyCommands), new InputGestureCollection { new KeyGesture(Key.F7) });
        public static readonly RoutedUICommand Transfer = new RoutedUICommand(
            "", "Transfer", typeof(TransferDirectlyCommands), new InputGestureCollection { new KeyGesture(Key.F10) });
        public static readonly RoutedUICommand TransferAndStart = new RoutedUICommand(
            "", "TransferAndStart", typeof(TransferDirectlyCommands), new InputGestureCollection { new KeyGesture(Key.F11) });
    }

    public partial class TransferDirectlyWindow
    {
        private void CommandBinding_CanExecuteIfAnySelected(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = vm.SelectedDevices.Count > 0;
        }

        private void CommandBinding_ExecuteToggleSelectAll(object sender, ExecutedRoutedEventArgs e)
        {
            selectAll.IsChecked = !selectAll.IsChecked;
            selectAll.Focus();
        }

        private void CommandBinding_ExecuteStart(object sender, ExecutedRoutedEventArgs e)
        {
            vm.StartDevices();
        }

        private void CommandBinding_ExecuteStop(object sender, ExecutedRoutedEventArgs e)
        {
            vm.StopDevices();
        }

        private void CommandBinding_CanExecuteTransfer(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = vm.SelectedDevices.Count > 0
                && vm.SelectedDevices.All(dev => dev.AssignedTrack != null);
        }

        private async void CommandBinding_ExecuteTransfer(object sender, ExecutedRoutedEventArgs e)
        {
            await vm.SendProgramsAsync();
        }
    }
}
