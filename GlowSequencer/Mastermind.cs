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

        public static void OpenMusicSegmentsWindow()
        {
            if (winMusicSegments == null)
            {
                winMusicSegments = new MusicSegmentsWindow();
                winMusicSegments.Owner = Application.Current.MainWindow;
                winMusicSegments.Show();

                winMusicSegments.Closed += (sender, e) => winMusicSegments = null;
            }
            else
            {
                winMusicSegments.Activate();
            }
        }

        public static void OpenAboutWindow()
        {
            if (winAbout == null)
            {
                winAbout = new AboutWindow();
                winAbout.Owner = Application.Current.MainWindow;
                winAbout.Show();

                winAbout.Closed += (sender, e) => winAbout = null;
            }
            else
            {
                winAbout.Activate();
            }
        }
    }
}
