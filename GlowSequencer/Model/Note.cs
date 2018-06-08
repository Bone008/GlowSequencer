using GlowSequencer.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace GlowSequencer.Model
{
    /// <summary>
    /// A marker that was added to the sequence at a given time.
    /// </summary>
    public class Note : Observable
    {
        private string _label = null;
        private string _description = null;
        private float _time = 0;

        /// <summary>May be null. Empty values are auto-converted to null.</summary>
        public string Label { get { return _label; } set { SetProperty(ref _label, StringUtil.WhiteSpaceToNull(value)); } }
        /// <summary>May be null. Empty values are auto-converted to null.</summary>
        public string Description { get { return _description; } set { SetProperty(ref _description, StringUtil.WhiteSpaceToNull(value)); } }
        public float Time { get { return _time; } set { SetProperty(ref _time, Math.Max(0, value)); } }

        public XElement ToXML()
        {
            return new XElement("note",
                new XElement("label", _label),
                new XElement("description", _description),
                new XElement("time", _time)
            );
        }

        public static Note FromXML(Timeline timeline, XElement element)
        {
            // timeline arg is unused for now

            return new Note
            {
                Label = (string)element.Element("label"),
                Description = (string)element.Element("description"),
                Time = ((float?)element.Element("time")) ?? 0,
            };
        }
    }
}
