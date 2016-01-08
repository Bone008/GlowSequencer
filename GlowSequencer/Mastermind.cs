using GlowSequencer.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace GlowSequencer
{
    public class Mastermind
    {
        private static MusicSegmentsWindow winMusicSegments = null;
        private static AboutWindow winAbout = null;
        private static TransferWindow winTransfer = null;

        private static void OpenWindow<T>(ref T win, Action closeHandler) where T : Window, new()
        {
            OpenWindow(ref win, () => new T(), closeHandler);
        }

        private static void OpenWindow<T>(ref T win, Func<T> windowConstructor, Action closeHandler) where T : Window
        {
            if (win == null)
            {
                win = windowConstructor();
                win.Owner = Application.Current.MainWindow;
                win.Show();

                win.Closed += (sender, e) => closeHandler();
            }
            else
            {
                win.Activate();
            }
        }


        public static void OpenMusicSegmentsWindow()
        {
            OpenWindow(ref winMusicSegments, () => winMusicSegments = null);
        }

        public static void OpenAboutWindow()
        {
            OpenWindow(ref winAbout, () => winAbout = null);
        }

        public static void OpenTransferWindow(ViewModel.MainViewModel main)
        {
            OpenWindow(ref winTransfer, () => new TransferWindow(main), () => winTransfer = null);
        }
    }
}
