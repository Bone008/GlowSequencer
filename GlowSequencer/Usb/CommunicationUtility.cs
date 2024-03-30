//#define SIMULATE_RW

using System;
using System.Collections.Generic;
using LibUsbDotNet;
using LibUsbDotNet.Main;

#nullable enable

namespace GlowSequencer.Usb
{
    public static class CommunicationUtility
    {
        public struct TransferHeader
        {
            public byte command;
            public byte dataLength;
            private byte addressLe0; // LSB
            private byte addressLe1; // MSB
            public byte unknown1;//0x00
            public byte unknown2;//0x00

            public ushort Address
            {
                get
                {
                    return (ushort)((addressLe1 << 8) | addressLe0);
                }
                set
                {
                    if(command == 0x02 && value < 16384)
                    {
                        throw new ArgumentOutOfRangeException("Address must be greater than 0x0040 (16384) for command 0x02");
                    }
                    addressLe0 = (byte)(value & 0xFF); 
                    addressLe1 = (byte)((value >> 8) & 0xFF);
                }
            }

            public byte[] AsBuffer
            {
                get
                {
                    byte[] buffer = new byte[] { command, dataLength, addressLe0, addressLe1, unknown1, unknown2 };
                    return buffer;
                }
            }
        }
        public static TransferHeader CreateHeader(byte[] headerData)
        {
            if (headerData.Length < 4)
            {
                throw new ArgumentException("Header data is too short!");
            }

            TransferHeader header = new TransferHeader()
            {
                command = headerData[0],
                dataLength = headerData[1],
                unknown1 = 0x00,
                unknown2 = 0x00,
            };
            header.Address = (ushort)((headerData[3] << 8) | headerData[2]);

            if (headerData.Length == 4)
            {
                return header;
            }

            if (headerData.Length == 6)
            {
                header.unknown1 = headerData[4];
                header.unknown2 = headerData[5];
                return header;
            }
            throw new ArgumentException("Header data length does not match!");
        }

        public static byte[] ReadContinuously(UsbDevice device, TransferHeader header, int amount)
        {
            int startAddress = header.Address;
            byte[] result = new byte[header.dataLength * amount];
            for (int i = 0; i < amount; i++)
            {
                header.Address = (ushort)(startAddress + (i * header.dataLength));
                byte[] headerBuffer = header.AsBuffer;
                byte[] readBuffer = WriteReadBulk(device, headerBuffer, header.dataLength + 6);

                Array.Copy(readBuffer, 6, result, (i * header.dataLength), header.dataLength);
            }

            return result;
        }

        public static IEnumerable<byte[]> ReadContinuouslyEnumerable(UsbDevice device, TransferHeader header)
        {
            int startAddress = header.Address;
            int i = 0;
            for (int j = 0; j < 65536 / header.dataLength; j++)
            {
                header.Address = (ushort)(startAddress + (i * header.dataLength));
                byte[] headerBuffer = header.AsBuffer;
                byte[] readBuffer;
                try
                {
                    readBuffer = WriteReadBulk(device, headerBuffer, header.dataLength + 6);
                }
                catch (UsbOperationException e)
                {
                    throw new UsbOperationException($"Continuous Read failed in block {i} - {e.Message}");
                }
;
                byte[] chunk = new byte[header.dataLength];
                Array.Copy(readBuffer, 6, chunk, 0, header.dataLength);

                yield return chunk;
                i++;
            }
        }


        public static void WriteContinuously(UsbDevice device, TransferHeader header, byte[] data, int amount, byte[] expectedReturn)
        {
            //pad data to match header.dataLength * amount
            if (data.Length < header.dataLength * amount)
            {
                byte[] newData = new byte[header.dataLength * amount];
                Array.Copy(data, newData, data.Length);
                data = newData;
            }

            int startAddress = header.Address;
            for (int i = 0; i < amount; i++)
            {
                header.Address = (ushort)(startAddress + (i * header.dataLength));
                byte[] headerBuffer = header.AsBuffer;
                byte[] writeBuffer = new byte[headerBuffer.Length + header.dataLength];
                Array.Copy(headerBuffer, writeBuffer, headerBuffer.Length);
                Array.Copy(data, (i * header.dataLength), writeBuffer, headerBuffer.Length, header.dataLength);
                //Console.WriteLine($"Writing: {BitConverter.ToString(writeBuffer)}");
                byte[] readBuffer = WriteReadBulk(device, writeBuffer, expectedReturn.Length);
                
                if (expectedReturn.Length > 0)
                {
                    for (int j = 0; j < expectedReturn.Length; j++)
                    {
                        if (readBuffer[j] != expectedReturn[j])
                        {
                            throw new UsbOperationException($"Expected return value not found! Expected: {BitConverter.ToString(expectedReturn)}, found: {BitConverter.ToString(readBuffer)}");
                        }
                    }
                }
            }
        }

        public static byte[] WriteReadBulk(UsbDevice device, byte[] writeBuffer, int readBufferSize)
        {
#if SIMULATE_RW
            Console.WriteLine($"WriteReadBulk sim: {BitConverter.ToString(writeBuffer)}");
            byte[] simulatedResultBuffer = new byte[readBufferSize];
            //copy writeBuffer to simulate read - pad or truncate if necessary
            Array.Copy(writeBuffer, simulatedResultBuffer, Math.Min(writeBuffer.Length, readBufferSize));
            Console.WriteLine($"write_read bulk sim result: {BitConverter.ToString(simulatedResultBuffer)}");
            return simulatedResultBuffer;
#else
            WriteBulk(device, writeBuffer);
            var result = ReadBulk(device, readBufferSize);
            return result;
#endif
        }

        private static void WriteBulk(UsbDevice device, byte[] buffer)
        {
            if (buffer.Length == 0)
            {
                throw new ArgumentException("Buffer is empty!");
            }
#if SIMULATE_RW
            Console.WriteLine($"Simulating write bulk {BitConverter.ToString(buffer)}");
            return;
#else
            Console.WriteLine($"Writing bulk {BitConverter.ToString(buffer)}");
            ErrorCode errorCode = device.OpenEndpointWriter(WriteEndpointID.Ep01, EndpointType.Bulk)
                .Write(buffer, 1000, out int transferLength);
            if (errorCode == ErrorCode.Success)
            {
                return;
            }
            throw new UsbOperationException($"write bulk {BitConverter.ToString(buffer)} resulted in {errorCode.ToString()}");
#endif
        }

        private static byte[] ReadBulk(UsbDevice device, int readBufferSize)
        {
            byte[] buffer = new byte[readBufferSize];
#if SIMULATE_RW
            Console.WriteLine($"Simulating read bulk");
            return buffer;
#else
            ErrorCode errorCode = device.OpenEndpointReader(ReadEndpointID.Ep01, readBufferSize, EndpointType.Bulk)
                .Read(buffer, 1000, out int transferLength);
            Console.WriteLine($"Read bulk {BitConverter.ToString(buffer)}");
            if (errorCode == ErrorCode.Success)
            {
                return buffer;
            }
            throw new UsbOperationException($"read bulk: {BitConverter.ToString(buffer)} with code {errorCode}");
#endif
        }

        public static void WriteControl(UsbDevice device)
        {
#if SIMULATE_RW
            Console.WriteLine("Simulating control transfer");
            return;
#else
            UsbSetupPacket packet = new UsbSetupPacket(0x42, 0xd1, 0, 0, 0);
            bool success = device.ControlTransfer(ref packet, null, 0, out _);
            if (success)
            {
                return;
            }
            throw new UsbOperationException($"Control transfer failed for {packet.ToString()}!");
#endif
        }
    }
}
