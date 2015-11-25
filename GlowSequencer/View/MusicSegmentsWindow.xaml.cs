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

namespace GlowSequencer.View
{
    /// <summary>
    /// Interaction logic for MusicSegmentsWindow.xaml
    /// </summary>
    public partial class MusicSegmentsWindow : Window
    {
        private SequencerViewModel sequencer { get { return (SequencerViewModel)DataContext; } }

        public MusicSegmentsWindow()
        {
            InitializeComponent();
        }


        private void ListView_Loaded(object sender, RoutedEventArgs e)
        {
            AdjustColumns((ListView)sender);
        }

        private void ListView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            AdjustColumns((ListView)sender);
        }

        private void AdjustColumns(ListView lv)
        {
            GridView gv = (GridView)lv.View;

            double w = gv.Columns.Skip(1).Sum(col => col.Width);
            gv.Columns[0].Width = Math.Max(10, lv.ActualWidth - w - SystemParameters.VerticalScrollBarWidth - 6);
        }

    }
}
