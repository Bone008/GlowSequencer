using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace GlowSequencer.Model
{
    public class RampBlock : Block
    {
        
        private GloColor _startColor = GloColor.Black;
        private GloColor _endColor = GloColor.White;
        
        public GloColor StartColor { get { return _startColor; } set { SetProperty(ref _startColor, value); } }
        public GloColor EndColor { get { return _endColor; } set { SetProperty(ref _endColor, value); } }

        public RampBlock(Timeline timeline, params Track[] tracks)
            : base(timeline, tracks)
        {
        }
        internal override IEnumerable<FileSerializer.PrimitiveBlock> BakePrimitive(Track track)
        {
            if (Tracks.Contains(track))
                yield return new FileSerializer.PrimitiveBlock(StartTime, GetEndTime(), _startColor, _endColor);
        }

        protected override GloColor GetColorAtLocalTimeCore(float localTime, Track track)
        {
            return GloColor.Blend(_startColor, _endColor, localTime / Duration);
        }

        [Obsolete]
        public override IEnumerable<GloCommand> ToGloCommands(GloSequenceContext context)
        {
            yield return new GloColorCommand(_startColor);
            yield return new GloRampCommand(_endColor, context.Advance(Duration).Ticks);
            yield return new GloColorCommand(GloColor.Black);
        }

        public override XElement ToXML()
        {
            XElement elem = base.ToXML();
            elem.Add(new XElement("start-color", _startColor.ToHexString()));
            elem.Add(new XElement("end-color", _endColor.ToHexString()));

            return elem;
        }

        protected override void PopulateFromXML(XElement element)
        {
            base.PopulateFromXML(element);
            _startColor = GloColor.FromHexString((string)element.Element("start-color"));
            _endColor = GloColor.FromHexString((string)element.Element("end-color"));
        }

    }
}
