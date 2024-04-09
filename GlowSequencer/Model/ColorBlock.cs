using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace GlowSequencer.Model
{
    public class ColorBlock : Block
    {

        private GloColor _color = GloColor.White;

        public GloColor Color { get { return _color; } set { SetProperty(ref _color, value); } }

        public ColorBlock(Timeline timeline, params Track[] tracks)
            : base(timeline, tracks)
        {
        }

        internal override IEnumerable<FileSerializer.PrimitiveBlock> BakePrimitive(Track track)
        {
            if (Tracks.Contains(track))
                yield return new FileSerializer.PrimitiveBlock(StartTime, GetEndTime(), _color, _color);
        }

        protected override GloColor GetColorAtLocalTimeCore(float localTime, Track track)
        {
            return _color;
        }

        [Obsolete]
        public override IEnumerable<GloCommand> ToGloCommands(GloSequenceContext context)
        {
            yield return new GloColorCommand(_color);
            yield return context.Advance(Duration).AsCommand();
            yield return new GloColorCommand(GloColor.Black);
        }

        public override XElement ToXML()
        {
            XElement elem = base.ToXML();
            elem.Add(new XElement("color", _color.ToHexString()));

            return elem;
        }

        protected override void PopulateFromXML(XElement element)
        {
            base.PopulateFromXML(element);
            _color = GloColor.FromHexString((string)element.Element("color"));
        }
    }
}
