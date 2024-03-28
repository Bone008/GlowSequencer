using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlowSequencer.Usb;

/// <summary>
/// Simulates connected club devices from a text file.
/// This enables testing of the application without real devices.
/// </summary>
public class FakeClubConnection : IClubConnection
{
    private const string FILENAME = "fake_connected_clubs.txt";

    public FakeClubConnection()
    {
        Debug.WriteLine("FCC: Using faked usb controller!");
        Debug.WriteLine("FCC: Reading from file: " + Path.GetFullPath(FILENAME));

#if DEBUG
        if (!File.Exists(FILENAME))
            File.Create(FILENAME).Close();
        // Open file with default program
        Process.Start(Path.GetFullPath(FILENAME));
#endif
    }

    private IEnumerable<ConnectedDevice> LoadClubsFromFile()
    {
        return File.ReadAllLines(FILENAME)
            .Select((line, index) =>
            {
                // Skip empty lines and comments, but still count their line number.
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    return (ConnectedDevice?)null;

                string[] parts = line.Split(';');
                return new ConnectedDevice
                {
                    connectedPortId = $"L{index + 1:00}",
                    name = parts[0].Trim(),
                    groupName = "1",
                    programName = parts.Length > 1 ? parts[1].Trim() : "",
                };
            })
            .Where(device => device != null)
            .Select(device => device.Value);
    }

    public OperationResult<ConnectedDevice> GetConnectedClubByPortId(string connectedPortId)
    {
        ConnectedDevice result = LoadClubsFromFile()
            .Single(device => device.connectedPortId == connectedPortId);
        return OperationResult.Success(result);
    }

    public List<string> GetConnectedPortIds()
    {
        return LoadClubsFromFile().Select(dev => dev.connectedPortId).ToList();
    }

    public OperationResult<List<ConnectedDevice>> ListConnectedClubs()
    {
        return OperationResult.Success(LoadClubsFromFile().ToList());
    }

    public OperationResult<string> ReadGroupName(string connectedPortId)
    {
        return OperationResult.Success(GetConnectedClubByPortId(connectedPortId).Data.groupName);
    }

    public OperationResult<string> ReadName(string connectedPortId)
    {
        return OperationResult.Success(GetConnectedClubByPortId(connectedPortId).Data.name);
    }

    public OperationResult<byte[]> ReadProgram(string connectedPortId, int amountOfBytes)
    {
        return OperationResult.Success(new byte[amountOfBytes]);
    }

    public OperationResult<byte[]> ReadProgramAutoDetect(string connectedPortId)
    {
        return OperationResult.Success(new byte[42]);
    }

    public OperationResult<string> ReadProgramName(string connectedPortId)
    {
        return OperationResult.Success(GetConnectedClubByPortId(connectedPortId).Data.programName);
    }

    public OperationResult SetColor(string connectedPortId, byte r, byte g, byte b)
    {
        Debug.WriteLine($"FCC: SetColor({connectedPortId}, {r}, {g}, {b})");
        return OperationResult.Success();
    }

    public OperationResult Start(string connectedPortId)
    {
        Debug.WriteLine($"FCC: Start({connectedPortId})");
        return OperationResult.Success();
    }

    public OperationResult Stop(string connectedPortId)
    {
        Debug.WriteLine($"FCC: Stop({connectedPortId})");
        return OperationResult.Success();
    }

    public OperationResult WriteGroupName(string connectedPortId, string groupName)
    {
        Debug.WriteLine($"FCC: WriteGroupName({connectedPortId}, {groupName})");
        return OperationResult.Success();
    }

    public OperationResult WriteName(string connectedPortId, string name)
    {
        Debug.WriteLine($"FCC: WriteName({connectedPortId}, {name})");
        return OperationResult.Success();
    }

    public OperationResult WriteProgram(string connectedPortId, byte[] programData)
    {
        Debug.WriteLine($"FCC: WriteProgram({connectedPortId}, {programData.Length} bytes)");
        return OperationResult.Success();
    }

    public OperationResult WriteProgramName(string connectedPortId, string programName)
    {
        Debug.WriteLine($"FCC: WriteProgramName({connectedPortId}, {programName})");
        return OperationResult.Success();
    }
}
