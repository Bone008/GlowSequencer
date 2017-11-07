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
            Application.Current.DispatcherUnhandledException += Application_DispatcherUnhandledException;

            if (e.Args.Length > 0)
            {
                string fileToLoad = e.Args[0];
                
                var main = (ViewModel.MainViewModel) Resources["vm_Main"];
                main.OpenDocument(fileToLoad);
            }
        }

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            // prompt user if they want to recover
            MessageBoxResult result = MessageBox.Show(
                "An unhandled exception was encountered in the program. The program may be able to recover but may also be in a corrupt state. Do you want to try recovery?"
                    + Environment.NewLine + Environment.NewLine + e.Exception.ToString(),
                "Unhandled Exception", MessageBoxButton.YesNo, MessageBoxImage.Error);

            if (result == MessageBoxResult.Yes)
                e.Handled = true;
        }


        //private void TrackCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        //{
        //    CheckBox cb = (CheckBox)sender;
        //    return;
        //    var data = cb.DataContext as Tuple<ViewModel.TrackViewModel, bool, ViewModel.BlockViewModel>;

        //    if (cb.IsChecked.Value)
        //        data.Item3.AddToTrack(data.Item1);
        //    else if (data.Item3.GetModel().Tracks.Count > 1)
        //        data.Item3.RemoveFromTrack(data.Item1);
        //    else
        //        cb.IsChecked = true;
        //}

        private void AnyContextMenu_Opening(object sender, ContextMenuEventArgs e)
        {
            //UIElement placementTarget = ((ContextMenu)sender).PlacementTarget;
            //FocusManager.SetFocusedElement(placementTarget, placementTarget);
        }
    }
}
