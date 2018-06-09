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
    /// Interaction logic for EditNoteWindow.xaml
    /// </summary>
    public partial class EditNoteWindow : Window
    {
        private readonly NoteViewModel vm;

        public EditNoteWindow(NoteViewModel vm)
        {
            InitializeComponent();
            DataContext = this.vm = vm;

            labelTextBox.Focus();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void labelTextBox_TargetUpdated(object sender, DataTransferEventArgs e)
        {
            labelTextBox.SelectAll();
        }
    }
}
