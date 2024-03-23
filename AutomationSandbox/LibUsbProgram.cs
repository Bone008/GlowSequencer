using LibUsbDotNet;
using LibUsbDotNet.Main;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using LibUsbDotNet.DeviceNotify;

namespace AutomationSandbox
{
    class LibUsbProgram
    {
        public static IDeviceNotifier UsbDeviceNotifier = DeviceNotifier.OpenDeviceNotifier();
        static void Main(string[] args)
        {
            IClubConnection clubConnection = new ClubConnectionUtility();
            if (!clubConnection.ListConnectedClubs().IsSuccessWithResult(out List<ConnectedDevice>? clubs))
            {
                return;
            }
            foreach (var club in clubs!)
            {
                Console.WriteLine($"ConnectedPortId: {club.connectedPortId}, name: {club.name}, group_name: {club.groupName}, program_name: {club.programName}");
            }
            if (clubs.Count == 0)
            {
                Console.WriteLine("No connected clubs found!");
                return;
            }
            
            //TestWriteAndReadName(clubConnection, clubs, "Testing new Name 4");
            //TestWriteAndReadGroupName(clubConnection, clubs, "AA");
            //TestWriteAndReadProgramName(clubConnection, clubs, "TestProgram Name 2");
            TestStartAndStop(clubConnection, clubs);
            TestSetColorAndStop(clubConnection, clubs);

            return;
            // Hook the device notifier event
            UsbDeviceNotifier.Enabled = true;
            UsbDeviceNotifier.OnDeviceNotify += OnDeviceNotifyEvent;

            Console.WriteLine("Listening for device events...");
            //Console.ReadKey(true);
            while (!Console.KeyAvailable)
            {
                Thread.Sleep(10);
            }
            
            UsbDeviceNotifier.Enabled = false;  // Disable the device notifier

            // Unhook the device notifier event
            UsbDeviceNotifier.OnDeviceNotify -= OnDeviceNotifyEvent;
            return;
            Console.WriteLine("All devices:");
            UsbDevice.AllDevices.ToList().ForEach(usbRegistry =>
            {
                //This somehow disables opening the device later
                // foreach (var p in usbRegistry.GetType().GetProperties().Where(p => !p.GetIndexParameters().Any()))
                //     Console.WriteLine(p.Name + " = " + p.GetValue(usbRegistry));
                //
                
                Console.WriteLine("props:");
                foreach (var x in usbRegistry.DeviceProperties)
                    Console.WriteLine("  " + x.Key + " = " + x.Value);
                

                UsbDevice device;
                if(!usbRegistry.Open(out device))
                {
                    Console.WriteLine("Unable to open device!");
                    return;
                }

                bool success;

                LibUsbDotNet.Main.UsbSetupPacket packet = new LibUsbDotNet.Main.UsbSetupPacket(0x42, 0xd1, 0, 0, 0);
                int lengthTransferred;
                //success = device.ControlTransfer(ref packet, null, 0, out lengthTransferred);
                //Console.WriteLine("ctl transfer: " + success);

                Thread.Sleep(2000);

                //Console.WriteLine("Trying to write sth ...");

                //UsbEndpointWriter writer = device.OpenEndpointWriter(WriteEndpointID.Ep01, EndpointType.Bulk);
                //transferSomething(writer, 0);
                var info = device.Info;
                Console.WriteLine($"Info: \n{info.ToString()}");


                success = device.Close();
                Console.WriteLine("close: " + success);
            });
            Console.WriteLine("Done!");
            Console.ReadKey(true);
        }

        private static void TestWriteAndReadName(IClubConnection clubConnection, List<ConnectedDevice> clubs, string name = "TestName Longer")
        {
            OperationResult writeNameOr = clubConnection.WriteName(clubs[0].connectedPortId, name);
            if (writeNameOr.IsSuccess)
            {
                Console.WriteLine($"Written Name: {name}");
            }
            else
            {
                Console.WriteLine("Failed to write name: " + name + "\n" + writeNameOr.ErrorMessage);
            }

            OperationResult<string> readNameOr = clubConnection.ReadName(clubs[0].connectedPortId);
            if (readNameOr.IsSuccess)
            {
                Console.WriteLine($"Read Name: {readNameOr.Data}");
            }
            else
            {
                Console.WriteLine("Failed to read name: " + "\n" + readNameOr.ErrorMessage);
            }
            string readName = readNameOr.Data!;
            if (!string.Equals(name,readName))
            {
                Console.WriteLine($"Debug - Name: |{name}|, ReadName: |{readName}|");
                foreach (char c in name)
                {
                    Console.Write($"{(int)c} ");
                }
                Console.WriteLine();
                foreach (char c in readName)
                {
                    Console.Write($"{(int)c} ");
                }
                Console.WriteLine();
                
                Console.WriteLine($"Name does not match the read name - Name: <{name}> != <{readName}>");
            }
            else
            {
                Console.WriteLine("Name matches the read name!");
            }
        }
        
        private static void TestWriteAndReadGroupName(IClubConnection clubConnection, List<ConnectedDevice> clubs, string groupName = "Test")
        {
            OperationResult writeGroupNameOr = clubConnection.WriteGroupName(clubs[0].connectedPortId, groupName);
            if (writeGroupNameOr.IsSuccess)
            {
                Console.WriteLine($"Written GroupName: {groupName}");
            }
            else
            {
                Console.WriteLine("Failed to write group name: " + groupName + "\n" + writeGroupNameOr.ErrorMessage);
            }

            OperationResult<string> readGroupNameOr = clubConnection.ReadGroupName(clubs[0].connectedPortId);
            if (readGroupNameOr.IsSuccess)
            {
                Console.WriteLine($"Read GroupName: {readGroupNameOr.Data}");
            }
            else
            {
                Console.WriteLine("Failed to read group name: " + "\n" + readGroupNameOr.ErrorMessage);
            }
            string readGroupName = readGroupNameOr.Data!;
            if (!string.Equals(groupName,readGroupName))
            {
                Console.WriteLine($"Debug - GroupName: |{groupName}|, ReadGroupName: |{readGroupName}|");
                foreach (char c in groupName)
                {
                    Console.Write($"{(int)c} ");
                }
                Console.WriteLine();
                foreach (char c in readGroupName)
                {
                    Console.Write($"{(int)c} ");
                }
                Console.WriteLine();
                
                Console.WriteLine($"GroupName does not match the read group name - GroupName: <{groupName}> != <{readGroupName}>");
            }
            else
            {
                Console.WriteLine("GroupName matches the read group name!");
            }
        }
        
        private static void TestWriteAndReadProgramName(IClubConnection clubConnection, List<ConnectedDevice> clubs, string programName = "TestProgram")
        {
            OperationResult writeProgramNameOr = clubConnection.WriteProgramName(clubs[0].connectedPortId, programName);
            if (writeProgramNameOr.IsSuccess)
            {
                Console.WriteLine($"Written ProgramName: {programName}");
            }
            else
            {
                Console.WriteLine("Failed to write program name: " + programName + "\n" + writeProgramNameOr.ErrorMessage);
            }

            OperationResult<string> readProgramNameOr = clubConnection.ReadProgramName(clubs[0].connectedPortId);
            if (readProgramNameOr.IsSuccess)
            {
                Console.WriteLine($"Read ProgramName: {readProgramNameOr.Data}");
            }
            else
            {
                Console.WriteLine("Failed to read program name: " + "\n" + readProgramNameOr.ErrorMessage);
            }
            string readProgramName = readProgramNameOr.Data!;
            if (!string.Equals(programName,readProgramName))
            {
                Console.WriteLine($"Debug - ProgramName: |{programName}|, ReadProgramName: |{readProgramName}|");
                foreach (char c in programName)
                {
                    Console.Write($"{(int)c} ");
                }
                Console.WriteLine();
                foreach (char c in readProgramName)
                {
                    Console.Write($"{(int)c} ");
                }
                Console.WriteLine();
                
                Console.WriteLine($"ProgramName does not match the read program name - ProgramName: <{programName}> != <{readProgramName}>");
            }
            else
            {
                Console.WriteLine("ProgramName matches the read program name!");
            }
        }
        
        private static void TestStartAndStop(IClubConnection clubConnection, List<ConnectedDevice> clubs)
        {
            OperationResult startOr = clubConnection.Start(clubs[0].connectedPortId);
            if (startOr.IsSuccess)
            {
                Console.WriteLine("Started!");
            }
            else
            {
                Console.WriteLine("Failed to start: " + startOr.ErrorMessage);
            }
            Thread.Sleep(2000);
            OperationResult stopOr = clubConnection.Stop(clubs[0].connectedPortId);
            if (stopOr.IsSuccess)
            {
                Console.WriteLine("Stopped!");
            }
            else
            {
                Console.WriteLine("Failed to stop: " + stopOr.ErrorMessage);
            }
        }
        
        private static void TestSetColorAndStop(IClubConnection clubConnection, List<ConnectedDevice> clubs)
        {
            OperationResult setColorOr = clubConnection.SetColor(clubs[0].connectedPortId, 255, 0, 0);
            if (setColorOr.IsSuccess)
            {
                Console.WriteLine("Color set!");
            }
            else
            {
                Console.WriteLine("Failed to set color: " + setColorOr.ErrorMessage);
            }
            Thread.Sleep(2000);
            OperationResult stopOr = clubConnection.Stop(clubs[0].connectedPortId);
            if (stopOr.IsSuccess)
            {
                Console.WriteLine("Stopped!");
            }
            else
            {
                Console.WriteLine("Failed to stop: " + stopOr.ErrorMessage);
            }
        }
        

        private static void transferSomething(UsbEndpointWriter writer, int dat)
        {
            //doBulkTransferSend(writer, 99, 255, -1);

            byte[] buf = new byte[32];

            // command
            buf[0] = 99;
            // payload
            buf[1] = (byte)((0xff0000 & dat) >> 16);
            buf[2] = (byte)((0xff00 & dat) >> 8);
            buf[3] = (byte)(dat & 0xff);

            sendBuffer(writer, buf);

            // TODO doBulkTransferReceive(0, false);
        }

        private static ErrorCode doBulkTransferSend(UsbEndpointWriter writer, int command, int dat2_8bit, int dat3_24bit)
        {
            byte[] buf = new byte[32];

            // command
            buf[0] = (byte)(command & 0xff);

            // payload
            if (dat2_8bit != 255)
                buf[1] = (byte)(dat2_8bit & 0xff);
            if (dat3_24bit != -1)
            {
                buf[2] = (byte)(dat3_24bit & 0xff);
                buf[3] = (byte)(dat3_24bit >> 8 & 0xff);
                buf[4] = (byte)(dat3_24bit >> 16 & 0xff);
                buf[5] = 0;
            }

            return sendBuffer(writer, buf);
        }

        private static ErrorCode sendBuffer(UsbEndpointWriter writer, byte[] buf)
        {
            int transferLength;
            return writer.Write(buf, 1000, out transferLength);
        }
        
        private static void OnDeviceNotifyEvent(object sender, DeviceNotifyEventArgs e)
        {
            // A Device system-level event has occured

            //Console.SetCursorPosition(0,Console.CursorTop);

            Console.WriteLine("Device event:" + e.ToString()); // Dump the event info to output.

            Console.WriteLine();
            Console.Write("[Press any key to exit]");
        }
    }
}
