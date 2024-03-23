using System;
using System.Collections.Generic;
using System.Linq;
using LibUsbDotNet;
using LibUsbDotNet.Main;

namespace AutomationSandbox
{
    public class ClubConnectionUtility: IClubConnection
    {
        public List<string> GetConnectedPortIds()
        {
            return UsbDevice.AllDevices.ToList()
                .Where(x => x.DeviceProperties["DeviceDesc"].ToString() == "Glo-Ultimate")
                .Select(y => y.DeviceProperties["DeviceID"].ToString()).ToList();
        }
        
        public List<ConnectedDevice> ListConnectedClubs()
        {
            List<string> connectedPortIds = GetConnectedPortIds();
            Console.WriteLine($"found {connectedPortIds.Count} clubs");
            List<ConnectedDevice> connectedDevices = new List<ConnectedDevice>();
            foreach (string connectedPortId in connectedPortIds)
            {
                connectedDevices.Add(GetConnectedClubByPortId(connectedPortId));
            }

            return connectedDevices;
        }

        public ConnectedDevice GetConnectedClubByPortId(string connectedPortId)
        {
            UsbDevice device = OpenDevice(connectedPortId);
            Console.WriteLine($"Reading device: {connectedPortId}");
            ConnectedDevice connectedDevice = new ConnectedDevice()
            {
                connectedPortId = connectedPortId,
                name = ReadNameFromDevice(device),
                groupName = "not implemented"//ReadGroupNameFromDevice(device),
            };
            device.Close();
            Console.WriteLine($"Closed device: {connectedPortId}");
            return connectedDevice;
        }

        public string ReadName(string connectedPortId)
        {
            UsbDevice device = OpenDevice(connectedPortId);
            string name = ReadNameFromDevice(device);
            device.Close();
            return name;
        }

        public void WriteName(string connectedPortId, string name)
        {
            throw new System.NotImplementedException();
        }

        public string ReadGroupName(string connectedPortId)
        {
            throw new System.NotImplementedException();
        }

        public void WriteGroupName(string connectedPortId, string groupName)
        {
            throw new System.NotImplementedException();
        }

        public string ReadProgramName(string connectedPortId)
        {
            throw new System.NotImplementedException();
        }

        public void WriteProgramName(string connectedPortId, string programName)
        {
            throw new System.NotImplementedException();
        }

        public byte[] ReadProgram(string connectedPortId)
        {
            throw new System.NotImplementedException();
        }

        public void WriteProgram(string connectedPortId, byte[] programData)
        {
            throw new System.NotImplementedException();
        }

        public void Start(string connectedPortId)
        {
            throw new System.NotImplementedException();
        }

        public void Stop(string connectedPortId)
        {
            throw new System.NotImplementedException();
        }

        public void SetColor(string connectedPortId, byte r, byte g, byte b)
        {
            throw new System.NotImplementedException();
        }
        
        
        


        private UsbDevice OpenDevice(string connectedPortId)
        {
            UsbRegistry usbRegistry = UsbDevice.AllDevices
                .ToList().FirstOrDefault(x => x.DeviceProperties["DeviceID"].ToString() == connectedPortId);
            if (usbRegistry == null)
            {
                //Console.WriteLine($"Unable to find device with id {connectedPortId}!");
                return null;
            }
            
            UsbDevice device;
            if(!usbRegistry.Open(out device))
            {
               //Console.WriteLine($"Unable to open device with id {connectedPortId}!");
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