using GlowSequencer.Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace GlowSequencer.View
{
    class WaveformVisual : FrameworkElement
    {
        public static readonly DependencyProperty WaveformProperty =
            DependencyProperty.Register("Waveform", typeof(Waveform), typeof(WaveformVisual), new PropertyMetadata(Waveform_Changed));
        public static readonly DependencyProperty TimeScaleProperty =
            DependencyProperty.Register("TimeScale", typeof(float), typeof(WaveformVisual), new PropertyMetadata(Waveform_Changed));

        private static void Waveform_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((WaveformVisual)d).UpdateWaveform();
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

        private readonly VisualCollection children;
        private double halfHeight = 0;

        public WaveformVisual()
        {
            children = new VisualCollection(this);

            this.SizeChanged += OnSizeChanged;
            halfHeight = ActualHeight / 2;
        }

        [Obsolete]
        public void SetWaveform(Waveform wf) { }

        private void UpdateWaveform()
        {
            children.Clear();
            if (Waveform != null)
                children.Add(CreateWaveFormVisual());
            this.InvalidateVisual();
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.NewSize.Height == e.PreviousSize.Height)
                return;

            halfHeight = ActualHeight / 2;
            UpdateWaveform();
        }

        private DrawingVisual CreateWaveFormVisual()
        {
            DrawingVisual drawingVisual = new DrawingVisual();

            // Retrieve the DrawingContext in order to create new drawing content.
            DrawingContext drawingContext = drawingVisual.RenderOpen();

            RenderPolygon(drawingContext);

            // Persist the drawing content.
            drawingContext.Close();

            return drawingVisual;
        }

        private void RenderPolygon(DrawingContext drawingContext)
        {
            var fillBrush = Brushes.LawnGreen;
            var borderPen = new Pen(Brushes.DarkGreen, 1.0);

            var maximums = Waveform.Maximums;
            var minimums = Waveform.Minimums;
            if (maximums.Length == 0) return;
            //  px/sample = px/sec    *  sec/sample
            double xScale = TimeScale * Waveform.TimePerSample;
            double offsetPx = Waveform.TimeOffset * TimeScale;

            StreamGeometry geometry = new StreamGeometry();
            using (var ctx = geometry.Open())
            {
                ctx.BeginFigure(new Point(offsetPx, SampleToYPosition(maximums[0])), isFilled: true, isClosed: true);

                for (int i = 1; i < maximums.Length; i++)
                    ctx.LineTo(new Point(offsetPx + i * xScale, SampleToYPosition(maximums[i])), true, false);

                ctx.LineTo(new Point(offsetPx + maximums.Length * xScale, SampleToYPosition(maximums[maximums.Length - 1])), true, false);
                ctx.LineTo(new Point(offsetPx + minimums.Length * xScale, SampleToYPosition(minimums[minimums.Length - 1])), true, false);

                for (int i = minimums.Length - 1; i >= 0; i--)
                    ctx.LineTo(new Point(offsetPx + i * xScale, SampleToYPosition(minimums[i])), true, false);
            }
            geometry.Freeze();

            drawingContext.DrawGeometry(fillBrush, borderPen, geometry);

            //PathFigure myPathFigure = new PathFigure();
            //myPathFigure.StartPoint = new Point(0, SampleToYPosition(maxPoints[0]));

            ////PolyLineSegment seg = new PolyLineSegment(

            //PathSegmentCollection myPathSegmentCollection = new PathSegmentCollection();

            //for (int i = 1; i < maxPoints.Length; i++)
            //{
            //    myPathSegmentCollection.Add(new LineSegment(new Point(i, SampleToYPosition(maxPoints[i])), true));
            //}
            //for (int i = minPoints.Length - 1; i >= 0; i--)
            //{
            //    myPathSegmentCollection.Add(new LineSegment(new Point(i, SampleToYPosition(minPoints[i])), true));
            //}

            //myPathFigure.Segments = myPathSegmentCollection;
            //PathGeometry myPathGeometry = new PathGeometry();

            //myPathGeometry.Figures.Add(myPathFigure);

            //drawingContext.DrawGeometry(fillBrush, borderPen, myPathGeometry);
        }

        private double SampleToYPosition(float value)
        {
            const double clipAt = 0.95;
            double scaledValue = Math.Sign(value) / (Math.Log(Math.Min(Math.Abs(value), clipAt)) / Math.Log(clipAt));
            return (1 - scaledValue) * halfHeight;
        }

        // Provide a required override for the VisualChildrenCount property.
        protected override int VisualChildrenCount => children.Count;

        // Provide a required override for the GetVisualChild method.
        protected override Visual GetVisualChild(int index)
        {
            if (index < 0 || index >= children.Count)
            {
                throw new ArgumentOutOfRangeException();
            }

            return children[index];
        }

    }
}
