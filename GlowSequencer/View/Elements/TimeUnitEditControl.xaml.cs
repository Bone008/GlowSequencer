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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GlowSequencer.View
{
    /// <summary>
    /// Interaction logic for TimeUnitEditControl.xaml
    /// </summary>
    public partial class TimeUnitEditControl : UserControl
    {

        public static readonly DependencyProperty ActiveUnitProperty =
            DependencyProperty.Register("ActiveUnit", typeof(int), typeof(TimeUnitEditControl), new PropertyMetadata(0));

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(TimeUnit), typeof(TimeUnitEditControl), new PropertyMetadata(null, OnValueChanged));

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = d as TimeUnitEditControl;
            if (self == null)
                return;

            if (self.Value != null && !self.Value.HasMusicData && self.unitComboBox.SelectedIndex != 0)
            {
                System.Diagnostics.Debug.WriteLine(self.GetHashCode() + ": setting to seconds");
                self.unitComboBox.SelectedIndex = 0;
            }
        }




        public TimeUnit Value
        {
            get { return (TimeUnit)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public int ActiveUnit
        {
            get { return (int)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }



        public TimeUnitEditControl()
        {
            InitializeComponent();
            layoutRoot.DataContext = this;
        }

        private void unitComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int i = unitComboBox.SelectedIndex;

            secondsTextBox.Visibility = (i == 0 ? Visibility.Visible : Visibility.Collapsed);
            totalBeatsTextBox.Visibility = (i == 1 ? Visibility.Visible : Visibility.Collapsed);
            barsTextBox.Visibility = (i == 2 ? Visibility.Visible : Visibility.Collapsed);
            beatsTextBox.Visibility = (i == 2 ? Visibility.Visible : Visibility.Collapsed);
        }
    }
}
