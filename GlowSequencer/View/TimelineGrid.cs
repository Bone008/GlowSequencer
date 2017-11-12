using GlowSequencer.Util;
using GlowSequencer.ViewModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
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
    /// Interaction logic for TimelineGrid.xaml
    /// </summary>
    public class TimelineGrid : FrameworkElement
    {
        private const double HEADER_HEIGHT = 45;

        private static readonly Pen pen_headerSeparator = new Pen(new SolidColorBrush(Colors.DarkGray), 1);
        private static readonly Pen pen_thinLine = new Pen(new SolidColorBrush(Colors.LightGray), 1);
        private static readonly Pen pen_mediumLine = new Pen(new SolidColorBrush(Colors.Gray), 2);
        private static readonly Pen pen_thickLine = new Pen(new SolidColorBrush(Colors.Black), 2);

        private static readonly FormattedText s_unitLabel_seconds = ToRenderText("sec");
        private static readonly FormattedText s_unitLabel_beats = ToRenderText("beat");
        private static readonly FormattedText s_unitLabel_barsbeats = ToRenderText("bar");

        private static FormattedText ToRenderText(string text, bool bold = false)
        {
            var tf = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, (bold ? FontWeights.Bold : FontWeights.Normal), FontStretches.Normal);
            return new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, tf, 12, Brushes.Black);
        }

        public static readonly DependencyProperty GridOffsetProperty =
            DependencyProperty.Register("GridOffset", typeof(double), typeof(TimelineGrid), new FrameworkPropertyMetadata(0.0) { AffectsRender = true });
        public static readonly DependencyProperty GridScaleProperty =
            DependencyProperty.Register("GridScale", typeof(double), typeof(TimelineGrid), new FrameworkPropertyMetadata(0.0) { AffectsRender = true });
        public static readonly DependencyProperty GridIntervalProperty =
            DependencyProperty.Register("GridInterval", typeof(float), typeof(TimelineGrid), new FrameworkPropertyMetadata(0.0f) { AffectsRender = true });
        public static readonly DependencyProperty MusicSegmentProperty =
            DependencyProperty.Register("MusicSegment", typeof(MusicSegmentViewModel), typeof(TimelineGrid), new FrameworkPropertyMetadata(null) { AffectsRender = true, PropertyChangedCallback = MusicSegment_Changed });


        public double GridOffset
        {
            get { return (double)GetValue(GridOffsetProperty); }
            set { SetValue(GridOffsetProperty, value); }
        }

        public double GridScale
        {
            get { return (double)GetValue(GridScaleProperty); }
            set { SetValue(GridScaleProperty, value); }
        }

        public float GridInterval
        {
            get { return (float)GetValue(GridIntervalProperty); }
            set { SetValue(GridIntervalProperty, value); }
        }

        public MusicSegmentViewModel MusicSegment
        {
            get { return (MusicSegmentViewModel)GetValue(MusicSegmentProperty); }
            set { SetValue(MusicSegmentProperty, value); }
        }

        public TimelineGrid()
        {
            ClipToBounds = true;
        }

        private static void MusicSegment_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TimelineGrid grid = (TimelineGrid)d;

            if (e.OldValue as MusicSegmentViewModel != null)
                ((MusicSegmentViewModel)e.OldValue).PropertyChanged -= grid.MusicSegment_PropertyChanged;
            if (e.NewValue as MusicSegmentViewModel != null)
                ((MusicSegmentViewModel)e.NewValue).PropertyChanged += grid.MusicSegment_PropertyChanged;
        }
        
        private void MusicSegment_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MusicSegmentViewModel.Bpm)
                || e.PropertyName == nameof(MusicSegmentViewModel.BeatsPerBar)
                || e.PropertyName == nameof(MusicSegmentViewModel.TimeOriginSeconds))
            {
                InvalidateVisual();
            }
        }


        protected override void OnRender(DrawingContext g)
        {
            base.OnRender(g);
            if (MusicSegment == null)
                return;

            g.PushClip(Geometry.Combine(
                    new RectangleGeometry(new Rect(0, 0, RenderSize.Width, RenderSize.Height)),
                    new RectangleGeometry(new Rect(0, 0, 30, HEADER_HEIGHT)),
                    GeometryCombineMode.Exclude, Transform.Identity
            ));

            double h = RenderSize.Height;

            double offsetPx = GridOffset;
            float offsetTime = (float)(offsetPx / GridScale) - MusicSegment.TimeOriginSeconds;

            float intervalTime = GridInterval;
            double intervalPx = intervalTime * GridScale;

            float t = -(offsetTime % intervalTime); // relative time at render point
            int i = (int)(offsetTime / intervalTime); // index of bar at render point
            double x; // x coordinate of render point

            while ((x = t * GridScale) < RenderSize.Width)
            {
                Pen p = GetAppropiatePen(i);
                g.DrawLine(p, new Point(x, HEADER_HEIGHT - (p == pen_thickLine ? 10 : (i % 2 == 0 ? 7 : 4))), new Point(x, h));

                if (i % 2 == 0)
                {
                    float absT = offsetTime + t;

                    FormattedText text = ToRenderText(FormatTimestamp(absT + MusicSegment.TimeOriginSeconds)); // correct for global timeline time again (independant from time origin)
                    g.DrawText(text, new Point(x - text.Width / 2, 4));
                    if (!MusicSegment.IsReadOnly)
                    {
                        float beats = absT * MusicSegment.GetBeatsPerSecond(); // 0-based
                        float display;

                        if (p == pen_thickLine)
                            // bars
                            display = (float)Math.Floor((beats + 0.0001f) / MusicSegment.BeatsPerBar) + 1;
                        else
                            // (local) beats
                            display = MathUtil.RealMod((beats - 0.0001f), MusicSegment.BeatsPerBar) + 1;

                        text = ToRenderText(display.ToString("0.##", CultureInfo.InvariantCulture), (p == pen_thickLine));
                        g.DrawText(text, new Point(x - text.Width / 2, 18));
                    }
                }

                t += intervalTime;
                i++;
                //g.DrawLine(new Pen(new SolidColorBrush(elem.BorderColor), elem.Thickness.Left), new Point(x, 0), new Point(x, h));
            }

            g.Pop();
            //g.DrawRectangle(Brushes.White, null, new Rect(0, 0, 30, HEADER_HEIGHT));
            g.DrawText(s_unitLabel_seconds, new Point(3, 4));
            if (!MusicSegment.IsReadOnly)
                g.DrawText(s_unitLabel_beats, new Point(3, 18));
            g.DrawLine(pen_headerSeparator, new Point(0, HEADER_HEIGHT), new Point(RenderSize.Width, HEADER_HEIGHT));
        }

        private Pen GetAppropiatePen(int n)
        {
            int beatsPerBar = MusicSegment.BeatsPerBar;

            float interval = GridInterval;
            int periodBar = (int)Math.Round(1 / MusicSegment.GetBeatsPerSecond() / interval * beatsPerBar);

            if (periodBar <= 1) // every line is at least a whole bar
                return pen_thickLine;

            if (n % periodBar == 0)
                return pen_thickLine;
            else if (periodBar > beatsPerBar && n % (periodBar / beatsPerBar) == 0) // mark beats medium, but only if they were subdivided
                return pen_mediumLine;
            else
                return pen_thinLine;
        }

        private string FormatTimestamp(float sec)
        {
            string sign = "";
            if (sec < 0)
            {
                sec = -sec;
                sign = "-";
            }

            TimeSpan ts = TimeSpan.FromSeconds(sec);

            //if (ts.TotalMinutes >= 1)
            //str += Math.Floor(ts.TotalMinutes) + ":";

            return sign + Math.Floor(ts.TotalMinutes) + ":" + ts.ToString("ss\\.ff", CultureInfo.InvariantCulture);
        }

    }
}
