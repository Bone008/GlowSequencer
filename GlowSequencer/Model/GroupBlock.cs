using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using GlowSequencer.Util;
using System.Collections.Specialized;

namespace GlowSequencer.Model
{
    public abstract class GroupBlock : Block
    {
        private ObservableCollection<Block> _children = new ObservableCollection<Block>();
        private GloColor _overlayStartColor = GloColor.White;
        private GloColor _overlayEndColor = GloColor.White;

        public ObservableCollection<Block> Children { get { return _children; } }

        public GloColor OverlayStartColor { get { return _overlayStartColor; } set { SetProperty(ref _overlayStartColor, value); } }
        public GloColor OverlayEndColor { get { return _overlayEndColor; } set { SetProperty(ref _overlayEndColor, value); } }


        public GroupBlock(Timeline timeline)
            : base(timeline)
        {
            RecalcTracks();

            _children.CollectionChanged += (sender, e) =>
            {
                if (e.Action == NotifyCollectionChangedAction.Reset)
                    throw new NotSupportedException("clearing children of group blocks is not supported");

                if (e.NewItems != null)
                {
                    Duration = Duration; // re-evaluate GetMinDuration(), will not raise any events if the old duration was already within limits
                    foreach (Block b in e.NewItems)
                    {
                        ForwardPropertyEvents("TrackNotificationPlaceholder", b, RecalcTracks);
                        b.ColorModifierFn = TransformChildColor;
                    }
                }

                if (e.OldItems != null)
                    foreach (Block b in e.OldItems)
                        b.ColorModifierFn = null;

                RecalcTracks();
            };


            Action notifyChildrenCol = () => { foreach (var child in _children) child.NotifyColorModifierFn(); };
            ForwardPropertyEvents("Duration", this, notifyChildrenCol);
            ForwardPropertyEvents("OverlayStartColor", this, notifyChildrenCol);
            ForwardPropertyEvents("OverlayEndColor", this, notifyChildrenCol);
        }


        private GloColor TransformChildColor(float time, GloColor color)
        {
            // TODO FIXME how to handle loops here?
            // TODO implement the transformed properties for ramps
            return GloColor.Blend(_overlayStartColor, _overlayEndColor, time / Duration) * color;
        }

        public void RecalcTracks()
        {
            var affectedTracks = _children.SelectMany(b => b.Tracks).Distinct().ToArray();
            foreach (Track t in Tracks.ToArray())
                if (!affectedTracks.Contains(t))
                    Tracks.Remove(t);
            foreach (Track t in affectedTracks)
                if (!Tracks.Contains(t))
                    Tracks.Add(t);
        }

        public override void ExtendToTrack(Track fromTrack, Track toTrack, GuiLabs.Undo.ActionManager am = null)
        {
            foreach (var child in _children.Where(b => b.Tracks.Contains(fromTrack)))
                am.RecordAdd(child.Tracks, toTrack);

            RecalcTracks();
        }

        public override bool RemoveFromTrack(Track track, GuiLabs.Undo.ActionManager am = null)
        {
            if (base.RemoveFromTrack(track, am))
            {
                foreach (var child in _children.Where(b => b.Tracks.Contains(track)).Reverse().ToArray())
                {
                    if (child.Tracks.Count > 1)
                        am.RecordRemove(child.Tracks, track);
                    else
                        am.RecordRemove(_children, child);
                }

                // since the base method already removed the track from the group block,
                // this call should in theory be redundant
                RecalcTracks();
                return true;
            }
            return false;
        }

        public override float GetMinDuration()
        {
            return Math.Max(base.GetMinDuration(), _children.Max(b => (float?)b.GetEndTime()).GetValueOrDefault(0));
        }

        public GroupBlock AddChild(Block child, bool transformToLocal)
        {
            if (transformToLocal)
                child.StartTime -= StartTime;

            _children.Add(child);
            return this;
        }

        public override IEnumerable<GloCommand> ToGloCommands(GloSequenceContext context)
        {
            throw new NotImplementedException("implemented by LoopBlock");
        }

        public override XElement ToXML()
        {
            XElement elem = base.ToXML();
            elem.Element("affected-tracks").Remove(); // this is a calculated field for group blocks
            elem.Add(new XElement("overlay-start", _overlayStartColor.ToHexString()));
            elem.Add(new XElement("overlay-end", _overlayEndColor.ToHexString()));
            elem.Add(new XElement("children", _children.Select(b => b.ToXML())));

            return elem;
        }

        protected override void PopulateFromXML(XElement element)
        {
            base.PopulateFromXML(element);
            _overlayStartColor = GloColor.FromHexString((string)element.Element("overlay-start") ?? "ffffff");
            _overlayEndColor = GloColor.FromHexString((string)element.Element("overlay-end") ?? "ffffff");
            foreach (XElement childElem in element.ElementOrEmpty("children").Elements("block"))
                _children.Add(Block.FromXML(timeline, childElem));
        }
    }
}
