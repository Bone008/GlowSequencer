using System;
using System.Collections.Generic;
using GlowSequencer.Model;

namespace GlowSequencer.Usb;

public class ProgramConverter
{
    public static byte[] ConvertToBytes(GloCommand gloCommand)
    {
        List<byte> bytes = new List<byte>();
        if (gloCommand is GloCommandContainer gloCommandContainer)
        {
            if (gloCommandContainer is GloLoopCommand loopCommand)
            {
                bytes.AddRange(LoopCommandAsBytes(loopCommand));
            }
            else if (gloCommandContainer.TerminatorName == "END")
            {
                bytes.AddRange(MainContainerAsBytes(gloCommandContainer));
            }
            else
            {
                throw new ArgumentException("Unsupported command container type", nameof(gloCommandContainer));
            }
        }
        else if (gloCommand is GloDelayCommand delayCommand)
        {
            bytes.AddRange(DelayCommandAsBytes(delayCommand));
        }
        else if (gloCommand is GloColorCommand colorCommand)
        {
            bytes.AddRange(ColorCommandAsBytes(colorCommand));
        }
        else if (gloCommand is GloRampCommand rampCommand)
        {
            bytes.AddRange(RampCommandAsBytes(rampCommand));
        }
        else
        {
            throw new ArgumentException("Unsupported command type", nameof(gloCommand));
        }

        return bytes.ToArray();
    }

    private static List<byte> MainContainerAsBytes(GloCommandContainer container)
    {
        List<byte> bytes = new List<byte>();
        foreach (var subCommand in container.Commands)
        {
            bytes.AddRange(ConvertToBytes(subCommand));
        }
        bytes.Add(0xff);//END
        bytes.Add(0xff);//Adding an additional end to make reading the program from the device easier
        return bytes;
    }

    private static List<byte> LoopCommandAsBytes(GloLoopCommand loopCommand)
    {
        List<byte> bytes = new List<byte>();
        bytes.Add(0x03); // loop command start
        if (loopCommand.Repetitions is < 0 or > 255)
        {
            throw new ArgumentOutOfRangeException(nameof(loopCommand.Repetitions), "Repetitions must be in range [0, 255]");
        }
        bytes.Add((byte)loopCommand.Repetitions);
        foreach (var subCommand in loopCommand.Commands)
        {
            bytes.AddRange(ConvertToBytes(subCommand));
        }
        bytes.Add(0x05);// loop command end
        return bytes;
    }

    private static List<byte> DelayCommandAsBytes(GloDelayCommand delayCommand)
    {
        List<byte> bytes = new List<byte>();
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
        return bytes;
    }

    private static List<byte> ColorCommandAsBytes(GloColorCommand colorCommand)
    {
        List<byte> bytes = new List<byte>();
        bytes.Add(0x01); // color command
        bytes.Add((byte)colorCommand.Color.r);
        bytes.Add((byte)colorCommand.Color.g);
        bytes.Add((byte)colorCommand.Color.b);
        return bytes;
    }

    private static List<byte> RampCommandAsBytes(GloRampCommand rampCommand)
    {
        //Ramp (short): [0x0c, r, g, b, time] Ramp (long): [0x0d, r, g, b, time 2 bytes L.E.] 
        List<byte> bytes = new List<byte>();
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
        return bytes;
    }


    private static byte[] LittleEndianShort(short number)
    {
        return new byte[] { (byte)(number & 0xFF), (byte)((number >> 8) & 0xFF) };
    }
}
