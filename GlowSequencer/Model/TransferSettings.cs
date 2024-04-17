using GlowSequencer.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace GlowSequencer.Model;

/// <summary>
/// Timeline-specific settings for the new "transfer directly" dialog.
/// These differ from the old-style transmission settings, which did not contian per-timeline data.
/// </summary>
public class TransferSettings
{
    public static readonly GloColor DEFAULT_IDENTIFY_COLOR = GloColor.White;

    public class Device
    {
        public string name;
        public Track assignedTrack;
        public GloColor identifyColor;
    }

    private static readonly TransferSettings DEFAULT = new TransferSettings();
    public static int DEFAULT_MAX_CONCURRENT_TRANSFERS => DEFAULT.MaxConcurrentTransfers;
    public static int DEFAULT_MAX_RETRIES => DEFAULT.MaxRetries;
    public static int DEFAULT_MUSIC_SYSTEM_DELAY_MS => DEFAULT.MusicSystemDelayMs;

    // NOTE: This class should be effectively treated as readonly for UI purposes!
    public IReadOnlyList<Device> DeviceConfigs { get; set; } = new List<Device>(0);
    public TimeSpan ExportStartTime { get; set; } = TimeSpan.Zero;
    public ColorTransformMode ColorMode { get; set; } = ColorTransformMode.None;
    public bool EnableMusic { get; set; } = false;
    public int MusicSystemDelayMs { get; set; } = 50;

    public int MaxConcurrentTransfers { get; set; } = 4;
    public int MaxRetries { get; set; } = 3;

    /// <summary>Removes all assigned references to the given track.</summary>
    public void PurgeTrackReferences(Track track)
    {
        foreach (var config in DeviceConfigs)
        {
            if (config.assignedTrack == track)
                config.assignedTrack = null;
        }
    }

    public XElement ToXML(XElement element)
    {
        element.Add(
            new XElement("export-start-time", ExportStartTime),
            new XElement("color-mode", ColorMode.ToString()),
            new XElement("start-music",
                new XAttribute("enabled", EnableMusic),
                new XElement("system-delay-ms", MusicSystemDelayMs)),
            new XElement("max-concurrent-transfers", MaxConcurrentTransfers),
            new XElement("max-retries", MaxRetries));

        foreach (var config in DeviceConfigs)
        {
            element.Add(new XElement("device",
                new XElement("name", config.name),
                new XElement("track-reference", config.assignedTrack?.GetIndex()),
                new XElement("identify-color", config.identifyColor.ToHexString())
            ));
        }
        return element;
    }

    public static TransferSettings FromXML(Timeline timeline, XElement element)
    {
        if (element == null)
            return null;

        XElement musicElement = element.Element("start-music") ?? new XElement("start-music");
        return new TransferSettings
        {
            ExportStartTime = (TimeSpan?)element.Element("export-start-time") ?? DEFAULT.ExportStartTime,
            ColorMode = element.ElementAsEnum("color-mode", DEFAULT.ColorMode),
            EnableMusic = (bool?)musicElement.Attribute("enabled") ?? DEFAULT.EnableMusic,
            MusicSystemDelayMs = (int?)musicElement.Element("system-delay-ms") ?? DEFAULT.MusicSystemDelayMs,
            MaxConcurrentTransfers = (int?)element.Element("max-concurrent-transfers") ?? DEFAULT.MaxConcurrentTransfers,
            MaxRetries = (int?)element.Element("max-retries") ?? DEFAULT.MaxRetries,

            DeviceConfigs = element.Elements("device").Select(deviceElement => new Device
            {
                name = (string)deviceElement.Element("name") ?? "",
                assignedTrack = TrackFromXML(timeline, deviceElement),
                identifyColor = GloColor.FromHexString(
                    (string)deviceElement.Element("identify-color") ?? DEFAULT_IDENTIFY_COLOR.ToHexString()),
            }).ToList(),
        };
    }

    private static Track TrackFromXML(Timeline timeline, XElement deviceElement)
    {
        string indexString = (string)deviceElement.Element("track-reference") ?? "";
        if (!int.TryParse(indexString, out int index))
            return null;
        return timeline.Tracks[index];
    }
}
