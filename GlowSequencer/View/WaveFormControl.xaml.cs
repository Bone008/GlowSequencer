using GlowSequencer.Audio;
using GlowSequencer.ViewModel;
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
    public partial class WaveformControl : UserControl
    {
        public static readonly DependencyProperty IsLoadingProperty =
            DependencyProperty.Register("IsLoading", typeof(bool), typeof(WaveformControl), new PropertyMetadata(false));
        public static readonly DependencyProperty WaveformProperty =
            DependencyProperty.Register("Waveform", typeof(Waveform), typeof(WaveformControl), new PropertyMetadata());
        public static readonly DependencyProperty WaveformDisplayModeProperty =
            DependencyProperty.Register("WaveformDisplayMode", typeof(WaveformDisplayMode), typeof(WaveformControl), new PropertyMetadata());
        public static readonly DependencyProperty TimeScaleProperty =
            DependencyProperty.Register("TimeScale", typeof(float), typeof(WaveformControl), new PropertyMetadata(1.0f));
        public static readonly DependencyProperty PixelOffsetProperty =
            DependencyProperty.Register("PixelOffset", typeof(double), typeof(WaveformControl), new PropertyMetadata(0.0));
        
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

        public WaveformDisplayMode WaveformDisplayMode
        {
            get { return (WaveformDisplayMode)GetValue(WaveformDisplayModeProperty); }
            set { SetValue(WaveformDisplayModeProperty, value); }
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

        public WaveformControl()
        {
            InitializeComponent();
            this.SizeChanged += WaveformControl_SizeChanged;
        }

        private void WaveformControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            centerLine.Y1 = centerLine.Y2 = e.NewSize.Height / 2;
        }
    }
}
