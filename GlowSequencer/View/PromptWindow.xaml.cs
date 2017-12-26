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
    /// Interaction logic for PromptWindow.xaml
    /// </summary>
    public partial class PromptWindow : Window
    {
        public string PromptText
        {
            get { return promptTextBox.Text; }
            set { promptTextBox.Text = value; promptTextBox.SelectAll(); }
        }
        public bool AllowEmpty { get; set; }

        public PromptWindow()
            : this("Prompt")
        { }

        public PromptWindow(string title)
        {
            InitializeComponent();
            this.Title = title;

            promptTextBox.Focus();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.MinHeight = this.ActualHeight;
            this.MaxHeight = this.ActualHeight;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (!AllowEmpty && string.IsNullOrEmpty(PromptText))
            {
                MessageBox.Show(this, "Please enter a value.");
                return;
            }

            this.DialogResult = true;
            this.Close();
        }

    }
}
