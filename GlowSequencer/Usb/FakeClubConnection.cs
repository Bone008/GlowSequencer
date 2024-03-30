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

    public ConnectedDevice GetConnectedClubByPortId(string connectedPortId)
    {
        return LoadClubsFromFile().Single(device => device.connectedPortId == connectedPortId);
    }

    public List<string> GetConnectedPortIds()
    {
        return LoadClubsFromFile().Select(dev => dev.connectedPortId).ToList();
    }

    public List<ConnectedDevice> ListConnectedClubs()
    {
        return LoadClubsFromFile().ToList();
    }

    public string ReadGroupName(string connectedPortId)
    {
        return GetConnectedClubByPortId(connectedPortId).groupName;
    }

    public string ReadName(string connectedPortId)
    {
        return GetConnectedClubByPortId(connectedPortId).name;
    }

    public byte[] ReadProgram(string connectedPortId, int amountOfBytes)
    {
        return new byte[amountOfBytes];
    }

    public byte[] ReadProgramAutoDetect(string connectedPortId)
    {
        return new byte[42];
    }

    public string ReadProgramName(string connectedPortId)
    {
        return GetConnectedClubByPortId(connectedPortId).programName;
    }

    public void SetColor(string connectedPortId, byte r, byte g, byte b)
    {
        Debug.WriteLine($"FCC: SetColor({connectedPortId}, {r}, {g}, {b})");
    }

    public void Start(string connectedPortId)
    {
        Debug.WriteLine($"FCC: Start({connectedPortId})");
    }

    public void Stop(string connectedPortId)
    {
        Debug.WriteLine($"FCC: Stop({connectedPortId})");
    }

    public void WriteGroupName(string connectedPortId, string groupName)
    {
        Debug.WriteLine($"FCC: WriteGroupName({connectedPortId}, {groupName})");
    }

    public void WriteName(string connectedPortId, string name)
    {
        Debug.WriteLine($"FCC: WriteName({connectedPortId}, {name})");
    }

    public void WriteProgram(string connectedPortId, byte[] programData)
    {
        Debug.WriteLine($"FCC: WriteProgram({connectedPortId}, {programData.Length} bytes)");
        System.Threading.Thread.Sleep(programData.Length);
        if (new Random().Next(0, 3) == 0)
            throw new UsbOperationException("Random simulated transmission failure");
    }

    public void WriteProgramName(string connectedPortId, string programName)
    {
        Debug.WriteLine($"FCC: WriteProgramName({connectedPortId}, {programName})");
    }

}
