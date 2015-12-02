using GlowSequencer.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace GlowSequencer.Model
{
    public class Timeline : Observable
    {
        private MusicSegment _defaultMusicSegment;

        public ObservableCollection<Block> Blocks { get; private set; }
        public ObservableCollection<Track> Tracks { get; private set; }

        public ObservableCollection<MusicSegment> MusicSegments { get; private set; }

        public MusicSegment DefaultMusicSegment
        {
            get { return _defaultMusicSegment; }
            set
            {
                SetProperty(ref _defaultMusicSegment, value);
                foreach (var seg in MusicSegments)
                    seg.OnIsDefaultChanged();
            }
        }

        public Timeline()
        {
            Blocks = new ObservableCollection<Block>();
            Tracks = new ObservableCollection<Track>();
            MusicSegments = new ObservableCollection<MusicSegment>();
            MusicSegments.Add(MusicSegment.CreateStandard(this)); // this is ALWAYS here, even when loading from XML

            _defaultMusicSegment = MusicSegments[0];
        }

        public string DeriveTrackLabel(string baseLabel)
        {
            string separator = " ";

            int n;
            int padWidth;
            if (baseLabel == Track.DEFAULT_BASE_LABEL)
            {
                n = Tracks.Count + 1;
                padWidth = 2;
            }
            else
            {
                Match m = Regex.Match(baseLabel, @"(.+?)( ?)(\d+)$");
                if (m.Success)
                {
                    baseLabel = m.Groups[1].Value;
                    separator = m.Groups[2].Value;
                    n = int.Parse(m.Groups[3].Value) + 1;
                    padWidth = m.Groups[3].Length;
                }
                else
                {
                    n = 2;
                    padWidth = 2;
                }
            }

            string label;
            do
            {
                label = baseLabel + separator + (n++).ToString().PadLeft(padWidth, '0');
            }
            while (Tracks.Any(g => g.Label.Equals(label, StringComparison.InvariantCultureIgnoreCase)));

            return label;
        }

        public Timeline SetupNew()
        {
            Tracks.Add(new Track(this, DeriveTrackLabel(Track.DEFAULT_BASE_LABEL)));
            return this;
        }

#if DEBUG
        public void SetupTestData()
        {
            // test data
            MusicSegments.Add(new MusicSegment(this) { Label = "Mah segment #1", Bpm = 128, BeatsPerBar = 4, TimeOrigin = 0 });
            MusicSegments.Add(new MusicSegment(this) { Label = "Slow segment", Bpm = 96, BeatsPerBar = 3, TimeOrigin = 10 });
            MusicSegments.Add(new MusicSegment(this) { Label = "D&B example", Bpm = 180, BeatsPerBar = 4, TimeOrigin = 20 });
            DefaultMusicSegment = MusicSegments[1];

            Tracks.Add(new Track(this, "Second track"));
            Tracks.Add(new Track(this, "Third track"));

            Blocks.Add(new ColorBlock(this, Tracks[0]) { StartTime = 0f, Duration = 0.5f, Color = GloColor.FromRGB(255, 0, 0), SegmentContext = MusicSegments[0] });
            Blocks.Add(new ColorBlock(this, Tracks[0]) { StartTime = 1f, Duration = 0.5f, Color = GloColor.FromRGB(255, 127, 0), SegmentContext = MusicSegments[0] });
            Blocks.Add(new RampBlock(this, Tracks[0]) { StartTime = 2f, Duration = 2f, StartColor = GloColor.FromRGB(255, 127, 0), EndColor = GloColor.White, SegmentContext = MusicSegments[0] });
            Blocks.Add(new ColorBlock(this, Tracks[1], Tracks[2]) { StartTime = 0.5f, Duration = 2.5f, Color = GloColor.White });
            Blocks.Add(new ColorBlock(this, Tracks[2]) { StartTime = 4f, Duration = 1f, Color = GloColor.FromRGB(0, 255, 120) });

            Blocks.Add(new LoopBlock(this) { StartTime = 4.5f, Duration = 3f, SegmentContext = MusicSegments[0] }
                .AddChild(new ColorBlock(this, Tracks[0]) { StartTime = 0, Duration = 1.25f, Color = GloColor.FromRGB(0, 0, 255), SegmentContext = MusicSegments[0] }, false)
                .AddChild(new ColorBlock(this, Tracks[0]) { StartTime = 1.5f, Duration = 1.5f, Color = GloColor.FromRGB(0, 255, 255), SegmentContext = MusicSegments[0] }, false)
                );
        }
#endif


        public XElement ToXML()
        {
            if (MusicSegments.Count < 1 || !MusicSegments[0].IsReadOnly)
                throw new Exception("the default segment somehow managed to disappear from slot 0");


            return new XElement("timeline",
                new XElement("segments", MusicSegments.Skip(1).Select(s => s.ToXML()).ToArray()),
                new XElement("default-segment", DefaultMusicSegment.GetIndex()),
                new XElement("tracks", Tracks.Select(g => g.ToXML()).ToArray()),
                new XElement("blocks", Blocks.Select(b => b.ToXML()).ToArray())
            );
        }

        public static Timeline FromXML(XElement element)
        {
            Timeline t = new Timeline();

            foreach (var segment in element.ElementOrEmpty("segments").Elements("segment").Select(s => MusicSegment.FromXML(t, s)))
                t.MusicSegments.Add(segment);

            t.DefaultMusicSegment = t.MusicSegments[((int?)element.Element("default-segment")).GetValueOrDefault(0)];

            foreach (var track in element.ElementOrEmpty("tracks").Elements("track").Select(g => Track.FromXML(t, g)))
                t.Tracks.Add(track);
            foreach (var block in element.ElementOrEmpty("blocks").Elements("block").Select(g => Block.FromXML(t, g)))
                t.Blocks.Add(block);

            // TODO XML load validation

            return t;
        }
    }
}
