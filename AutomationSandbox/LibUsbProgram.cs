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
            List<ConnectedDevice> clubs = clubConnection.ListConnectedClubs();
            foreach (var club in clubs!)
            {
                Console.WriteLine(
                    $"ConnectedPortId: {club.connectedPortId}, name: {club.name}, group_name: {club.groupName}, program_name: {club.programName}");
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
            clubConnection.WriteProgram(club.connectedPortId, programBytes);
            clubConnection.WriteName(club.connectedPortId, "LongProgram");
            clubConnection.Start(club.connectedPortId);
        }

        private static byte[] TestGeneratingProgram()
        {
            GloLoopCommand loopCommand = new GloLoopCommand(2);
            loopCommand.Commands.AddRange(new GloCommand[]
            {
                new GloColorCommand(GloColor.FromRGB(20, 0, 0)),
                new GloDelayCommand(100),
                new GloColorCommand(GloColor.FromRGB(0, 20, 0)),
                new GloDelayCommand(100),
                new GloColorCommand(GloColor.FromRGB(0, 0, 20)),
                new GloDelayCommand(100),
            });

            GloCommandContainer program = new GloCommandContainer("Program", "END");
            program.Commands.AddRange(new GloCommand[]
            {
                new GloColorCommand(GloColor.FromRGB(20, 0, 0)),
                new GloDelayCommand(100),
                new GloColorCommand(GloColor.FromRGB(0, 20, 0)),
                new GloDelayCommand(1000),
                new GloRampCommand(GloColor.FromRGB(0, 0, 20), 100),
                new GloRampCommand(GloColor.Black, 1000),
                loopCommand,
                new GloRampCommand(GloColor.FromRGB(20, 20, 0), 1000),
                new GloRampCommand(GloColor.FromRGB(0, 20, 20), 1000),
                new GloRampCommand(GloColor.FromRGB(20, 0, 20), 1000),
                new GloDelayCommand(2000),
            });
            byte[] programBytes = ProgramConverter.ConvertToBytes(program);
            Console.WriteLine($"Program bytes: {BitConverter.ToString(programBytes)}");
            return programBytes;
        }


        private static void TestWriteAndReadProgram(IClubConnection clubConnection, List<ConnectedDevice> clubs,
            byte[] programBytes)
        {
            clubConnection.WriteProgram(clubs[0].connectedPortId, programBytes);
            Console.WriteLine("Written Program!");

            byte[] readProgram = clubConnection.ReadProgram(clubs[0].connectedPortId, programBytes.Length);
            Console.WriteLine($"Read Program: {BitConverter.ToString(readProgram)}");

            if (!programBytes.SequenceEqual(readProgram))
            {
                Console.WriteLine("Program does not match the read program!");
            }
            else
            {
                Console.WriteLine("Program matches the read program!");                
            }

        }

        private static void TestWriteAndReadName(IClubConnection clubConnection, List<ConnectedDevice> clubs,
            string name = "TestName Longer")
        {
            clubConnection.WriteName(clubs[0].connectedPortId, name);
            Console.WriteLine($"Written Name: {name}");

            string readName = clubConnection.ReadName(clubs[0].connectedPortId);
            Console.WriteLine($"Read Name: {readName}");

            if (!string.Equals(name, readName))
            {
                Console.WriteLine($"Name does not match the read name - Name: <{name}> != <{readName}>");
            }
            else
            {
                Console.WriteLine("Name matches the read name!");
            }
        }

        private static void TestWriteAndReadGroupName(IClubConnection clubConnection, List<ConnectedDevice> clubs,
            string groupName = "Test")
        {
            clubConnection.WriteGroupName(clubs[0].connectedPortId, groupName);
            Console.WriteLine($"Written GroupName: {groupName}");

            string readGroupName = clubConnection.ReadGroupName(clubs[0].connectedPortId);
            Console.WriteLine($"Read GroupName: {readGroupName}");

            if (!string.Equals(groupName, readGroupName))
            {
                Console.WriteLine(
                    $"GroupName does not match the read group name - GroupName: <{groupName}> != <{readGroupName}>");
            }
            else
            {
                Console.WriteLine("GroupName matches the read group name!");
            }
        }

        private static void TestWriteAndReadProgramName(IClubConnection clubConnection, List<ConnectedDevice> clubs,
            string programName = "TestProgram")
        {
            clubConnection.WriteProgramName(clubs[0].connectedPortId, programName);
            Console.WriteLine($"Written ProgramName: {programName}");

            string readProgramName = clubConnection.ReadProgramName(clubs[0].connectedPortId);
            Console.WriteLine($"Read ProgramName: {readProgramName}");

            if (!string.Equals(programName, readProgramName))
            {
                Console.WriteLine(
                    $"ProgramName does not match the read program name - ProgramName: <{programName}> != <{readProgramName}>");
            }
            else
            {
                Console.WriteLine("ProgramName matches the read program name!");
            }
        }

        private static void TestAutoReadProgram(IClubConnection clubConnection, List<ConnectedDevice> clubs)
        {
            byte[] readProgram = clubConnection.ReadProgramAutoDetect(clubs[0].connectedPortId);
            Console.WriteLine($"Read Program: {BitConverter.ToString(readProgram)}");
        }

        private static void TestStartAndStop(IClubConnection clubConnection, List<ConnectedDevice> clubs)
        {
            clubConnection.Start(clubs[0].connectedPortId);
            Console.WriteLine("Started!");
            Thread.Sleep(2000);
            clubConnection.Stop(clubs[0].connectedPortId);
            Console.WriteLine("Stopped!");
        }

        private static void TestSetColorAndStop(IClubConnection clubConnection, List<ConnectedDevice> clubs)
        {
            clubConnection.SetColor(clubs[0].connectedPortId, 255, 0, 0);
            Console.WriteLine("Color set!");
            Thread.Sleep(2000);
            clubConnection.Stop(clubs[0].connectedPortId);
            Console.WriteLine("Stopped!");
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