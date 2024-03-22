using System;
using System.Diagnostics;
using System.Linq;
using LibUsbDotNet;
using LibUsbDotNet.Main;

namespace AutomationSandbox
{
    public class CommunicationUtility
    {
        public struct TransferHeader
        {
            public byte command;
            public byte returnLength;
            public byte addressLe0; // LSB
            public byte addressLe1; // MSB
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
                    addressLe0 = (byte)(value & 0xFF); 
                    addressLe1 = (byte)((value >> 8) & 0xFF); 
                }
            }

            public byte[] AsBuffer {
            get
            {
                byte[] buffer = new byte[] { command, returnLength, addressLe0, addressLe1, unknown1, unknown2 };
                return buffer;
            }}
        }
        public static TransferHeader? CreateHeader(byte[] headerData)
        {
            if (headerData.Length < 4)
            {
                return null;
            }

            TransferHeader header = new TransferHeader()
            {
                command = headerData[0],
                returnLength = headerData[1],
                addressLe0 = headerData[2],
                addressLe1 = headerData[3],
                unknown1 = 0x00,
                unknown2 = 0x00,
            };

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

            return null;
        }

        public static byte[] ReadContinuously(UsbDevice device, TransferHeader header, int amount)
        {
            int startAddress = header.Address;
            byte[] result = new byte[header.returnLength * amount];
            for (int i = 0; i < amount; i++)
            {
                header.Address = (ushort)(startAddress + (i * header.returnLength));
                byte[] headerBuffer = header.AsBuffer;
                byte[] readBuffer = WriteReadBulk(device, headerBuffer, header.returnLength+6);
                Array.Copy(readBuffer, 6, result, (i* header.returnLength), header.returnLength);
            }

            return result;
        }

        private static byte[] WriteReadBulk(UsbDevice device, byte[] writeBuffer, int readBufferSize)
        {
            WriteBulk(device, writeBuffer);
            return ReadBulk(device, readBufferSize);
        }
        
        private static void WriteBulk(UsbDevice device, byte[] buffer)
        {
            
            ErrorCode errorCode = device.OpenEndpointWriter(WriteEndpointID.Ep01, EndpointType.Bulk)
                .Write(buffer, 1000, out int transferLength);
            Console.WriteLine($"write bulk {buffer.ToString()} resulted in {errorCode.ToString()}");
        }

        private static byte[] ReadBulk(UsbDevice device, int readBufferSize)
        {
            byte[] buffer = new byte[readBufferSize];
            ErrorCode errorCode = device.OpenEndpointReader(ReadEndpointID.Ep01, readBufferSize, EndpointType.Bulk)
                .Read(buffer, 1000, out int transferLength);
            Console.WriteLine($"read bulk: {buffer.ToString()} with code {errorCode.ToString()}");
            return buffer;
        }
    }
}