using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace GlowSequencer.Model
{
    public class MusicSegment : Observable
    {
        public static MusicSegment CreateStandard(Timeline timeline)
        {
            return new MusicSegment(timeline) { Label = "<none>", Bpm = 60, BeatsPerBar = 1, TimeOrigin = 0, IsReadOnly = true };
        }

        private Timeline _timeline;

        private string _label = "Unnamed";
        private float _bpm = 128;
        private int _beatsPerBar = 4;
        private float _timeOrigin = 0;

        private bool _readonly = false;
        internal bool _fallbackOverride = false;


        public string Label
        {
            get { return _label; }
            set { if (IsReadOnly) throw new InvalidOperationException("readonly segment"); SetProperty(ref _label, value); }
        }
        public float Bpm
        {
            get { return _bpm; }
            set { if (IsReadOnly) throw new InvalidOperationException("readonly segment"); SetProperty(ref _bpm, value); }
        }
        public int BeatsPerBar
        {
            get { return _beatsPerBar; }
            set { if (IsReadOnly) throw new InvalidOperationException("readonly segment"); SetProperty(ref _beatsPerBar, value); }
        }
        public float TimeOrigin
        {
            get { return _timeOrigin; }
            set { if (IsReadOnly) throw new InvalidOperationException("readonly segment"); SetProperty(ref _timeOrigin, value); }
        }


        public bool IsReadOnly
        {
            get { return _readonly; }
            private set { SetProperty(ref _readonly, value); }
        }
        
        [Obsolete]
        public bool IsDefault
        {
            get { return _timeline.DefaultMusicSegment == this; }
        }


        public MusicSegment(Timeline timeline)
        {
            _timeline = timeline;
        }

        public float GetBeatsPerSecond()
        {
            return Bpm / 60.0f;
        }

        public int GetIndex()
        {
            return _timeline.MusicSegments.IndexOf(this);
        }

        public void OnIsDefaultChanged()
        {
            Notify("IsDefault");
        }



        public XElement ToXML()
        {
            if (_readonly)
                throw new InvalidOperationException("the default segment cannot be serialized");

            XElement elem = new XElement("segment");
            elem.SetAttributeValue("id", GetIndex());
            elem.Add(new XElement("label", _label));
            elem.Add(new XElement("bpm", _bpm));
            elem.Add(new XElement("beats-per-bar", _beatsPerBar));
            elem.Add(new XElement("time-origin", _timeOrigin));

            return elem;
        }

        public static MusicSegment FromXML(Timeline timeline, XElement element)
        {
            if (timeline == null)
                throw new ArgumentNullException("timeline");

            return new MusicSegment(timeline)
            {
                Label = (string)element.Element("label"),
                Bpm = (float)element.Element("bpm"),
                BeatsPerBar = (int)element.Element("beats-per-bar"),
                TimeOrigin = (float)element.Element("time-origin")
            };
        }

    }
}
