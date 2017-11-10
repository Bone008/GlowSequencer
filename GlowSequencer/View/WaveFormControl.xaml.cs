using GlowSequencer.Audio;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    /// Interaction logic for WaveFormControl.xaml
    /// </summary>
    public partial class WaveFormControl : UserControl
    {
        public static readonly DependencyProperty IsLoadingProperty =
            DependencyProperty.Register("IsLoading", typeof(bool), typeof(WaveFormControl), new PropertyMetadata(false));
        public static readonly DependencyProperty WaveformProperty =
            DependencyProperty.Register("Waveform", typeof(Waveform), typeof(WaveFormControl), new PropertyMetadata());
        public static readonly DependencyProperty TimeScaleProperty =
            DependencyProperty.Register("TimeScale", typeof(float), typeof(WaveFormControl), new PropertyMetadata(1.0f));
        public static readonly DependencyProperty PixelOffsetProperty =
            DependencyProperty.Register("PixelOffset", typeof(double), typeof(WaveFormControl), new PropertyMetadata(0.0));

        public bool IsLoading
        {
            get { return (bool)GetValue(IsLoadingProperty); }
            set { SetValue(IsLoadingProperty, value); }
        }

        public Waveform Waveform
        {
            get { return (Waveform)GetValue(WaveformProperty); }
            set { SetValue(WaveformProperty, value); }
        }

        public float TimeScale
        {
            get { return (float)GetValue(TimeScaleProperty); }
            set { SetValue(TimeScaleProperty, value); }
        }
        public double PixelOffset
        {
            get { return (double)GetValue(PixelOffsetProperty); }
            set { SetValue(PixelOffsetProperty, value); }
        }

        public WaveFormControl()
        {
            InitializeComponent();
        }
    }
}
