using System;
using System.Collections.Generic;
using System.Linq;
using LibUsbDotNet;
using LibUsbDotNet.Main;

namespace GlowSequencer.Usb
{
    public class ClubConnectionUtility : IClubConnection
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
            if (OpenDevice(connectedPortId).IsFailWithNewOperatingResultAndData(out OperationResult<ConnectedDevice> result, out UsbDevice? data))
            {
                return result;
            }
            UsbDevice device = data!;

            Console.WriteLine($"Reading device: {connectedPortId}");

            if (ReadNameFromDevice(device).IsFailWithNewOperatingResultAndData(out result, out string dataName))
            {
                return result;
            }

            if (ReadGroupNameFromDevice(device).IsFailWithNewOperatingResultAndData(out result, out string dataGroupName))
            {
                return result;
            }

            if (ReadProgramNameFromDevice(device).IsFailWithNewOperatingResultAndData(out result, out string programName))
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
            if (OpenDevice(connectedPortId).IsFailWithNewOperatingResultAndData<string>(out OperationResult<string> result, out UsbDevice? data))
            {
                return result;
            }
            UsbDevice device = data!;

            if (ReadNameFromDevice(device).IsFailWithNewOperatingResultAndData(out result, out string name))
            {
                return result;
            }

            device.Close();
            return OperationResult<string>.Success(name);
        }

        public OperationResult WriteName(string connectedPortId, string name)
        {
            if (OpenDevice(connectedPortId).IsFailWithNewOperatingResultAndData(out OperationResult result, out UsbDevice? data))
            {
                return result;
            }
            UsbDevice device = data!;

            if (WriteNameToDevice(device, name).IsFail(out result))
            {
                return result;
            }

            device.Close();
            return OperationResult.Success();
        }


        public OperationResult<string> ReadGroupName(string connectedPortId)
        {
            if (OpenDevice(connectedPortId).IsFailWithNewOperatingResultAndData<string>(out OperationResult<string> result, out UsbDevice? data))
            {
                return result;
            }
            UsbDevice device = data!;

            if (ReadGroupNameFromDevice(device).IsFailWithNewOperatingResultAndData(out result, out string name))
            {
                return result;
            }

            device.Close();
            return OperationResult<string>.Success(name);
        }

        public OperationResult WriteGroupName(string connectedPortId, string groupName)
        {
            if (OpenDevice(connectedPortId).IsFailWithNewOperatingResultAndData(out OperationResult result, out UsbDevice? data))
            {
                return result;
            }
            UsbDevice device = data!;

            if (WriteGroupNameToDevice(device, groupName).IsFail(out result))
            {
                return result;
            }

            device.Close();
            return OperationResult.Success();
        }

        public OperationResult<string> ReadProgramName(string connectedPortId)
        {
            if (OpenDevice(connectedPortId).IsFailWithNewOperatingResultAndData<string>(out OperationResult<string> result, out UsbDevice? data))
            {
                return result;
            }
            UsbDevice device = data!;

            if (ReadProgramNameFromDevice(device).IsFailWithNewOperatingResultAndData(out result, out string name))
            {
                return result;
            }

            device.Close();
            return OperationResult<string>.Success(name);
        }

        public OperationResult WriteProgramName(string connectedPortId, string programName)
        {
            if (OpenDevice(connectedPortId).IsFailWithNewOperatingResultAndData(out OperationResult result, out UsbDevice? data))
            {
                return result;
            }
            UsbDevice device = data!;

            if (WriteProgramNameToDevice(device, programName).IsFail(out result))
            {
                return result;
            }

            device.Close();
            return OperationResult.Success();
        }

        public OperationResult<byte[]> ReadProgramAutoDetect(string connectedPortId)
        {
            if (OpenDevice(connectedPortId).IsFailWithNewOperatingResultAndData<byte[]>(out OperationResult<byte[]> result, out UsbDevice? data))
            {
                return result;
            }
            UsbDevice device = data!;

            if (ReadProgramFromDeviceAutoDetect(device).IsFailWithNewOperatingResultAndData(out result, out byte[] programData))
            {
                return result;
            }

            device.Close();
            return OperationResult<byte[]>.Success(programData);
        }

        public OperationResult<byte[]> ReadProgram(string connectedPortId, int amountOfBytes)
        {
            if (OpenDevice(connectedPortId).IsFailWithNewOperatingResultAndData<byte[]>(out OperationResult<byte[]> result, out UsbDevice? data))
            {
                return result;
            }
            UsbDevice device = data!;

            if (ReadProgramFromDevice(device, amountOfBytes).IsFailWithNewOperatingResultAndData(out result, out byte[] programData))
            {
                return result;
            }

            device.Close();
            return OperationResult<byte[]>.Success(programData);
        }

        public OperationResult WriteProgram(string connectedPortId, byte[] programData)
        {
            if (OpenDevice(connectedPortId).IsFailWithNewOperatingResultAndData(out OperationResult result, out UsbDevice? data))
            {
                return result;
            }
            UsbDevice device = data!;

            if (WriteProgramToDevice(device, programData).IsFail(out result))
            {
                return result;
            }

            device.Close();
            return OperationResult.Success();
        }

        public OperationResult Start(string connectedPortId)
        {
            if (OpenDevice(connectedPortId).IsFailWithNewOperatingResultAndData(out OperationResult result, out UsbDevice? data))
            {
                return result;
            }
            UsbDevice device = data!;

            if (StartProgramOnDevice(device).IsFail(out result))
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
            if (OpenDevice(connectedPortId).IsFailWithNewOperatingResultAndData(out OperationResult result, out UsbDevice? data))
            {
                return result;
            }
            UsbDevice device = data!;

            if (ActivateColorOnDevice(device, r, g, b).IsFail(out result))
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

            if (!usbRegistry.Open(out UsbDevice? device))
            {
                //Console.WriteLine($"Unable to open device with id {connectedPortId}!");
                return OperationResult<UsbDevice>.Fail($"Unable to open device with id {connectedPortId}!"); ;
            }

            return OperationResult<UsbDevice>.Success(device)!;
        }

        private OperationResult<string> ReadNameFromDevice(UsbDevice device)
        {
            OperationResult<CommunicationUtility.TransferHeader> orHeader = CommunicationUtility.CreateHeader(new byte[]{
                    0x04, 0x08, 0x80, 0x00, 0x00, 0x00
                });
            if (orHeader.IsFailWithNewOperatingResult(out OperationResult<string> result))
            {
                return result;
            }
            CommunicationUtility.TransferHeader header = orHeader.Data;

            if (CommunicationUtility.ReadContinuously(device, header, 8).IsFailWithNewOperatingResultAndData(out result, out byte[] nameBytes))
            {
                return result;
            }
            string name = System.Text.Encoding.UTF8.GetString(nameBytes).TrimEnd('\0'); ;
            return OperationResult<string>.Success(name);
        }

        private OperationResult WriteNameToDevice(UsbDevice device, string name)
        {
            //[0x05, 0x08, first_byte at 0x80, 0x00, 0x00, 0x00, ...8 bytes of partial name...]
            OperationResult<CommunicationUtility.TransferHeader> orHeader = CommunicationUtility.CreateHeader(new byte[]{
                    0x05, 0x08, 0x80, 0x00, 0x00, 0x00
                });
            if (orHeader.IsFail(out OperationResult result))
            {
                return result;
            }

            CommunicationUtility.TransferHeader header = orHeader.Data;

            byte[] nameBytes = System.Text.Encoding.UTF8.GetBytes(name);
            if (nameBytes.Length > 64)
            {
                return OperationResult.Fail($"Name <{name}> is too long! Max 64 characters allowed.");
            }
            if (CommunicationUtility.WriteContinuously(device, header, nameBytes, 8, new byte[] { 0x05 }).IsFail(out result))
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
            if (orHeader.IsFailWithNewOperatingResult(out OperationResult<string> result))
            {
                return result;
            }
            CommunicationUtility.TransferHeader header = orHeader.Data;

            if (CommunicationUtility.ReadContinuously(device, header, 1).IsFailWithNewOperatingResultAndData(out result, out byte[] nameBytes))
            {
                return result;
            }
            string name = System.Text.Encoding.UTF8.GetString(nameBytes).TrimEnd('\0'); ;
            return OperationResult<string>.Success(name);
        }

        private OperationResult WriteGroupNameToDevice(UsbDevice device, string groupName)
        {
            //[0x05, 0x04, 0x7c, 0x00, 0x00, 0x00, ...4 bytes group-name...]
            OperationResult<CommunicationUtility.TransferHeader> orHeader = CommunicationUtility.CreateHeader(new byte[]{
                    0x05, 0x04, 0x7c, 0x00, 0x00, 0x00
                });
            if (orHeader.IsFail(out OperationResult result))
            {
                return result;
            }

            CommunicationUtility.TransferHeader header = orHeader.Data;

            byte[] nameBytes = System.Text.Encoding.UTF8.GetBytes(groupName);
            if (nameBytes.Length > 4)
            {
                return OperationResult.Fail($"Group name <{groupName}> is too long! Max 4 characters allowed.");
            }
            if (CommunicationUtility.WriteContinuously(device, header, nameBytes, 4, new byte[] { 0x05 }).IsFail(out result))
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
            if (orHeader.IsFailWithNewOperatingResult(out OperationResult<string> result))
            {
                return result;
            }
            CommunicationUtility.TransferHeader header = orHeader.Data;

            if (CommunicationUtility.ReadContinuously(device, header, 8).IsFailWithNewOperatingResultAndData(out result, out byte[] nameBytes))
            {
                return result;
            }
            string name = System.Text.Encoding.UTF8.GetString(nameBytes).TrimEnd('\0'); ;
            return OperationResult<string>.Success(name);
        }

        private OperationResult WriteProgramNameToDevice(UsbDevice device, string programName)
        {
            //[0x05, 0x08, first_byte at 0xc0, 0x00, 0x00, 0x00, ...8 bytes of partial name...]
            OperationResult<CommunicationUtility.TransferHeader> orHeader = CommunicationUtility.CreateHeader(new byte[]{
                    0x05, 0x08, 0xc0, 0x00, 0x00, 0x00
                });
            if (orHeader.IsFail(out OperationResult result))
            {
                return result;
            }

            CommunicationUtility.TransferHeader header = orHeader.Data;

            byte[] nameBytes = System.Text.Encoding.UTF8.GetBytes(programName);
            if (nameBytes.Length > 64)
            {
                return OperationResult.Fail($"Program name <{programName}> is too long! Max 64 characters allowed.");
            }
            if (CommunicationUtility.WriteContinuously(device, header, nameBytes, 8, new byte[] { 0x05 }).IsFail(out result))
            {
                return result;
            }
            return OperationResult.Success();
        }

        private OperationResult ActivateColorOnDevice(UsbDevice device, byte r, byte g, byte b)
        {
            //[0x63, red_hex, green_hex, blue_hex]
            return CommunicationUtility.WriteReadBulk(device, new byte[] { 0x63, r, g, b }, 4);
        }

        private OperationResult StartProgramOnDevice(UsbDevice device)
        {
            return CommunicationUtility.WriteControl(device);
        }

        private OperationResult<byte[]> ReadProgramFromDevice(UsbDevice device, int bytesAmount)
        {
            //[0x01, 0x10, 2 bytes address L.E. starting at 0x00 0x40, 0x00, 0x00]
            OperationResult<CommunicationUtility.TransferHeader> orHeader = CommunicationUtility.CreateHeader(new byte[]{
                    0x01, 0x10, 0x00, 0x40, 0x00, 0x00
                });
            if (orHeader.IsFailWithNewOperatingResult(out OperationResult<byte[]> result))
            {
                return result;
            }
            CommunicationUtility.TransferHeader header = orHeader.Data;
            int amount = (int)Math.Ceiling(bytesAmount / (float)header.dataLength);
            if (CommunicationUtility.ReadContinuously(device, header, amount).IsFailWithNewOperatingResultAndData(out result, out byte[] programData))
            {
                return result;
            }
            return OperationResult<byte[]>.Success(programData.Take(bytesAmount).ToArray());
        }

        private OperationResult<byte[]> ReadProgramFromDeviceAutoDetect(UsbDevice device)
        {
            //[0x01, 0x10, 2 bytes address L.E. starting at 0x00 0x40, 0x00, 0x00]
            OperationResult<CommunicationUtility.TransferHeader> orHeader = CommunicationUtility.CreateHeader(new byte[]{
                    0x01, 0x10, 0x00, 0x40, 0x00, 0x00
                });
            if (orHeader.IsFailWithNewOperatingResult(out OperationResult<byte[]> result))
            {
                return result;
            }
            CommunicationUtility.TransferHeader header = orHeader.Data;
            List<byte> retrievedData = new List<byte>();
            bool sequenceFound = false;

            IEnumerable<byte[]> dataEnumerable = CommunicationUtility.ReadContinuouslyEnumerable(device, header);
            foreach (byte[] data in dataEnumerable)
            {
                int index = 0;
                // Detect [0xff, 0xff] sequence
                for (; index < data.Length - 1; index++)
                {
                    if (data[index] == 0xff && data[index + 1] == 0xff)
                    {
                        sequenceFound = true;
                        break;
                    }
                }
                retrievedData.AddRange(data.Take(index));

                if (sequenceFound)
                {
                    retrievedData.AddRange(new byte[] { 0xff, 0xff });
                    break;
                }
            }

            if (sequenceFound)
            {
                Console.WriteLine("Sequence [0xff, 0xff] found, stopped processing.");
            }
            else
            {
                Console.WriteLine("Sequence [0xff, 0xff] not found in the data.");
            }

            return OperationResult<byte[]>.Success(retrievedData.ToArray());
        }

        private OperationResult WriteProgramToDevice(UsbDevice device, byte[] programData)
        {
            //[0x02, 0x10, 2 bytes address L.E. starting at 0x00 0x40, 0x00, 0x00, ...programData...]
            OperationResult<CommunicationUtility.TransferHeader> orHeader = CommunicationUtility.CreateHeader(new byte[]{
                    0x02, 0x10, 0x00, 0x40, 0x00, 0x00
                });
            if (orHeader.IsFail(out OperationResult result))
            {
                return result;
            }
            CommunicationUtility.TransferHeader header = orHeader.Data;

            //[0x03, 0x01, 2 bytes address L.E. starting at 0x00 0x40, 0x00, 0x00]
            OperationResult<CommunicationUtility.TransferHeader> orInBetweenHeader = CommunicationUtility.CreateHeader(new byte[]{
                    0x03, 0x10, 0x00, 0x40, 0x00, 0x00
                });
            if (orHeader.IsFail(out result))
            {
                return result;
            }
            CommunicationUtility.TransferHeader InBetweenHeader = orInBetweenHeader.Data;

            //split the programData into chunks of 4*16 bytes
            int transmissionDataLength = header.dataLength;
            int consecutiveTransmissions = 4;
            int chunkSize = consecutiveTransmissions * transmissionDataLength;
            int amount = (int)Math.Ceiling(programData.Length / (float)chunkSize);
            for (int i = 0; i < amount; i++)
            {
                //in-between transmission (every 4*16 bytes) - reason unknown but necessary
                InBetweenHeader.Address = header.Address;
                if (CommunicationUtility.WriteReadBulk(device, InBetweenHeader.AsBuffer, 1).IsFailWithNewOperatingResultAndData(out result, out byte[]? returnData))
                {
                    return result;
                }
                if (returnData![0] != 0x03)
                {
                    return OperationResult.Fail($"In-between transmission failed. Expected 0x03, got {returnData[0]}");
                }

                //program transmission
                byte[] chunk = programData.Skip(i * chunkSize).Take(chunkSize).ToArray();
                int transmissionAmount = (int)Math.Ceiling(chunk.Length / (float)transmissionDataLength);
                if (CommunicationUtility.WriteContinuously(device, header, chunk, transmissionAmount, new byte[] { 0x02 }).IsFail(out result))
                {
                    return result;
                }
                header.Address += (ushort)chunkSize;
            }
            return OperationResult.Success();
        }
    }
}
