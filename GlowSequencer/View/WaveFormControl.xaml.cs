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
        public static WaveFormControl instance;

        public static readonly DependencyProperty IsLoadingProperty =
            DependencyProperty.Register("IsLoading", typeof(bool), typeof(WaveFormControl), new PropertyMetadata(false));
        public static readonly DependencyProperty WaveformProperty =
            DependencyProperty.Register("Waveform", typeof(Waveform), typeof(WaveFormControl), new PropertyMetadata(Waveform_Changed));
        public static readonly DependencyProperty TimeScaleProperty =
            DependencyProperty.Register("TimeScale", typeof(float), typeof(WaveFormControl), new PropertyMetadata(Waveform_Changed));
        public static readonly DependencyProperty PixelOffsetProperty =
            DependencyProperty.Register("PixelOffset", typeof(double), typeof(WaveFormControl), new PropertyMetadata(0.0));

        private static void Waveform_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((WaveFormControl)d).UpdateWaveform();
        }

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
        
        private double yTranslate = 40;
        private double yScale = 40;

        public WaveFormControl()
        {
            this.SizeChanged += OnSizeChanged;
            InitializeComponent();
            instance = this;
        }

        private void UpdateWaveform()
        {
            var wf = Waveform;
            if(wf == null)
            {
                waveformPolygon.Points.Clear();
                return;
            }
            Debug.Assert(wf.Minimums.Length == wf.Maximums.Length);
            Debug.WriteLine("redrawing waveform with samples: " + wf.Maximums.Length);
            
            //  px/sample = px/sec    *  sec/sample
            double xScale = TimeScale * wf.TimePerSample.TotalSeconds;

            float[] mins = wf.Minimums;
            float[] maxs = wf.Maximums;
            PointCollection points = new PointCollection(maxs.Length * 2);

            for (int x = 0; x < maxs.Length; x++)
                points.Add(new Point(x * xScale, SampleToYPosition(maxs[x])));
            for (int x = mins.Length - 1; x >= 0; x--)
                points.Add(new Point(x * xScale, SampleToYPosition(mins[x])));

            waveformPolygon.Points = points;
        }

        private double SampleToYPosition(float value)
        {
            return yTranslate + value * yScale;
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.NewSize.Height == e.PreviousSize.Height)
                return;

            yTranslate = ActualHeight / 2;
            yScale = ActualHeight / 2;
            UpdateWaveform();
        }


        //private double xScale = 2;
        //private int blankZone = 10;

        //public void AddValue(float minValue, float maxValue)
        //{
        //    int visiblePixels = (int)(ActualWidth / xScale);
        //    if (visiblePixels > 0)
        //    {
        //        CreatePoint(maxValue, minValue);

        //        if (renderPosition > visiblePixels)
        //        {
        //            renderPosition = 0;
        //        }
        //        int erasePosition = (renderPosition + blankZone) % visiblePixels;
        //        if (erasePosition < GetPointsCount())
        //        {
        //            double yPos = SampleToYPosition(0);
        //            waveformPolygon.Points[erasePosition] = new Point(erasePosition * xScale, yPos);
        //            waveformPolygon.Points[BottomPointIndex(erasePosition)] = new Point(erasePosition * xScale, yPos);
        //        }
        //    }
        //}

        //private void ClearAllPoints()
        //{
        //    waveformPolygon.Points.Clear();
        //}

        //private int BottomPointIndex(int position)
        //{
        //    return waveformPolygon.Points.Count - position - 1;
        //}

        //private int GetPointsCount()
        //{
        //    return waveformPolygon.Points.Count / 2;
        //}

        //private void CreatePoint(float topValue, float bottomValue)
        //{
        //    double topYPos = SampleToYPosition(topValue);
        //    double bottomYPos = SampleToYPosition(bottomValue);
        //    double xPos = renderPosition * xScale;
        //    if (renderPosition >= GetPointsCount())
        //    {
        //        int insertPos = GetPointsCount();
        //        waveformPolygon.Points.Insert(insertPos, new Point(xPos, topYPos));
        //        waveformPolygon.Points.Insert(insertPos + 1, new Point(xPos, bottomYPos));
        //    }
        //    else
        //    {
        //        waveformPolygon.Points[renderPosition] = new Point(xPos, topYPos);
        //        waveformPolygon.Points[BottomPointIndex(renderPosition)] = new Point(xPos, bottomYPos);
        //    }
        //    renderPosition++;
        //}
    }
}
