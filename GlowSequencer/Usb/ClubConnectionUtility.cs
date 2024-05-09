using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LibUsbDotNet;
using LibUsbDotNet.Main;

#nullable enable

namespace GlowSequencer.Usb
{
    public class ClubConnectionUtility : IClubConnection
    {
        private static readonly TimeSpan MAX_TIME_TO_CACHE_DEVICE_LIST = TimeSpan.FromSeconds(2);

        // would want this but there is some weird error with IsExternalInit :(
        //private record DeviceReference(UsbDevice usbDevice, ConnectedDevice? deviceData) { }
        private class DeviceReference
        {
            /// <summary>The open device handle, required.</summary>
            public readonly UsbDevice usbDevice;

            /// <summary>The metadata read from the device, can be null.</summary>
            public readonly ConnectedDevice? deviceData;

            public DeviceReference(UsbDevice usbDevice, ConnectedDevice? deviceData)
            {
                this.usbDevice = usbDevice;
                this.deviceData = deviceData;
            }

            public void TryClose()
            {
                try { usbDevice.Close(); }
                catch (Exception e) { Console.WriteLine("Failed to close device: " + e); }
            }
        }


        // WARNING: KEEP IN MIND THAT ALL STATE HERE NEEDS TO BE THREAD-SAFE!

        /// <summary>Caches device connections to avoid reopening them, which is slow.</summary>
        private readonly ConcurrentDictionary<string, DeviceReference> _devicesByPortId = new();
        private readonly Stopwatch _lastUsbRegDeviceListStopwatch = new();
        private UsbRegDeviceList? _lastUsbRegDeviceList = null;

        /// <summary>
        /// Wrapper around UsbDevice.AllDevices that caches the results for a short time.
        /// </summary>
        private UsbRegDeviceList GetAllDeviceRegistries()
        {
            if (_lastUsbRegDeviceList == null || _lastUsbRegDeviceListStopwatch.Elapsed < MAX_TIME_TO_CACHE_DEVICE_LIST)
            {
                _lastUsbRegDeviceList = UsbDevice.AllDevices;
                _lastUsbRegDeviceListStopwatch.Restart();
            }
            return _lastUsbRegDeviceList;
        }

        public void DisconnectAll()
        {
            foreach (DeviceReference deviceRef in _devicesByPortId.Values)
            {
                deviceRef.TryClose();
            }
            _devicesByPortId.Clear();
        }

        public List<string> GetConnectedPortIds()
        {
            return GetAllDeviceRegistries()
                .Where(x => x.DeviceProperties["DeviceDesc"].ToString() == "Glo-Ultimate")
                .Select(y => y.DeviceProperties["DeviceID"].ToString())
                .ToList();
        }

        public List<ConnectedDevice> ListConnectedClubs()
        {
            List<string> connectedPortIds = GetConnectedPortIds();
            List<ConnectedDevice> connectedDevices = connectedPortIds.Select(GetConnectedClubByPortId).ToList();

            // Remove disconnected devices from cache. Not sure if closing the handle should be done
            // or has a chance of succeeding, but better safe than sorry.
            foreach (string stalePortId in _devicesByPortId.Keys.Except(connectedPortIds).ToList())
            {
                Console.WriteLine($"Removing disconnected device at port ${stalePortId}.");
                ForgetDevice(stalePortId);
            }

            return connectedDevices;
        }

        public ConnectedDevice GetConnectedClubByPortId(string connectedPortId)
        {
            // Reuse cached connections, but only if their data has been loaded already.
            if (
                _devicesByPortId.TryGetValue(connectedPortId, out DeviceReference deviceRef)
                && deviceRef.deviceData != null
            )
            {
                return deviceRef.deviceData.Value;
            }

            UsbDevice device = OpenDevice(connectedPortId);
            Console.WriteLine($"Reading device: {connectedPortId}");

            string name = ReadNameFromDevice(device);
            string groupName = ReadGroupNameFromDevice(device);
            string programName = ReadProgramNameFromDevice(device);

            ConnectedDevice connectedDevice = new ConnectedDevice()
            {
                connectedPortId = connectedPortId,
                name = name,
                groupName = groupName,
                programName = programName
            };
            _devicesByPortId[connectedPortId] = new DeviceReference(device, connectedDevice);
            return connectedDevice;
        }

        public string ReadName(string connectedPortId)
        {
            try
            {
                UsbDevice device = OpenDevice(connectedPortId);
                string name = ReadNameFromDevice(device);
                return name;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                ForgetDevice(connectedPortId);
                throw;
            }
        }

        public void WriteName(string connectedPortId, string name)
        {
            try
            {
                UsbDevice device = OpenDevice(connectedPortId);
                WriteNameToDevice(device, name);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                ForgetDevice(connectedPortId);
                throw;
            }
        }


        public string ReadGroupName(string connectedPortId)
        {
            try
            {
                UsbDevice device = OpenDevice(connectedPortId);
                string groupName = ReadGroupNameFromDevice(device);
                return groupName;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                ForgetDevice(connectedPortId);
                throw;
            }
        }

        public void WriteGroupName(string connectedPortId, string groupName)
        {
            try
            {
                UsbDevice device = OpenDevice(connectedPortId);
                WriteGroupNameToDevice(device, groupName);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                ForgetDevice(connectedPortId);
                throw;
            }
        }

        public string ReadProgramName(string connectedPortId)
        {
            try
            {
                UsbDevice device = OpenDevice(connectedPortId);
                string programName = ReadProgramNameFromDevice(device);
                return programName;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                ForgetDevice(connectedPortId);
                throw;
            }
        }

        public void WriteProgramName(string connectedPortId, string programName)
        {
            try
            {
                UsbDevice device = OpenDevice(connectedPortId);
                WriteProgramNameToDevice(device, programName);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                ForgetDevice(connectedPortId);
                throw;
            }
        }

        public byte[] ReadProgramAutoDetect(string connectedPortId)
        {
            try
            {
                UsbDevice device = OpenDevice(connectedPortId);
                byte[] programData = ReadProgramFromDeviceAutoDetect(device);
                return programData;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                ForgetDevice(connectedPortId);
                throw;
            }
        }

        public byte[] ReadProgram(string connectedPortId, int amountOfBytes)
        {
            try
            {
                UsbDevice device = OpenDevice(connectedPortId);
                byte[] programData = ReadProgramFromDevice(device, amountOfBytes);
                return programData;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                ForgetDevice(connectedPortId);
                throw;
            }
        }

        public void WriteProgram(string connectedPortId, byte[] programData)
        {
            try
            {
                UsbDevice device = OpenDevice(connectedPortId);
                WriteProgramToDevice(device, programData);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                ForgetDevice(connectedPortId);
                throw;
            }
        }

        public void Start(string connectedPortId)
        {
            try
            {
                UsbDevice device = OpenDevice(connectedPortId);
                StartProgramOnDevice(device);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                ForgetDevice(connectedPortId);
                throw;
            }
        }

        public void StartSync(IEnumerable<string> connectedPortIds)
        {
            List<UsbDevice> devices = new List<UsbDevice>();
            try
            {
                // Note: After device caching, these SHOULD all be fast cache hits, unless something
                // errored out before, so it's still worth keeping as a preprocess step I guess ...
                foreach (string connectedPortId in connectedPortIds)
                {
                    UsbDevice device = OpenDevice(connectedPortId);
                    devices.Add(device);
                }

                foreach (var (connectedPortId, device) in connectedPortIds.Zip(devices, Tuple.Create))
                {
                    // Nested try blocks? let's go!
                    try { StartProgramOnDevice(device); }
                    catch (Exception)
                    {
                        ForgetDevice(connectedPortId);
                        throw;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public void Stop(string connectedPortId)
        {
            SetColor(connectedPortId, 0x00, 0x00, 0x00);
        }

        public void SetColor(string connectedPortId, byte r, byte g, byte b)
        {
            try
            {
                UsbDevice device = OpenDevice(connectedPortId);
                ActivateColorOnDevice(device, r, g, b);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                ForgetDevice(connectedPortId);
                throw;
            }
        }


        private void ForgetDevice(string connectedPortId)
        {
            if (_devicesByPortId.TryRemove(connectedPortId, out DeviceReference deviceRef))
            {
                deviceRef.TryClose();
            }
        }

        private UsbDevice OpenDevice(string connectedPortId)
        {
            // Reuse cached connections, does not matter if they have metadata or not.
            if (_devicesByPortId.TryGetValue(connectedPortId, out DeviceReference deviceRef))
            {
                return deviceRef.usbDevice;
            }

            UsbRegistry? usbRegistry = GetAllDeviceRegistries().FirstOrDefault(x => x.DeviceProperties["DeviceID"].ToString() == connectedPortId);
            if (usbRegistry == null)
            {
                throw new UsbOperationException($"Unable to find device with id {connectedPortId}!");
            }
            if (!usbRegistry.Open(out UsbDevice device))
            {
                throw new UsbOperationException($"Unable to open device with id {connectedPortId}!");
            }

            // Store handle without metadata, which can still be filled in during the next refresh later.
            _devicesByPortId[connectedPortId] = new DeviceReference(device, null);
            return device;
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
                Debug.WriteLine($"WARNING: Truncating name <{name}>! Max 64 bytes allowed.");
                nameBytes = TruncateUtf8Data(nameBytes, 64);
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
                Debug.WriteLine($"WARNING: Truncating group name <{groupName}>! Max 4 bytes allowed.");
                nameBytes = TruncateUtf8Data(nameBytes, 4);
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
                Debug.WriteLine($"WARNING: Truncating program name <{programName}>! Max 64 bytes allowed.");
                nameBytes = TruncateUtf8Data(nameBytes, 64);
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
                inBetweenHeader.Address = header.Address;
                byte[] returnData = CommunicationUtility.WriteReadBulk(device, inBetweenHeader.AsBuffer, 1);
                if (returnData[0] != 0x03)
                {
                    throw new UsbOperationException($"In-between transmission failed. Expected 0x03, got {returnData[0]}");
                }

                byte[] chunk = programData.Skip(i * chunkSize).Take(chunkSize).ToArray();
                int transmissionAmount = (int)Math.Ceiling(chunk.Length / (float)transmissionDataLength);
                CommunicationUtility.WriteContinuously(device, header, chunk, transmissionAmount, new byte[] { 0x02 });
                header.Address += (ushort)chunkSize;
            }
        }

        private static byte[] TruncateUtf8Data(byte[] data, int maxLength)
        {
            Debug.Assert(data.Length > maxLength);
            int safeLength = maxLength;
            // If the first truncated byte is part of a multi-byte sequence ...
            if ((data[maxLength] & 0xC0) == 0x80)
            {
                // remove all bytes whose two highest bits are 10
                // and one more (start of multi-byte sequence - highest bits should be 11)
                while (--safeLength > 0 && (data[safeLength] & 0xC0) == 0x80)
                    ;
            }

            byte[] truncatedBytes = new byte[safeLength];
            Array.Copy(data, truncatedBytes, safeLength);
            return truncatedBytes;
        }
    }
}
