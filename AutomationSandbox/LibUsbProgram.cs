using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GlowSequencer.Model;
using GlowSequencer.Usb;

namespace AutomationSandbox
{
    class LibUsbProgram
    {
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
            //TestStartAndStop(clubConnection, clubs);
            //TestSetColorAndStop(clubConnection, clubs);

            //byte[] programBytes = TestGeneratingProgram();
            //TestWriteAndReadProgram(clubConnection, clubs, programBytes);
            //clubConnection.Start(clubs[0].connectedPortId);
            //TestAutoReadProgram(clubConnection, clubs);
            
            //byte[] programBytes = TestGeneratingLongProgram();
            //foreach (ConnectedDevice club in clubs)
            //{
            //   Task.Run(() => TestTransmit(club, clubConnection, programBytes));
            //}

            
            Console.WriteLine("Done!");
            Console.ReadKey(true);
        }
        
        private static void TestTransmit(ConnectedDevice club, IClubConnection clubConnection, byte[] programBytes)
        {
            Console.WriteLine(clubConnection.WriteProgram(club.connectedPortId, programBytes).ToString());
            Console.WriteLine(clubConnection.WriteName(club.connectedPortId, "LongProgram").ToString());
            clubConnection.Start(club.connectedPortId);
        }
        
        private static byte[] TestGeneratingProgram()
        {
            GloLoopCommand loopCommand = new GloLoopCommand(2);
            loopCommand.Commands.AddRange(new GloCommand[]
            {
                new GloColorCommand(GloColor.FromRGB(20,0,0)),
                new GloDelayCommand(100),
                new GloColorCommand(GloColor.FromRGB(0,20,0)),
                new GloDelayCommand(100),
                new GloColorCommand(GloColor.FromRGB(0,0,20)),
                new GloDelayCommand(100),
            });
            
            GloCommandContainer program = new GloCommandContainer("Program", "END");
            program.Commands.AddRange(new GloCommand[]
            {
                new GloColorCommand(GloColor.FromRGB(20,0,0)),
                new GloDelayCommand(100),
                new GloColorCommand(GloColor.FromRGB(0,20,0)),
                new GloDelayCommand(1000),
                new GloRampCommand(GloColor.FromRGB(0,0,20),100),
                new GloRampCommand(GloColor.Black, 1000),
                loopCommand,
                new GloRampCommand(GloColor.FromRGB(20,20,0),1000),
                new GloRampCommand(GloColor.FromRGB(0,20,20),1000),
                new GloRampCommand(GloColor.FromRGB(20,0,20),1000),
                new GloDelayCommand(2000),
            });
            byte[] programBytes = ProgramConverter.ConvertToBytes(program);
            Console.WriteLine($"Program bytes: {BitConverter.ToString(programBytes)}");
            return programBytes;
        }

        
        private static void TestWriteAndReadProgram(IClubConnection clubConnection, List<ConnectedDevice> clubs, byte[] programBytes)
        {
            OperationResult writeProgramOr = clubConnection.WriteProgram(clubs[0].connectedPortId, programBytes);
            if (writeProgramOr.IsSuccess)
            {
                Console.WriteLine("Written Program!");
            }
            else
            {
                Console.WriteLine("Failed to write program: " + writeProgramOr.ErrorMessage);
            }

            OperationResult<byte[]> readProgramOr = clubConnection.ReadProgram(clubs[0].connectedPortId, programBytes.Length);
            if (readProgramOr.IsSuccess)
            {
                Console.WriteLine($"Read Program: {BitConverter.ToString(readProgramOr.Data)}");
            }
            else
            {
                Console.WriteLine("Failed to read program: " + readProgramOr.ErrorMessage);
            }
            byte[] readProgram = readProgramOr.Data!;
            if (!programBytes.SequenceEqual(readProgram))
            {
                Console.WriteLine($"Program does not match the read program - Program: <{BitConverter.ToString(programBytes)}> != <{BitConverter.ToString(readProgram)}>");
            }
            else
            {
                Console.WriteLine("Program matches the read program!");
            }
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
        
        private static void TestAutoReadProgram(IClubConnection clubConnection, List<ConnectedDevice> clubs)
        {
            OperationResult<byte[]> readProgramOr = clubConnection.ReadProgramAutoDetect(clubs[0].connectedPortId);
            if (readProgramOr.IsSuccess)
            {
                Console.WriteLine($"Read Program: {BitConverter.ToString(readProgramOr.Data)}");
            }
            else
            {
                Console.WriteLine("Failed to read program: " + readProgramOr.ErrorMessage);
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

        private static byte[] TestGeneratingLongProgram()
        {
            GloCommandContainer program = new GloCommandContainer("Program", "END");
            program.Commands.Add(new GloColorCommand(GloColor.White));
            //This lead to overflow and writing before the program offset address
            // int m = (63897 - 4 - 2) / 2;
            // for (int i = 0; i < m; i++)
            // {
            //     program.Commands.Add(new GloDelayCommand(1));;
            // }
            return ProgramConverter.ConvertToBytes(program);
        }
    }
}
