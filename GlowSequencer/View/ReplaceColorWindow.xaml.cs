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
    /// Interaction logic for ReplaceColorWindow.xaml
    /// </summary>
    public partial class ReplaceColorWindow : Window
    {

        private ReplaceColorViewModel vm;

        public ReplaceColorWindow(ReplaceColorViewModel vm)
        {
            InitializeComponent();
            DataContext = this.vm = vm;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            vm.Execute();
            this.Close();
        }

    }
}
