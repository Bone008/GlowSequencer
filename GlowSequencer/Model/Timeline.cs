using GlowSequencer.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace GlowSequencer.Model
{
    public class Timeline : Observable
    {
        private string _musicFileName = null;
        private MusicSegment _defaultMusicSegment;

        public ObservableCollection<Block> Blocks { get; private set; }
        public ObservableCollection<Track> Tracks { get; private set; }
        public ObservableCollection<Note> Notes { get; private set; }

        /// <summary>Name of an audio file containing music. Should be a relative path if possible.
        /// Can be null if no in-app music is desired.</summary>
        public string MusicFileName { get { return _musicFileName; } set { SetProperty(ref _musicFileName, value); } }
        public ObservableCollection<MusicSegment> MusicSegments { get; private set; }

        /// <summary>The saved configuration of the "Transfer directly" dialog, or null if none are saved.</summary>
        public TransferSettings TransferSettings { get; set; } = null;

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
            Notes = new ObservableCollection<Note>();
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

            Notes.Add(new Note { Label = "First marker", Time = 0.5f });
            Notes.Add(new Note { Label = "Second marker", Time = 1.446f });
            Notes.Add(new Note { Label = "Third marker", Time = 3.0f, Description = "This one is super fancy and has a description.\r\nThe possibilities are endless!" });
            Notes.Add(new Note { Time = 4f });
        }
#endif


        public XElement ToXML(string relativePathBase = null)
        {
            if (MusicSegments.Count < 1 || !MusicSegments[0].IsReadOnly)
                throw new Exception("the default segment somehow managed to disappear from slot 0");

            return new XElement("timeline",
                MusicFileToXML(relativePathBase),
                new XElement("segments", MusicSegments.Skip(1).Select(s => s.ToXML()).ToArray()),
                new XElement("default-segment", DefaultMusicSegment.GetIndex()),
                new XElement("tracks", Tracks.Select(g => g.ToXML()).ToArray()),
                new XElement("blocks", Blocks.Select(b => b.ToXML()).ToArray()),
                new XElement("notes", Notes.Select(n => n.ToXML()).ToArray()),
                TransferSettings?.ToXML(new XElement("transfer-settings"))
            );
        }

        private XElement MusicFileToXML(string relativePathBase)
        {
            return new XElement("music-file",
                new XElement("absolute", MusicFileName),
                new XElement("relative", ConvertToRelative(MusicFileName, relativePathBase))
            );
        }

        public static Timeline FromXML(XElement element, string relativePathBase)
        {
            Timeline t = new Timeline();

            t.MusicFileName = MusicFileFromXML(element.Element("music-file"), relativePathBase);

            foreach (var segment in element.ElementOrEmpty("segments").Elements("segment").Select(s => MusicSegment.FromXML(t, s)))
                t.MusicSegments.Add(segment);

            t.DefaultMusicSegment = t.MusicSegments[((int?)element.Element("default-segment")).GetValueOrDefault(0)];

            foreach (var track in element.ElementOrEmpty("tracks").Elements("track").Select(g => Track.FromXML(t, g)))
                t.Tracks.Add(track);
            foreach (var block in element.ElementOrEmpty("blocks").Elements("block").Select(g => Block.FromXML(t, g)))
                t.Blocks.Add(block);
            foreach (var note in element.ElementOrEmpty("notes").Elements("note").Select(n => Note.FromXML(t, n)))
                t.Notes.Add(note);

            t.TransferSettings = TransferSettings.FromXML(t, element.Element("transfer-settings"));

            // TODO XML load validation

            return t;
        }

        private static string MusicFileFromXML(XElement element, string relativePathBase)
        {
            if (element == null) return null;
            string absolute = (string)element.Element("absolute");
            string relative = (string)element.Element("relative");

            // normalize null
            if (string.IsNullOrWhiteSpace(absolute)) absolute = null;
            if (string.IsNullOrWhiteSpace(absolute)) relative = null;

            // If the absolute version is found, great, take it.
            if (absolute != null && File.Exists(absolute))
                return absolute;
            // Otherwise, try to resolve the relative version and use that.
            else if (relative != null)
                return ConvertToAbsolute(relative, relativePathBase);
            // If we don't have a relative version, return the absolute one,
            // which will return in an error dialog when the file is loaded.
            else
                return absolute;
        }

        private static string ConvertToRelative(string path, string basePath)
        {
            if (basePath == null) return path;
            if (path == null) return null;

            // Normalize paths.
            path = Path.GetFullPath(path);
            basePath = Path.GetFullPath(basePath);
            if (!basePath.EndsWith(Path.DirectorySeparatorChar.ToString())) basePath += Path.DirectorySeparatorChar;

            // Calculate relative path. See https://stackoverflow.com/a/703292.
            Uri pathUri = new Uri(path);
            Uri basePathUri = new Uri(basePath);
            return Uri.UnescapeDataString(
                basePathUri.MakeRelativeUri(pathUri).ToString().Replace('/', Path.DirectorySeparatorChar));
        }

        private static string ConvertToAbsolute(string path, string basePath)
        {
            if (basePath == null) return path;
            if (path == null) return null;

            // Don't change if path is already absolute.
            if (Path.IsPathRooted(path)) return path;

            // Normalize basePath.
            basePath = Path.GetFullPath(basePath);
            if (!basePath.EndsWith(Path.DirectorySeparatorChar.ToString())) basePath += Path.DirectorySeparatorChar;

            return Path.GetFullPath(basePath + path);
        }
    }
}
