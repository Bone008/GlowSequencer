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

        public TransferToEquipmentSettings()
        {
            AerotechAppExePath = "";
            StartAutomagicallyAfterTransfer = true;
            CloseProgramAfterTransfer = true;
        }

        public void ToXML(XElement elem)
        {
            elem.Add(
                new XElement("aerotech-exe-path", AerotechAppExePath),
                new XElement("start-post-transfer-automagically", StartAutomagicallyAfterTransfer),
                new XElement("close-program-after-transfer", CloseProgramAfterTransfer)
            );
        }

        public void PopulateFromXML(XElement elem)
        {
            AerotechAppExePath = (string)elem.Element("aerotech-exe-path") ?? AerotechAppExePath;
            StartAutomagicallyAfterTransfer = (bool?)elem.Element("start-post-transfer-automagically") ?? StartAutomagicallyAfterTransfer;
            CloseProgramAfterTransfer = (bool?)elem.Element("close-program-after-transfer") ?? CloseProgramAfterTransfer;
        }
    }
}
