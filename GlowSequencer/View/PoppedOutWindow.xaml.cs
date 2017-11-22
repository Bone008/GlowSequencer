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
    /// Interaction logic for PoppedOutWindow.xaml
    /// </summary>
    public partial class PoppedOutWindow : Window
    {
        public PoppedOutWindow()
        {
            InitializeComponent();
        }

        private void ButtonPopIn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
