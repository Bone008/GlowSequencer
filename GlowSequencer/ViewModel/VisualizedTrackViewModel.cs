using GlowSequencer.Model;
using GlowSequencer.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Media;

namespace GlowSequencer.ViewModel
{
    public class VisualizedTrackViewModel : Observable
    {
        //private static readonly MoreVibrantColorConverter VIBRANT_COLOR_CONVERTER = new MoreVibrantColorConverter();
        //private static Color MakeVibrant(Color c) { return (Color)VIBRANT_COLOR_CONVERTER.Convert(c, typeof(Color), null, CultureInfo.InvariantCulture); }


        internal readonly Track track;
        private Color _currentColor = Colors.Black;

        public Color CurrentColor { get { return _currentColor; } set { SetProperty(ref _currentColor, value); } }
        //public bool IsColorBright => ColorUtil.GetPerceivedBrightness(MakeVibrant(_currentColor)) > 0.5;

        public string Label => track.Label;

        public VisualizedTrackViewModel(Track track)
        {
            this.track = track;

            //ForwardPropertyEvents(nameof(CurrentColor), this, nameof(IsColorBright));
            ForwardPropertyEvents(nameof(track.Label), track, nameof(Label));
        }
    }
}
