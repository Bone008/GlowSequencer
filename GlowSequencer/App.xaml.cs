using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace GlowSequencer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public const string FILENAME_TRANSFER_SETTINGS = "transfer_settings.xml";

        public static string GetUserDataDir(bool create = true)
        {
            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GlowSequencer");
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            return dir;
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            EventManager.RegisterClassHandler(typeof(Window), Window.PreviewMouseUpEvent, new MouseButtonEventHandler(OnPreviewMouseUp));
            Application.Current.DispatcherUnhandledException += Application_DispatcherUnhandledException;

            string fileToLoad = e.Args.SkipWhile(arg => arg.StartsWith("--")).FirstOrDefault();
            if (fileToLoad != null)
            {
                var main = (ViewModel.MainViewModel)Resources["vm_Main"];
                main.OpenDocument(fileToLoad);
            }
        }

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            // prompt user if they want to recover
            string exceptionDetails = e.Exception.ToString();
            if (exceptionDetails.Length > 600)
                exceptionDetails = exceptionDetails.Substring(0, 597) + "...";

            MessageBoxResult result = MessageBox.Show(
                "An unhandled exception was encountered in the program. The program may be able to recover but may also be in a corrupt state."
                    + Environment.NewLine + Environment.NewLine + exceptionDetails
                    + Environment.NewLine + Environment.NewLine + "Do you want to try recovery?",
                "Unhandled Exception", MessageBoxButton.YesNo, MessageBoxImage.Error);

            if (result == MessageBoxResult.Yes)
                e.Handled = true;
        }

        private void AnyContextMenu_Opening(object sender, ContextMenuEventArgs e)
        {
            //UIElement placementTarget = ((ContextMenu)sender).PlacementTarget;
            //FocusManager.SetFocusedElement(placementTarget, placementTarget);
        }

        private void OnPreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                draggingTargetState = null;
        }

        // Tracks dragging over checkboxes. Null means that no dragging is currently active.
        private bool? draggingTargetState = null;

        // Allow click & drag to select multiple checkboxes. Adapted from https://stackoverflow.com/a/28402504.
        // TODO: This should be moved into some more specific place like a UserControl.
        private void GenericPropertiesTrackCheckBox_MouseEnter(object sender, MouseEventArgs e)
        {
            var checkbox = (CheckBox)sender;
            if (e.LeftButton == MouseButtonState.Pressed && draggingTargetState != null && GetThreeStateAfterClick(checkbox.IsChecked) == draggingTargetState)
            {
                checkbox.IsChecked = draggingTargetState;
            }
            else if (e.LeftButton == MouseButtonState.Released)
            {
                // This is a hacky improvement because the global OnPreviewMouseUp does not catch mouse releases
                // if they happen outside of a window. So we reset the dragging state here as well.
                draggingTargetState = null;
            }
        }

        private void GenericPropertiesTrackCheckBox_GotMouseCapture(object sender, MouseEventArgs e)
        {
            var checkbox = (CheckBox)sender;
            bool targetState = GetThreeStateAfterClick(checkbox.IsChecked);
            checkbox.IsChecked = targetState;

            // Only allow dragging if data binding did not interfere with state change.
            if (checkbox.IsChecked == targetState)
            {
                draggingTargetState = checkbox.IsChecked;
                checkbox.ReleaseMouseCapture(); // allow MouseEnter events for other checkboxes
            }
        }

        private static bool GetThreeStateAfterClick(bool? state)
        {
            // indeterminate state ==> disabled
            if (state == null) return false;
            // invert otherwise
            return !state.Value;
        }
    }
}
