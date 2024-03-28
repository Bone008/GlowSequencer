//#define SIMULATE_RW

using System;
using System.Collections.Generic;
using LibUsbDotNet;
using LibUsbDotNet.Main;

namespace GlowSequencer.Usb
{
    public static class CommunicationUtility
    {
        public struct TransferHeader
        {
            public byte command;
            public byte dataLength;
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

            public byte[] AsBuffer
            {
                get
                {
                    byte[] buffer = new byte[] { command, dataLength, addressLe0, addressLe1, unknown1, unknown2 };
                    return buffer;
                }
            }
        }
        public static OperationResult<TransferHeader> CreateHeader(byte[] headerData)
        {
            if (headerData.Length < 4)
            {
                return OperationResult<TransferHeader>.Fail("Header data is too short!");
            }

            TransferHeader header = new TransferHeader()
            {
                command = headerData[0],
                dataLength = headerData[1],
                addressLe0 = headerData[2],
                addressLe1 = headerData[3],
                unknown1 = 0x00,
                unknown2 = 0x00,
            };

            if (headerData.Length == 4)
            {
                return OperationResult<TransferHeader>.Success(header);
            }

            if (headerData.Length == 6)
            {
                header.unknown1 = headerData[4];
                header.unknown2 = headerData[5];
                return OperationResult<TransferHeader>.Success(header);
            }

            return OperationResult<TransferHeader>.Fail("Header data length does not match!");
        }

        public static OperationResult<byte[]> ReadContinuously(UsbDevice device, TransferHeader header, int amount)
        {
            int startAddress = header.Address;
            byte[] result = new byte[header.dataLength * amount];
            for (int i = 0; i < amount; i++)
            {
                header.Address = (ushort)(startAddress + (i * header.dataLength));
                byte[] headerBuffer = header.AsBuffer;
                if (WriteReadBulk(device, headerBuffer, header.dataLength + 6).IsFail(out OperationResult<byte[]> readResult))
                {
                    return OperationResult<byte[]>.Fail($"Continuous Read failed in block {i}", readResult.ErrorMessage)!;
                }
                byte[] readBuffer = readResult.Data!;
                Array.Copy(readBuffer, 6, result, (i * header.dataLength), header.dataLength);
            }

            return OperationResult<byte[]>.Success(result);
        }

        public static IEnumerable<byte[]> ReadContinuouslyEnumerable(UsbDevice device, TransferHeader header)
        {
            int startAddress = header.Address;
            int i = 0;
            for (int j = 0; j < 65536 / header.dataLength; j++)
            {
                header.Address = (ushort)(startAddress + (i * header.dataLength));
                byte[] headerBuffer = header.AsBuffer;

                var writeReadResult = WriteReadBulk(device, headerBuffer, header.dataLength + 6);
                if (writeReadResult.IsFail(out OperationResult<byte[]?> readResult))
                {
                    throw new Exception($"Continuous Read failed in block {i}. Error: {readResult.ErrorMessage}");
                }

                byte[] readBuffer = readResult.Data!;
                byte[] chunk = new byte[header.dataLength];
                Array.Copy(readBuffer, 6, chunk, 0, header.dataLength);

                yield return chunk;
                i++;
            }
        }


        public static OperationResult WriteContinuously(UsbDevice device, TransferHeader header, byte[] data, int amount, byte[] expectedReturn)
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
                Console.WriteLine($"Writing: {BitConverter.ToString(writeBuffer)}");
                if (WriteReadBulk(device, writeBuffer, expectedReturn.Length).IsFail(out OperationResult<byte[]?> writeResult))
                {
                    return OperationResult.Fail($"Continuous Write failed in block {i}", writeResult.ErrorMessage);
                }
                byte[] readBuffer = writeResult.Data!;
                if (expectedReturn.Length > 0)
                {
                    for (int j = 0; j < expectedReturn.Length; j++)
                    {
                        if (readBuffer[j] != expectedReturn[j])
                        {
                            return OperationResult.Fail($"Expected return value not found! Expected: {BitConverter.ToString(expectedReturn)}, found: {BitConverter.ToString(readBuffer)}");
                        }
                    }
                }
            }

            return OperationResult.Success();
        }

        public static OperationResult<byte[]?> WriteReadBulk(UsbDevice device, byte[] writeBuffer, int readBufferSize)
        {
            if (WriteBulk(device, writeBuffer).IsFail(out OperationResult writeResult))
            {
                return OperationResult<byte[]>.Fail(writeResult.ErrorMessage);
            }

            if (ReadBulk(device, readBufferSize).IsFail(out OperationResult<byte[]?> readResult))
            {
                return OperationResult<byte[]>.Fail(readResult.ErrorMessage);
            }
#if SIMULATE_RW
            byte[] simulatedResultBuffer = new byte[readBufferSize];
            //copy writeBuffer to simulate read - pad or truncate if necessary
            Array.Copy(writeBuffer, simulatedResultBuffer, Math.Min(writeBuffer.Length, readBufferSize));
            Console.WriteLine($"write_read bulk sim result: {BitConverter.ToString(simulatedResultBuffer)}");
            return OperationResult<byte[]>.Success(simulatedResultBuffer)!;
#else
            return OperationResult<byte[]>.Success(readResult.Data!)!;
#endif
        }

        private static OperationResult WriteBulk(UsbDevice device, byte[] buffer)
        {
            if (buffer.Length == 0)
            {
                return OperationResult.Fail("Buffer is empty!");
            }
#if SIMULATE_RW
            Console.WriteLine($"Simulating write bulk {BitConverter.ToString(buffer)}");
            return OperationResult.Success();
#else 
            ErrorCode errorCode = device.OpenEndpointWriter(WriteEndpointID.Ep01, EndpointType.Bulk)
                .Write(buffer, 1000, out int transferLength);
            if (errorCode == ErrorCode.Success)
            {
                return OperationResult.Success();
            }
            return OperationResult.Fail($"write bulk {BitConverter.ToString(buffer)} resulted in {errorCode.ToString()}");
#endif
        }

        private static OperationResult<byte[]?> ReadBulk(UsbDevice device, int readBufferSize)
        {
            byte[] buffer = new byte[readBufferSize];
#if SIMULATE_RW
            Console.WriteLine($"Simulating read bulk");
            return OperationResult<byte[]?>.Success(buffer);
#else
            ErrorCode errorCode = device.OpenEndpointReader(ReadEndpointID.Ep01, readBufferSize, EndpointType.Bulk)
                .Read(buffer, 1000, out int transferLength);
            if (errorCode == ErrorCode.Success)
            {
                return OperationResult<byte[]>.Success(buffer)!;
            }
            return OperationResult<byte[]>.Fail($"read bulk: {BitConverter.ToString(buffer)} with code {errorCode.ToString()}");
#endif
        }

        public static OperationResult WriteControl(UsbDevice device)
        {
#if SIMULATE_RW
            Console.WriteLine("Simulating control transfer");
            return OperationResult.Success();
#else
            UsbSetupPacket packet = new UsbSetupPacket(0x42, 0xd1, 0, 0, 0);
            bool success = device.ControlTransfer(ref packet, null, 0, out _);
            if (success)
            {
                return OperationResult.Success();
            }
            return OperationResult.Fail($"Control transfer failed for {packet.ToString()}!");
#endif
        }
    }
}
