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
    /// Interaction logic for TransferDirectlyWindow.xaml
    /// </summary>
    public partial class TransferDirectlyWindow : Window
    {
        private TransferDirectlyViewModel vm;

        public TransferDirectlyWindow(MainViewModel main)
        {
            InitializeComponent();
            DataContext = vm = new TransferDirectlyViewModel(main);
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
    }
}
