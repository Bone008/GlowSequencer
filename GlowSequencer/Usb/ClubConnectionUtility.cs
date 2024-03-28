using System;
using System.Collections.Generic;
using System.Linq;
using LibUsbDotNet;
using LibUsbDotNet.Main;

#nullable enable

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

        public List<ConnectedDevice> ListConnectedClubs()
        {
            List<string> connectedPortIds = GetConnectedPortIds();
            Console.WriteLine($"found {connectedPortIds.Count} clubs");
            List<ConnectedDevice> connectedDevices = new List<ConnectedDevice>();
            foreach (string connectedPortId in connectedPortIds)
            {
                ConnectedDevice c = GetConnectedClubByPortId(connectedPortId);
                connectedDevices.Add(c);
            }

            return connectedDevices;
        }

        public ConnectedDevice GetConnectedClubByPortId(string connectedPortId)
        {
            UsbDevice device = null;
            try
            {
                device = OpenDevice(connectedPortId);
                Console.WriteLine($"Reading device: {connectedPortId}");

                string name = ReadNameFromDevice(device);
                string groupName = ReadGroupNameFromDevice(device);
                string programName = ReadProgramNameFromDevice(device);

                ConnectedDevice connectedDevice = new ConnectedDevice()
                {
                    connectedPortId = connectedPortId,
                    name = name,
                    groupName = groupName,
                    programName = programName,
                };
                return connectedDevice;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            finally
            {
                device?.Close();
            }
            
        }

        public string ReadName(string connectedPortId)
        {
            UsbDevice device = null;
            try
            {
                device = OpenDevice(connectedPortId);
                string name = ReadNameFromDevice(device);
                return name;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            finally
            {
                device?.Close();
            }
        }

        public void WriteName(string connectedPortId, string name)
        {
            UsbDevice device = null;
            try
            {
                device = OpenDevice(connectedPortId);
                WriteNameToDevice(device, name);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            finally
            {
                device?.Close();
            }
        }


        public string ReadGroupName(string connectedPortId)
        {
            UsbDevice device = null;
            try
            {
                device = OpenDevice(connectedPortId);
                string groupName = ReadGroupNameFromDevice(device);
                return groupName;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            finally
            {
                device?.Close();
            }
        }

        public void WriteGroupName(string connectedPortId, string groupName)
        {
            UsbDevice device = null;
            try
            {
                device = OpenDevice(connectedPortId);
                WriteGroupNameToDevice(device, groupName);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            finally
            {
                device?.Close();
            }
        }

        public string ReadProgramName(string connectedPortId)
        {
            UsbDevice device = null;
            try
            {
                device = OpenDevice(connectedPortId);
                string programName = ReadProgramNameFromDevice(device);
                return programName;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            finally
            {
                device?.Close();
            }
        }

        public void WriteProgramName(string connectedPortId, string programName)
        {
            UsbDevice device = null;
            try
            {
                device = OpenDevice(connectedPortId);
                WriteProgramNameToDevice(device, programName);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            finally
            {
                device?.Close();
            }
        }

        public byte[] ReadProgramAutoDetect(string connectedPortId)
        {
            UsbDevice device = null;
            try
            {
                device = OpenDevice(connectedPortId);
                byte[] programData = ReadProgramFromDeviceAutoDetect(device);
                return programData;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            finally
            {
                device?.Close();
            }
        }

        public byte[] ReadProgram(string connectedPortId, int amountOfBytes)
        {
            UsbDevice device = null;
            try
            {
                device = OpenDevice(connectedPortId);
                byte[] programData = ReadProgramFromDevice(device, amountOfBytes);
                return programData;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            finally
            {
                device?.Close();
            }
        }

        public void WriteProgram(string connectedPortId, byte[] programData)
        {
            UsbDevice device = null;
            try
            {
                device = OpenDevice(connectedPortId);
                WriteProgramToDevice(device, programData);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            finally
            {
                device?.Close();
            }
        }

        public void Start(string connectedPortId)
        {
            UsbDevice device = null;
            try
            {
                device = OpenDevice(connectedPortId);
                StartProgramOnDevice(device);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            finally
            {
                device?.Close();
            }
        }

        public void Stop(string connectedPortId)
        {
            UsbDevice device = null;
            try
            {
                device = OpenDevice(connectedPortId);
                SetColor(connectedPortId, 0x00, 0x00, 0x00);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            finally
            {
                device?.Close();
            }
        }

        public void SetColor(string connectedPortId, byte r, byte g, byte b)
        {
            UsbDevice device = null;
            try
            {
                device = OpenDevice(connectedPortId);
                ActivateColorOnDevice(device, r, g, b);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            finally
            {
                device?.Close();
            }
        }


        private UsbDevice OpenDevice(string connectedPortId)
        {
            UsbRegistry? usbRegistry = UsbDevice.AllDevices
                    .ToList().FirstOrDefault(x => x.DeviceProperties["DeviceID"].ToString() == connectedPortId);
            if (usbRegistry == null)
            {
                throw new Exception($"Unable to find device with id {connectedPortId}!");
            }

            if (!usbRegistry.Open(out UsbDevice? device))
            {
                throw new Exception($"Unable to open device with id {connectedPortId}!");
            }

            return device!;
        }

        private string ReadNameFromDevice(UsbDevice device)
        {
            CommunicationUtility.TransferHeader header = CommunicationUtility.CreateHeader(new byte[]{
                    0x04, 0x08, 0x80, 0x00, 0x00, 0x00
                });
            var nameBytes = CommunicationUtility.ReadContinuously(device, header, 8);
            string name = System.Text.Encoding.UTF8.GetString(nameBytes).TrimEnd('\0'); ;
            return name;
        }

        private void WriteNameToDevice(UsbDevice device, string name)
        {
            //[0x05, 0x08, first_byte at 0x80, 0x00, 0x00, 0x00, ...8 bytes of partial name...]
            var header = CommunicationUtility.CreateHeader(new byte[]{
                    0x05, 0x08, 0x80, 0x00, 0x00, 0x00
                });

            byte[] nameBytes = System.Text.Encoding.UTF8.GetBytes(name);
            if (nameBytes.Length > 64)
            {
                throw new Exception($"Name <{name}> is too long! Max 64 characters allowed.");
            }

            CommunicationUtility.WriteContinuously(device, header, nameBytes, 8, new byte[] { 0x05 });
        }
        
        private string ReadGroupNameFromDevice(UsbDevice device)
        {
            //[0x04, 0x04, 0x7c, 0x00, 0x00, 0x00]
            var header = CommunicationUtility.CreateHeader(new byte[]{
                0x04, 0x04, 0x7c, 0x00, 0x00, 0x00
            });

            byte[] nameBytes = CommunicationUtility.ReadContinuously(device, header, 1);
            string name = System.Text.Encoding.UTF8.GetString(nameBytes).TrimEnd('\0');
            return name;
        }

        private void WriteGroupNameToDevice(UsbDevice device, string groupName)
        {
            //[0x05, 0x04, 0x7c, 0x00, 0x00, 0x00, ...4 bytes group-name...]
            var header = CommunicationUtility.CreateHeader(new byte[]{
                0x05, 0x04, 0x7c, 0x00, 0x00, 0x00
            });

            byte[] nameBytes = System.Text.Encoding.UTF8.GetBytes(groupName);
            if (nameBytes.Length > 4)
            {
                throw new Exception($"Group name <{groupName}> is too long! Max 4 characters allowed.");
            }
            CommunicationUtility.WriteContinuously(device, header, nameBytes, 4, new byte[] { 0x05 });
        }

        private string ReadProgramNameFromDevice(UsbDevice device)
        {
            //[0x04, 0x08, first_byte at 0xc0, 0x00, 0x00, 0x00]
            var header = CommunicationUtility.CreateHeader(new byte[]{
                0x04, 0x08, 0xc0, 0x00, 0x00, 0x00
            });

            byte[] nameBytes = CommunicationUtility.ReadContinuously(device, header, 8);
            string name = System.Text.Encoding.UTF8.GetString(nameBytes).TrimEnd('\0');
            return name;
        }

        private void WriteProgramNameToDevice(UsbDevice device, string programName)
        {
            //[0x05, 0x08, first_byte at 0xc0, 0x00, 0x00, 0x00, ...8 bytes of partial name...]
            var header = CommunicationUtility.CreateHeader(new byte[]{
                0x05, 0x08, 0xc0, 0x00, 0x00, 0x00
            });

            byte[] nameBytes = System.Text.Encoding.UTF8.GetBytes(programName);
            if (nameBytes.Length > 64)
            {
                throw new Exception($"Program name <{programName}> is too long! Max 64 characters allowed.");
            }
            CommunicationUtility.WriteContinuously(device, header, nameBytes, 8, new byte[] { 0x05 });
        }

        private void ActivateColorOnDevice(UsbDevice device, byte r, byte g, byte b)
        {
            //[0x63, red_hex, green_hex, blue_hex]
            CommunicationUtility.WriteReadBulk(device, new byte[] { 0x63, r, g, b }, 4);
        }

        private void StartProgramOnDevice(UsbDevice device)
        {
            CommunicationUtility.WriteControl(device);
        }

        private byte[] ReadProgramFromDevice(UsbDevice device, int bytesAmount)
        {
            //[0x01, 0x10, 0x00, 0x40, 0x00, 0x00]
            var header = CommunicationUtility.CreateHeader(new byte[]{
                0x01, 0x10, 0x00, 0x40, 0x00, 0x00
            });

            int amount = (int)Math.Ceiling(bytesAmount / (float)header.dataLength);
            byte[] programData = CommunicationUtility.ReadContinuously(device, header, amount);
            return programData.Take(bytesAmount).ToArray();
        }

        private byte[] ReadProgramFromDeviceAutoDetect(UsbDevice device)
        {
            //[0x01, 0x10, 0x00, 0x40, 0x00, 0x00]
            var header = CommunicationUtility.CreateHeader(new byte[]{
                0x01, 0x10, 0x00, 0x40, 0x00, 0x00
            });

            List<byte> retrievedData = new List<byte>();
            bool sequenceFound = false;

            IEnumerable<byte[]> dataEnumerable = CommunicationUtility.ReadContinuouslyEnumerable(device, header);
            foreach (byte[] data in dataEnumerable)
            {
                for (int index = 0; index < data.Length - 1; index++)
                {
                    if (data[index] == 0xff && data[index + 1] == 0xff)
                    {
                        sequenceFound = true;
                        retrievedData.AddRange(data.Take(index + 2)); // Include the 0xff, 0xff sequence
                        break;
                    }
                }
                if (sequenceFound) break;
                else retrievedData.AddRange(data);
            }

            if (!sequenceFound)
            {
                Console.WriteLine("Sequence [0xff, 0xff] not found in the data.");
            }
            return retrievedData.ToArray();
        }

        private void WriteProgramToDevice(UsbDevice device, byte[] programData)
        {
            //[0x02, 0x10, 0x00, 0x40, 0x00, 0x00]
            var header = CommunicationUtility.CreateHeader(new byte[]{
                0x02, 0x10, 0x00, 0x40, 0x00, 0x00
            });

            //[0x03, 0x01, 2 bytes address L.E. starting at 0x00 0x40, 0x00, 0x00]
            var inBetweenHeader = CommunicationUtility.CreateHeader(new byte[]{
                0x03, 0x10, 0x00, 0x40, 0x00, 0x00
            });

            int transmissionDataLength = header.dataLength;
            int consecutiveTransmissions = 4;
            int chunkSize = consecutiveTransmissions * transmissionDataLength;
            int amount = (int)Math.Ceiling(programData.Length / (float)chunkSize);
            for (int i = 0; i < amount; i++)
            {
                //in-between transmission (every 4*16 bytes) - reason unknown but necessary
                byte[] returnData = CommunicationUtility.WriteReadBulk(device, inBetweenHeader.AsBuffer, 1);
                if (returnData[0] != 0x03)
                {
                    throw new Exception($"In-between transmission failed. Expected 0x03, got {returnData[0]}");
                }

                byte[] chunk = programData.Skip(i * chunkSize).Take(chunkSize).ToArray();
                int transmissionAmount = (int)Math.Ceiling(chunk.Length / (float)transmissionDataLength);
                CommunicationUtility.WriteContinuously(device, header, chunk, transmissionAmount, new byte[] { 0x02 });
                header.Address += (ushort)chunkSize;
            }
        }
    }
}
