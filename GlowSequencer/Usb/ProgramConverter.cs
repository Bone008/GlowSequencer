using System;
using System.Collections.Generic;
using GlowSequencer.Model;

#nullable enable

namespace GlowSequencer.Usb
{
    public class ProgramConverter
    {
        public static byte[] ConvertToBytes(GloCommand gloCommand)
        {
            List<byte> bytes = new List<byte>();
            AddAsBytes(bytes, gloCommand);
            return bytes.ToArray();
        }

        private static void AddAsBytes(List<byte> bytes, GloCommand gloCommand)
        {
            if (gloCommand is GloCommandContainer gloCommandContainer)
            {
                if (gloCommandContainer is GloLoopCommand loopCommand)
                {
                    AddLoopCommandAsBytes(bytes, loopCommand);
                }
                else if (gloCommandContainer.TerminatorName == "END")
                {
                    AddMainContainerAsBytes(bytes, gloCommandContainer);
                }
                else
                {
                    throw new ArgumentException("Unsupported command container type", nameof(gloCommandContainer));
                }
            }
            else if (gloCommand is GloDelayCommand delayCommand)
            {
                AddDelayCommandAsBytes(bytes, delayCommand);
            }
            else if (gloCommand is GloColorCommand colorCommand)
            {
                AddColorCommandAsBytes(bytes, colorCommand);
            }
            else if (gloCommand is GloRampCommand rampCommand)
            {
                AddRampCommandAsBytes(bytes, rampCommand);
            }
            else
            {
                throw new ArgumentException("Unsupported command type", nameof(gloCommand));
            }
        }

        private static void AddMainContainerAsBytes(List<byte> bytes, GloCommandContainer container)
        {
            foreach (var subCommand in container.Commands)
            {
                AddAsBytes(bytes, subCommand);
            }
            bytes.Add(0xff);//END
            bytes.Add(0xff);//Adding an additional end to make reading the program from the device easier
        }

        private static void AddLoopCommandAsBytes(List<byte> bytes, GloLoopCommand loopCommand)
        {
            bytes.Add(0x03); // loop command start
            if (loopCommand.Repetitions is < 0 or > 255)
            {
                throw new ArgumentOutOfRangeException(nameof(loopCommand.Repetitions), "Repetitions must be in range [0, 255]");
            }
            bytes.Add((byte)loopCommand.Repetitions);
            foreach (var subCommand in loopCommand.Commands)
            {
                AddAsBytes(bytes, subCommand);
            }
            bytes.Add(0x05);// loop command end
        }

        private static void AddDelayCommandAsBytes(List<byte> bytes, GloDelayCommand delayCommand)
        {
            if (delayCommand.DelayTicks is >= 0 and <= 255)
            {
                bytes.Add(0x02); // delay command short
                bytes.Add((byte)delayCommand.DelayTicks);
            }
            else if (delayCommand.DelayTicks is >= 256 and <= 65535)
            {
                bytes.Add(0x04); // delay command long
                bytes.AddRange(LittleEndianShort((short)delayCommand.DelayTicks));
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(delayCommand.DelayTicks), "Delay ticks must be in range [0, 65535]");
            }
        }

        private static void AddColorCommandAsBytes(List<byte> bytes, GloColorCommand colorCommand)
        {
            bytes.Add(0x01); // color command
            bytes.Add((byte)colorCommand.Color.r);
            bytes.Add((byte)colorCommand.Color.g);
            bytes.Add((byte)colorCommand.Color.b);
        }

        private static void AddRampCommandAsBytes(List<byte> bytes, GloRampCommand rampCommand)
        {
            if (rampCommand.DurationTicks is >= 0 and <= 255)
            {
                bytes.Add(0x0c); // ramp command short
                bytes.Add((byte)rampCommand.TargetColor.r);
                bytes.Add((byte)rampCommand.TargetColor.g);
                bytes.Add((byte)rampCommand.TargetColor.b);
                bytes.Add((byte)rampCommand.DurationTicks);
            }
            else if (rampCommand.DurationTicks is >= 256 and <= 65535)
            {
                bytes.Add(0x0d); // ramp command long
                bytes.Add((byte)rampCommand.TargetColor.r);
                bytes.Add((byte)rampCommand.TargetColor.g);
                bytes.Add((byte)rampCommand.TargetColor.b);
                bytes.AddRange(LittleEndianShort((short)rampCommand.DurationTicks));
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(rampCommand.DurationTicks), "Time must be in range [0, 65535]");
            }
        }


        private static byte[] LittleEndianShort(short number)
        {
            return new byte[] { (byte)(number & 0xFF), (byte)((number >> 8) & 0xFF) };
        }
    }
}
