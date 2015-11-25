using GlowSequencer.Model;
using GlowSequencer.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlowSequencer.ViewModel
{
    public class TimeUnit /* : Observable */
    {
        private const float BEATS_PRECISION_BIAS = 0.0001f;

        private bool _absolute = false;

        private float? _seconds; // null = indeterminate
        private Action<float> _setter;
        private MusicSegment _musicData;

        public bool IsIndeterminate { get { return _seconds == null; } }

        public float? Seconds
        {
            get { return _seconds - (_absolute ? _musicData.TimeOrigin : 0); }
            set
            {
                if (value == null) return;

                // even though this instance will be discarded shortly, the local value still needs to be changed;
                // the control that invoked the setter will query its source value one more time and will jump back to the old value if this is not set
                _seconds = value + (_absolute ? _musicData.TimeOrigin : 0);

                _setter(value.Value + (_absolute ? _musicData.TimeOrigin : 0));
                //Notify("Seconds");
                //Notify("TotalBeats");
                //Notify("Bars");
                //Notify("Beats");
            }
        }

        public float? TotalBeats
        {
            get { return Seconds * _musicData.GetBeatsPerSecond() + GetAbsoluteBias(); }
            set { Seconds = (value - GetAbsoluteBias()) / _musicData.GetBeatsPerSecond(); }
        }

        public int? Bars
        {
            //get { return (int?)(TotalBeats - GetAbsoluteBias() + BEATS_PRECISION_BIAS) / _musicData.BeatsPerBar + GetAbsoluteBias(); }
            get { return MathUtil.FloorToInt((TotalBeats - GetAbsoluteBias() + BEATS_PRECISION_BIAS) / _musicData.BeatsPerBar) + GetAbsoluteBias(); }
            set { TotalBeats = ((value - GetAbsoluteBias()) * _musicData.BeatsPerBar) + Beats; }
        }
        public float? Beats
        {
            //get { return (TotalBeats - GetAbsoluteBias() + BEATS_PRECISION_BIAS) % _musicData.BeatsPerBar + GetAbsoluteBias(); }
            get { return MathUtil.RealMod(TotalBeats - GetAbsoluteBias() + BEATS_PRECISION_BIAS, _musicData.BeatsPerBar) + GetAbsoluteBias(); }
            set { TotalBeats = ((Bars - GetAbsoluteBias()) * _musicData.BeatsPerBar) + value; }
        }

        public bool HasMusicData
        {
            get { return _musicData.GetIndex() != 0; }
        }


        private int GetAbsoluteBias()
        {
            return (_absolute ? 1 : 0);
        }

        public static TimeUnit Wrap(float? seconds, MusicSegment musicData, Action<float> setter)
        {
            return new TimeUnit { _seconds = seconds, _setter = setter, _musicData = musicData };
        }

        public static TimeUnit WrapAbsolute(float? seconds, MusicSegment musicData, Action<float> setter)
        {
            return new TimeUnit { _seconds = seconds, _setter = setter, _musicData = musicData, _absolute = true };
        }

    }
}
