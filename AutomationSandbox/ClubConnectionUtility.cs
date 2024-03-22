using System;
using System.Collections.Generic;
using System.Linq;
using LibUsbDotNet;
using LibUsbDotNet.Main;

namespace AutomationSandbox
{
    public class ClubConnectionUtility: IClubConnection
    {
        public List<ConnectedDevice> ListConnectedClubs()
        {
            List<string> ids = GetConnectedClubs();
            Console.WriteLine($"found {ids.Count} clubs");
            List<ConnectedDevice> connectedDevices = new List<ConnectedDevice>();
            foreach (string id in ids)
            {
                UsbDevice device = OpenDevice(id);
                Console.WriteLine($"Reading device: {id}");
                connectedDevices.Add(new ConnectedDevice()
                {
                    deviceId = id,
                    name = ReadNameFromDevice(device),
                    groupName = "not implemented"//ReadGroupNameFromDevice(device),
                });
                device.Close();
                Console.WriteLine($"Closed device: {id}");
            }

            return connectedDevices;
        }

        public string ReadName(string deviceId)
        {
            throw new System.NotImplementedException();
        }

        public void WriteName(string deviceId, string name)
        {
            throw new System.NotImplementedException();
        }

        public string ReadGroupName(string deviceId)
        {
            throw new System.NotImplementedException();
        }

        public void WriteGroupName(string deviceId, string groupName)
        {
            throw new System.NotImplementedException();
        }

        public string ReadProgramName(string deviceId)
        {
            throw new System.NotImplementedException();
        }

        public void WriteProgramName(string deviceId, string programName)
        {
            throw new System.NotImplementedException();
        }

        public byte[] ReadProgram(string deviceId)
        {
            throw new System.NotImplementedException();
        }

        public void WriteProgram(string deviceId, byte[] programData)
        {
            throw new System.NotImplementedException();
        }

        public void Start(string deviceId)
        {
            throw new System.NotImplementedException();
        }

        public void Stop(string deviceId)
        {
            throw new System.NotImplementedException();
        }

        public void SetColor(string deviceId, byte r, byte g, byte b)
        {
            throw new System.NotImplementedException();
        }
        
        
        

        private List<string> GetConnectedClubs()
        {
            return UsbDevice.AllDevices.ToList()
                .Where(x => x.DeviceProperties["DeviceDesc"].ToString() == "Glo-Ultimate")
                .Select(y => y.DeviceProperties["DeviceID"].ToString()).ToList();
        }

        private UsbDevice OpenDevice(string deviceId)
        {
            UsbRegistry usbRegistry = UsbDevice.AllDevices
                .ToList().FirstOrDefault(x => x.DeviceProperties["DeviceID"].ToString() == deviceId);
            if (usbRegistry == null)
            {
                //Console.WriteLine($"Unable to find device with id {deviceId}!");
                return null;
            }
            
            UsbDevice device;
            if(!usbRegistry.Open(out device))
            {
               //Console.WriteLine($"Unable to open device with id {deviceId}!");
                return null;
            }

            return device;
        }

        private string ReadNameFromDevice(UsbDevice device)
        {
            CommunicationUtility.TransferHeader? header = CommunicationUtility.CreateHeader(new byte[]{
                0x04, 0x08, 0x80, 0x00, 0x00, 0x00
            });
            if (header == null)
            {
                return null;
            }

            byte[] nameBytes = CommunicationUtility.ReadContinuously(device, header.Value, 8);
            return System.Text.Encoding.UTF8.GetString(nameBytes);
        }

        private string ReadGroupNameFromDevice(UsbDevice device)
        {
            throw new System.NotImplementedException();
        }
    }
}