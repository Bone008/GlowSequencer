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

        public GloColor RenderedColor1 { get { return (ColorModifierFn != null ? ColorModifierFn(StartTime, _color) : _color); } }
        public GloColor RenderedColor2 { get { return (ColorModifierFn != null ? ColorModifierFn(GetEndTime(), _color) : _color); } }

        public ColorBlock(Timeline timeline, params Track[] tracks)
            : base(timeline, tracks)
        {
            ForwardPropertyEvents(nameof(Color), this, nameof(RenderedColor1), nameof(RenderedColor2));
            ForwardPropertyEvents(nameof(ColorModifierFn), this, nameof(RenderedColor1), nameof(RenderedColor2));
            ForwardPropertyEvents(nameof(StartTime), this, () => { if (ColorModifierFn != null) { Notify(nameof(RenderedColor1)); Notify(nameof(RenderedColor2)); } });
            ForwardPropertyEvents(nameof(Duration), this, () => { if (ColorModifierFn != null) { Notify(nameof(RenderedColor2)); } });
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
