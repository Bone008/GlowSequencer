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
    private static readonly TransferSettings DEFAULT = new TransferSettings();

    // NOTE: This class should be effectively treated readonly!
    public IDictionary<string, Track> AssignedTracksByDeviceName { get; set; } = new Dictionary<string, Track>();
    public TimeSpan ExportStartTime { get; set; } = TimeSpan.Zero;
    public int MaxConcurrentTransfers { get; set; } = 4;
    public int MaxRetries { get; set; } = 3;

    /// <summary>Removes all assigned references to the given track.</summary>
    public void PurgeTrackReferences(Track track)
    {
        foreach (var pair in AssignedTracksByDeviceName.ToList())
        {
            if (pair.Value == track)
                AssignedTracksByDeviceName[pair.Key] = null;
        }
    }

    public XElement ToXML(XElement element)
    {
        element.Add(
            new XElement("export-start-time", ExportStartTime),
            new XElement("max-concurrent-transfers", MaxConcurrentTransfers),
            new XElement("max-retries", MaxRetries));

        foreach (var pair in AssignedTracksByDeviceName)
        {
            element.Add(new XElement("device",
                new XAttribute("name", pair.Key),
                new XElement("track-reference", pair.Value?.GetIndex())));
        }
        return element;
    }

    public static TransferSettings FromXML(Timeline timeline, XElement element)
    {
        if (element == null)
            return null;

        return new TransferSettings
        {
            ExportStartTime = (TimeSpan?)element.Element("export-start-time") ?? DEFAULT.ExportStartTime,
            MaxConcurrentTransfers = (int?)element.Element("max-concurrent-transfers") ?? DEFAULT.MaxConcurrentTransfers,
            MaxRetries = (int?)element.Element("max-retries") ?? DEFAULT.MaxRetries,

            AssignedTracksByDeviceName = element.Elements("device").ToDictionary(
                deviceElement => (string)deviceElement.Attribute("name"),
                deviceElement => TrackFromXML(timeline, deviceElement)),
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
