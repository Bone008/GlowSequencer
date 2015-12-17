using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace GlowSequencer
{
    public class TransferToEquipmentSettings
    {
        public string AerotechAppExePath { get; set; }
        public bool StartAutomagicallyAfterTransfer { get; set; }
        public bool CloseProgramAfterTransfer { get; set; }

        public int DelayBetweenKeys { get; set; }
        public int DelayForUpload { get; set; }
        public int DelayBeforeStart { get; set; }

        public TransferToEquipmentSettings()
        {
            // default values
            AerotechAppExePath = "";
            StartAutomagicallyAfterTransfer = true;
            CloseProgramAfterTransfer = true;
            DelayBetweenKeys = 150;
            DelayForUpload = 1500;
            DelayBeforeStart = 5000;
        }

        public void ToXML(XElement elem)
        {
            elem.Add(
                new XElement("aerotech-exe-path", AerotechAppExePath),
                new XElement("start-post-transfer-automagically", StartAutomagicallyAfterTransfer),
                new XElement("close-program-after-transfer", CloseProgramAfterTransfer),
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
            StartAutomagicallyAfterTransfer = (bool?)elem.Element("start-post-transfer-automagically") ?? StartAutomagicallyAfterTransfer;
            CloseProgramAfterTransfer = (bool?)elem.Element("close-program-after-transfer") ?? CloseProgramAfterTransfer;

            XElement delaysElem = elem.Element("delays") ?? new XElement("delays");
            DelayBetweenKeys = (int?)delaysElem.Element("between-keys") ?? DelayBetweenKeys;
            DelayForUpload = (int?)delaysElem.Element("upload") ?? DelayForUpload;
            DelayBeforeStart = (int?)delaysElem.Element("before-start") ?? DelayBeforeStart;
        }
    }
}
