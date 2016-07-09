using ContinuousLinq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace GlowSequencer.Model
{
    public class Track : Observable
    {
        public const string DEFAULT_BASE_LABEL = "Track";

        private Timeline _timeline;
        private string _label;

        public string Label { get { return _label; } set { SetProperty(ref _label, value); } }

        /// <summary>
        /// THIS IS NOT SORTED THE SAME WAY AS Timeline.Blocks!! DEMONS WAIT HERE IF YOU TRY TO USE THIS FOR ORDER-SENSITIVE WORK.
        /// </summary>
        public ReadOnlyContinuousCollection<Block> Blocks { get; private set; }

        public Track(Timeline timeline, string label)
        {
            _timeline = timeline;
            _label = label;

            Blocks = timeline.Blocks.Where(block => block.TrackNotificationPlaceholder && block.Tracks.Contains(this));
        }

        public int GetIndex()
        {
            return _timeline.Tracks.IndexOf(this);
        }

        public Timeline GetTimeline()
        {
            return _timeline;
        }

        public XElement ToXML()
        {
            XElement elem = new XElement("track");
            elem.SetAttributeValue("id", GetIndex());
            elem.Value = _label;

            return elem;
        }

        public static Track FromXML(Timeline timeline, XElement element)
        {
            return new Track(timeline, element.Value);
        }
    }
}
