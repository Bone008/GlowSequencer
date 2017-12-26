using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace GlowSequencer
{
    public class TransferToEquipmentSettings
    {
        public string AerotechAppExePath { get; set; } = "";
        public TimeSpan ExportStartTime { get; set; } = TimeSpan.Zero;
        public bool StartAutomagicallyAfterTransfer { get; set; } = false;
        public bool CloseProgramAfterTransfer { get; set; } = false;

        public bool StartInternalMusicAfterTransfer { get; set; } = false;
        public bool StartExternalMusicAfterTransfer { get; set; } = false;
        public string MusicWindowProcessName { get; set; } = null;
        public string MusicWindowTitle { get; set; } = null;

        // advanced settings
        public int DelayBetweenKeys { get; set; } = 150;
        public int DelayForUpload { get; set; } = 1500;
        public int DelayBeforeStart { get; set; } = 5000;

        public Process GetMusicProcess()
        {
            if (MusicWindowProcessName != null)
            {
                Process[] processes = Process.GetProcessesByName(MusicWindowProcessName);
                // nothing available
                if (processes.Length == 0)
                    return null;
                // exact match
                if (processes.Length == 1)
                    return processes[0];
                // exact match based on window title
                if (MusicWindowTitle != null && processes.Any(p => p.MainWindowTitle == MusicWindowTitle))
                    return processes.First(p => p.MainWindowTitle == MusicWindowTitle);

                // out of luck
            }
            return null;
        }

        public int GetMusicProcessId()
        {
            Process proc = GetMusicProcess();
            return (proc == null ? 0 : proc.Id);
        }

        public void ToXML(XElement elem)
        {
            elem.Add(
                new XElement("aerotech-exe-path", AerotechAppExePath),
                new XElement("export-start-time", ExportStartTime),
                new XElement("start-post-transfer-automagically", StartAutomagicallyAfterTransfer),
                new XElement("close-program-after-transfer", CloseProgramAfterTransfer),
                new XElement("start-internal-music", StartInternalMusicAfterTransfer),
                new XElement("start-music",
                    new XAttribute("enabled", StartExternalMusicAfterTransfer),
                    new XElement("process-name", MusicWindowProcessName),
                    new XElement("window-title", MusicWindowTitle)
                ),
                new XElement("delays",
                    new XElement("between-keys", DelayBetweenKeys),
                    new XElement("upload", DelayForUpload),
                    new XElement("before-start", DelayBeforeStart)
                )
            );
        }

        public void PopulateFromXML(XElement elem)
        {
            AerotechAppExePath = (string)elem.Element("aerotech-exe-path") ?? AerotechAppExePath;
            ExportStartTime = (TimeSpan?)elem.Element("export-start-time") ?? ExportStartTime;
            StartAutomagicallyAfterTransfer = (bool?)elem.Element("start-post-transfer-automagically") ?? StartAutomagicallyAfterTransfer;
            CloseProgramAfterTransfer = (bool?)elem.Element("close-program-after-transfer") ?? CloseProgramAfterTransfer;
            StartInternalMusicAfterTransfer = (bool?)elem.Element("start-internal-music") ?? StartInternalMusicAfterTransfer;

            XElement startMusicElem = elem.Element("start-music") ?? new XElement("start-music");
            StartExternalMusicAfterTransfer = (bool?)startMusicElem.Attribute("enabled") ?? StartExternalMusicAfterTransfer;
            MusicWindowProcessName = (string)startMusicElem.Element("process-name") ?? MusicWindowProcessName;
            MusicWindowTitle = (string)startMusicElem.Element("window-title") ?? MusicWindowTitle;

            XElement delaysElem = elem.Element("delays") ?? new XElement("delays");
            DelayBetweenKeys = (int?)delaysElem.Element("between-keys") ?? DelayBetweenKeys;
            DelayForUpload = (int?)delaysElem.Element("upload") ?? DelayForUpload;
            DelayBeforeStart = (int?)delaysElem.Element("before-start") ?? DelayBeforeStart;
        }

    }
}
