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
        
        public OperationResult<List<ConnectedDevice>?> ListConnectedClubs()
        {
            List<string> connectedPortIds = GetConnectedPortIds();
            Console.WriteLine($"found {connectedPortIds.Count} clubs");
            List<ConnectedDevice> connectedDevices = new List<ConnectedDevice>();
            foreach (string connectedPortId in connectedPortIds)
            {
                OperationResult<ConnectedDevice> r = GetConnectedClubByPortId(connectedPortId);
                if (!r.IsSuccess)
                {
                    return OperationResult<List<ConnectedDevice>>.Fail(r.ErrorMessage);
                }
                connectedDevices.Add(r.Data);
            }

            return OperationResult<List<ConnectedDevice>?>.Success(connectedDevices);
        }

        public OperationResult<ConnectedDevice> GetConnectedClubByPortId(string connectedPortId)
        {
            if(OpenDevice(connectedPortId).IsFailWithNewOperatingResultAndData(out OperationResult<ConnectedDevice> result, out UsbDevice? data))
            {
                return result;
            }
            UsbDevice device = data!;
            
            Console.WriteLine($"Reading device: {connectedPortId}");

            if(ReadNameFromDevice(device).IsFailWithNewOperatingResultAndData(out result, out string dataName))
            {
                return result;
            }
            
            if(ReadGroupNameFromDevice(device).IsFailWithNewOperatingResultAndData(out result, out string dataGroupName))
            {
                return result;
            }
            
            if(ReadProgramNameFromDevice(device).IsFailWithNewOperatingResultAndData(out result, out string programName))
            {
                return result;
            }
            
            ConnectedDevice connectedDevice = new ConnectedDevice()
            {
                connectedPortId = connectedPortId,
                name = dataName,
                groupName = dataGroupName,
                programName = programName,
            };
            device.Close();
            Console.WriteLine($"Closed device: {connectedPortId}");
            return OperationResult<ConnectedDevice>.Success(connectedDevice);
        }

        public OperationResult<string> ReadName(string connectedPortId)
        {
            if(OpenDevice(connectedPortId).IsFailWithNewOperatingResultAndData<string>(out OperationResult<string> result, out UsbDevice? data))
            {
                return result;
            }
            UsbDevice device = data!;
            
            if(ReadNameFromDevice(device).IsFailWithNewOperatingResultAndData(out result, out string name))
            {
                return result;
            }
            
            device.Close();
            return OperationResult<string>.Success(name);
        }

        public OperationResult WriteName(string connectedPortId, string name)
        {
            if(OpenDevice(connectedPortId).IsFailWithNewOperatingResultAndData(out OperationResult result, out UsbDevice? data))
            {
                return result;
            }
            UsbDevice device = data!;
            
            if(WriteNameToDevice(device, name).IsFail(out result))
            {
                return result;
            }
            
            device.Close();
            return OperationResult.Success();
        }


        public OperationResult<string> ReadGroupName(string connectedPortId)
        {
            if(OpenDevice(connectedPortId).IsFailWithNewOperatingResultAndData<string>(out OperationResult<string> result, out UsbDevice? data))
            {
                return result;
            }
            UsbDevice device = data!;
            
            if(ReadGroupNameFromDevice(device).IsFailWithNewOperatingResultAndData(out result, out string name))
            {
                return result;
            }
            
            device.Close();
            return OperationResult<string>.Success(name);
        }

        public OperationResult WriteGroupName(string connectedPortId, string groupName)
        {
            if(OpenDevice(connectedPortId).IsFailWithNewOperatingResultAndData(out OperationResult result, out UsbDevice? data))
            {
                return result;
            }
            UsbDevice device = data!;
            
            if(WriteGroupNameToDevice(device, groupName).IsFail(out result))
            {
                return result;
            }
            
            device.Close();
            return OperationResult.Success();
        }

        public OperationResult<string> ReadProgramName(string connectedPortId)
        {
            if(OpenDevice(connectedPortId).IsFailWithNewOperatingResultAndData<string>(out OperationResult<string> result, out UsbDevice? data))
            {
                return result;
            }
            UsbDevice device = data!;
            
            if(ReadProgramNameFromDevice(device).IsFailWithNewOperatingResultAndData(out result, out string name))
            {
                return result;
            }
            
            device.Close();
            return OperationResult<string>.Success(name);
        }

        public OperationResult WriteProgramName(string connectedPortId, string programName)
        {
            if(OpenDevice(connectedPortId).IsFailWithNewOperatingResultAndData(out OperationResult result, out UsbDevice? data))
            {
                return result;
            }
            UsbDevice device = data!;
            
            if(WriteProgramNameToDevice(device, programName).IsFail(out result))
            {
                return result;
            }
            
            device.Close();
            return OperationResult.Success();
        }

        public OperationResult<byte[]> ReadProgram(string connectedPortId)
        {
            throw new System.NotImplementedException();
        }

        public OperationResult WriteProgram(string connectedPortId, byte[] programData)
        {
            throw new System.NotImplementedException();
        }

        public OperationResult Start(string connectedPortId)
        {
            if(OpenDevice(connectedPortId).IsFailWithNewOperatingResultAndData(out OperationResult result, out UsbDevice? data))
            {
                return result;
            }
            UsbDevice device = data!;
            
            if(StartProgramOnDevice(device).IsFail(out result))
            {
                return result;
            }
            
            device.Close();
            return OperationResult.Success();
        }

        public OperationResult Stop(string connectedPortId)
        {
            OperationResult or = SetColor(connectedPortId, 0x00, 0x00, 0x00);
            if (!or.IsSuccess)
            {
                return OperationResult.Fail("Failed to stop club: " + or.ErrorMessage);
            }
            return or;
        }

        public OperationResult SetColor(string connectedPortId, byte r, byte g, byte b)
        {
            if(OpenDevice(connectedPortId).IsFailWithNewOperatingResultAndData(out OperationResult result, out UsbDevice? data))
            {
                return result;
            }
            UsbDevice device = data!;
            
            if(ActivateColorOnDevice(device, r, g, b).IsFail(out result))
            {
                return result;
            }
            
            device.Close();
            return OperationResult.Success();
        }


        private OperationResult<UsbDevice?> OpenDevice(string connectedPortId)
        {
            UsbRegistry? usbRegistry = UsbDevice.AllDevices
                .ToList().FirstOrDefault(x => x.DeviceProperties["DeviceID"].ToString() == connectedPortId);
            if (usbRegistry == null)
            {
                return OperationResult<UsbDevice>.Fail($"Unable to find device with id {connectedPortId}!");
            }

            if(!usbRegistry.Open(out UsbDevice? device))
            {
               //Console.WriteLine($"Unable to open device with id {connectedPortId}!");
               return OperationResult<UsbDevice>.Fail($"Unable to open device with id {connectedPortId}!");;
            }

            return OperationResult<UsbDevice>.Success(device)!;
        }

        private OperationResult<string> ReadNameFromDevice(UsbDevice device)
        {
            OperationResult<CommunicationUtility.TransferHeader> orHeader = CommunicationUtility.CreateHeader(new byte[]{
                0x04, 0x08, 0x80, 0x00, 0x00, 0x00
            });
            if(orHeader.IsFailWithNewOperatingResult(out OperationResult<string> result))
            {
                return result;
            }
            CommunicationUtility.TransferHeader header = orHeader.Data;

            if(CommunicationUtility.ReadContinuously(device, header, 8).IsFailWithNewOperatingResultAndData(out result, out byte[] nameBytes))
            {
                return result;
            }
            string name = System.Text.Encoding.UTF8.GetString(nameBytes).TrimEnd('\0');;
            return OperationResult<string>.Success(name);
        }
        
        private OperationResult WriteNameToDevice(UsbDevice device, string name)
        {
            //[0x05, 0x08, first_byte at 0x80, 0x00, 0x00, 0x00, ...8 bytes of partial name...]
            OperationResult<CommunicationUtility.TransferHeader> orHeader = CommunicationUtility.CreateHeader(new byte[]{
                0x05, 0x08, 0x80, 0x00, 0x00, 0x00
            });
            if(orHeader.IsFail(out OperationResult result))
            {
                return result;
            }
            
            CommunicationUtility.TransferHeader header = orHeader.Data;
            
            byte[] nameBytes = System.Text.Encoding.UTF8.GetBytes(name);
            if(nameBytes.Length > 64)
            {
                return OperationResult.Fail($"Name <{name}> is too long! Max 64 characters allowed.");
            }
            if(CommunicationUtility.WriteContinuously(device, header, nameBytes, 8, new byte[]{0x05}).IsFail(out result))
            {
                return result;
            }
            return OperationResult.Success();
        }
        
        private OperationResult<string> ReadGroupNameFromDevice(UsbDevice device)
        {
            //[0x04, 0x04, 0x7c, 0x00, 0x00, 0x00]
            OperationResult<CommunicationUtility.TransferHeader> orHeader = CommunicationUtility.CreateHeader(new byte[]{
                0x04, 0x04, 0x7c, 0x00, 0x00, 0x00
            });
            if(orHeader.IsFailWithNewOperatingResult(out OperationResult<string> result))
            {
                return result;
            }
            CommunicationUtility.TransferHeader header = orHeader.Data;
            
            if(CommunicationUtility.ReadContinuously(device, header, 1).IsFailWithNewOperatingResultAndData(out result, out byte[] nameBytes))
            {
                return result;
            }
            string name = System.Text.Encoding.UTF8.GetString(nameBytes).TrimEnd('\0');;
            return OperationResult<string>.Success(name);
        }
        
        private OperationResult WriteGroupNameToDevice(UsbDevice device, string groupName)
        {
            //[0x05, 0x04, 0x7c, 0x00, 0x00, 0x00, ...4 bytes group-name...]
            OperationResult<CommunicationUtility.TransferHeader> orHeader = CommunicationUtility.CreateHeader(new byte[]{
                0x05, 0x04, 0x7c, 0x00, 0x00, 0x00
            });
            if(orHeader.IsFail(out OperationResult result))
            {
                return result;
            }
            
            CommunicationUtility.TransferHeader header = orHeader.Data;
            
            byte[] nameBytes = System.Text.Encoding.UTF8.GetBytes(groupName);
            if(nameBytes.Length > 4)
            {
                return OperationResult.Fail($"Group name <{groupName}> is too long! Max 4 characters allowed.");
            }
            if(CommunicationUtility.WriteContinuously(device, header, nameBytes, 4, new byte[]{0x05}).IsFail(out result))
            {
                return result;
            }
            return OperationResult.Success();
        }
        
        private OperationResult<string> ReadProgramNameFromDevice(UsbDevice device)
        {
            //[0x04, 0x08, first_byte at 0xc0, 0x00, 0x00, 0x00]
            OperationResult<CommunicationUtility.TransferHeader> orHeader = CommunicationUtility.CreateHeader(new byte[]{
                0x04, 0x08, 0xc0, 0x00, 0x00, 0x00
            });
            if(orHeader.IsFailWithNewOperatingResult(out OperationResult<string> result))
            {
                return result;
            }
            CommunicationUtility.TransferHeader header = orHeader.Data;
            
            if(CommunicationUtility.ReadContinuously(device, header, 8).IsFailWithNewOperatingResultAndData(out result, out byte[] nameBytes))
            {
                return result;
            }
            string name = System.Text.Encoding.UTF8.GetString(nameBytes).TrimEnd('\0');;
            return OperationResult<string>.Success(name);
        }
        
        private OperationResult WriteProgramNameToDevice(UsbDevice device, string programName)
        {
            //[0x05, 0x08, first_byte at 0xc0, 0x00, 0x00, 0x00, ...8 bytes of partial name...]
            OperationResult<CommunicationUtility.TransferHeader> orHeader = CommunicationUtility.CreateHeader(new byte[]{
                0x05, 0x08, 0xc0, 0x00, 0x00, 0x00
            });
            if(orHeader.IsFail(out OperationResult result))
            {
                return result;
            }
            
            CommunicationUtility.TransferHeader header = orHeader.Data;
            
            byte[] nameBytes = System.Text.Encoding.UTF8.GetBytes(programName);
            if(nameBytes.Length > 64)
            {
                return OperationResult.Fail($"Program name <{programName}> is too long! Max 64 characters allowed.");
            }
            if(CommunicationUtility.WriteContinuously(device, header, nameBytes, 8, new byte[]{0x05}).IsFail(out result))
            {
                return result;
            }
            return OperationResult.Success();
        }

        private OperationResult ActivateColorOnDevice(UsbDevice device, byte r, byte g, byte b)
        {
            //[0x63, red_hex, green_hex, blue_hex]
            return CommunicationUtility.WriteReadBulk(device, new byte[]{0x63, r, g, b}, 4);
        }

        private OperationResult StartProgramOnDevice(UsbDevice device)
        {
            return CommunicationUtility.WriteControl(device);
        }
    }
}