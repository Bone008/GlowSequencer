using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace AutomationSandbox
{
    public class ComProgram
    {
        static void Main(string[] args)
        {
            var usbDevices = GetUSBDevices();

            foreach (var usbDevice in usbDevices)
            {
                Console.WriteLine("Device ID: {0}, PNP Device ID: {1}, Description: {2}",
                    usbDevice.DeviceID, usbDevice.PnpDeviceID, usbDevice.Description);
            }

            Console.Read();
        }

        static List<USBDeviceInfo> GetUSBDevices()
        {
            List<USBDeviceInfo> devices = new List<USBDeviceInfo>();

            ManagementObjectCollection collection;
            using (var searcher = new ManagementObjectSearcher(@"Select * From Win32_USBHub"))
                collection = searcher.Get();

            foreach (var device in collection)
            {
                devices.Add(new USBDeviceInfo(
                (string)device.GetPropertyValue("DeviceID"),
                (string)device.GetPropertyValue("PNPDeviceID"),
                (string)device.GetPropertyValue("Description")
                ));
            }

            collection.Dispose();
            return devices;
        }


        class USBDeviceInfo
        {
            public USBDeviceInfo(string deviceID, string pnpDeviceID, string description)
            {
                this.DeviceID = deviceID;
                this.PnpDeviceID = pnpDeviceID;
                this.Description = description;
            }
            public string DeviceID { get; private set; }
            public string PnpDeviceID { get; private set; }
            public string Description { get; private set; }
        }

        //static void Main(string[] args)
        //{
        //    //ManagementObjectSearcher mos = new ManagementObjectSearcher(
        //    //    "root\\CIMV2",
        //    //    "SELECT * FROM Win32_PnPEntity WHERE ClassGuid=\"{4d36e978-e325-11ce-bfc1-08002be10318}\""
        //    //);

        //    //var result = mos.Get();
        //    //foreach (var entry in result)
        //    //    Console.WriteLine(entry.GetPropertyValue("Name"));

        //    //string[] names = SerialPort.GetPortNames();
        //    SerialPort port = new SerialPort("COM3");

        //    port.BaudRate = 9600;
        //    port.Parity = Parity.None;
        //    port.DataBits = 8;
        //    port.StopBits = StopBits.One;
        //    port.Handshake = Handshake.XOnXOff;

        //    port.ReadTimeout = 500;
        //    port.WriteTimeout = 500;

        //    for (int i = 0; i < 13; i++)
        //    {
        //        port.PortName = "COM" + i;
        //        try
        //        {
        //            port.Open();
        //            Console.WriteLine("COM{0}: success", i);
        //            port.Close();
        //        }
        //        catch(Exception e)
        //        {
        //            Console.WriteLine("COM{0}: {1}", i, e.Message);
        //        }
        //    }

        //    byte[] buf = { 0x64, 255, 0, 0 };
        //    port.Write(buf, 0, buf.Length);

        //    port.Close();
        //}
    }
}
