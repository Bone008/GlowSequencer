﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GlowSequencer.Usb;

/// <summary>
/// Simulates connected club devices from a text file.
/// This enables testing of the application without real devices.
/// </summary>
public class FakeClubConnection : IClubConnection
{
    private const string FILENAME = "fake_connected_clubs.txt";
    private const string MAGIC_HARD_FAIL_STRING = "YOU_FAILED_IN_LIFE";

    private static Dictionary<string, string> s_programNameOverridesByPort = new();

    private bool _hardFailMode = false;

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
        if (!File.Exists(FILENAME))
            return Enumerable.Empty<ConnectedDevice>();

        string[] lines = File.ReadAllLines(FILENAME);
        _hardFailMode = lines.Any(line => line.Contains(MAGIC_HARD_FAIL_STRING) && !line.StartsWith("#"));
        return lines
            .Select((line, index) =>
            {
                // Skip empty lines and comments, but still count their line number.
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    return (ConnectedDevice?)null;

                string[] parts = line.Split(';');
                string port = $"L{index + 1:00}";
                return new ConnectedDevice
                {
                    connectedPortId = port,
                    name = parts[0].Trim(),
                    groupName = "1",
                    programName = s_programNameOverridesByPort.TryGetValue(port, out string programName)
                        ? programName
                        : (parts.Length > 1 ? parts[1].Trim() : ""),
                };
            })
            .Where(device => device != null)
            .Select(device => device.Value);
    }

    public void DisconnectAll()
    {
        Debug.WriteLine("FCC: DisconnectAll()");
    }

    public void InvalidateDeviceData(string connectedPortId)
    {
        Debug.WriteLine($"FCC: InvalidateDeviceData({connectedPortId})");
    }

    public ConnectedDevice GetConnectedClubByPortId(string connectedPortId)
    {
        return LoadClubsFromFile().Single(device => device.connectedPortId == connectedPortId);
    }

    public List<string> GetConnectedPortIds()
    {
        Thread.Sleep(500);
        return LoadClubsFromFile().Select(dev => dev.connectedPortId).ToList();
    }

    public List<ConnectedDevice> ListConnectedClubs()
    {
        var result = LoadClubsFromFile().ToList();
        Thread.Sleep(100 * result.Count);
        return result;
    }

    public string ReadGroupName(string connectedPortId)
    {
        if (_hardFailMode)
            throw new UsbOperationException("SIMULATED HARD FAILURE");
        return GetConnectedClubByPortId(connectedPortId).groupName;
    }

    public string ReadName(string connectedPortId)
    {
        if (_hardFailMode)
            throw new UsbOperationException("SIMULATED HARD FAILURE");
        return GetConnectedClubByPortId(connectedPortId).name;
    }

    public byte[] ReadProgram(string connectedPortId, int amountOfBytes)
    {
        if (_hardFailMode)
            throw new UsbOperationException("SIMULATED HARD FAILURE");
        return new byte[amountOfBytes];
    }

    public byte[] ReadProgramAutoDetect(string connectedPortId)
    {
        if (_hardFailMode)
            throw new UsbOperationException("SIMULATED HARD FAILURE");
        return new byte[42];
    }

    public string ReadProgramName(string connectedPortId)
    {
        if (_hardFailMode)
            throw new UsbOperationException("SIMULATED HARD FAILURE");
        return GetConnectedClubByPortId(connectedPortId).programName;
    }

    public void SetColor(string connectedPortId, byte r, byte g, byte b)
    {
        Thread.Sleep(300);
        Debug.WriteLine($"FCC: SetColor({connectedPortId}, {r}, {g}, {b})");
        if (_hardFailMode)
            throw new UsbOperationException("SIMULATED HARD FAILURE");
    }

    public void Start(string connectedPortId)
    {
        Thread.Sleep(100);
        Debug.WriteLine($"FCC: Start({connectedPortId})");
        if (_hardFailMode)
            throw new UsbOperationException("SIMULATED HARD FAILURE");
    }

    public void StartSync(IEnumerable<string> connectedPortIds)
    {
        Thread.Sleep(120 * connectedPortIds.Count());
        Debug.WriteLine($"FCC: StartSync({string.Join(", ", connectedPortIds)})");
        if (_hardFailMode)
            throw new UsbOperationException("SIMULATED HARD FAILURE");
    }

    public void Stop(string connectedPortId)
    {
        Thread.Sleep(100);
        Debug.WriteLine($"FCC: Stop({connectedPortId})");
        if (_hardFailMode)
            throw new UsbOperationException("SIMULATED HARD FAILURE");
    }

    public void WriteGroupName(string connectedPortId, string groupName)
    {
        Debug.WriteLine($"FCC: WriteGroupName({connectedPortId}, {groupName})");
        if (_hardFailMode)
            throw new UsbOperationException("SIMULATED HARD FAILURE");
    }

    public void WriteName(string connectedPortId, string name)
    {
        Debug.WriteLine($"FCC: WriteName({connectedPortId}, {name})");
        if (_hardFailMode)
            throw new UsbOperationException("SIMULATED HARD FAILURE");
    }

    public void WriteProgram(string connectedPortId, byte[] programData)
    {
        Debug.WriteLine($"FCC: WriteProgram({connectedPortId}, {programData.Length} bytes)");
        Thread.Sleep(programData.Length);

        if (_hardFailMode)
            throw new UsbOperationException("SIMULATED HARD FAILURE");
        if (new Random().Next(0, 3) == 0)
            throw new UsbOperationException("Random simulated transmission failure");
    }

    public void WriteProgramName(string connectedPortId, string programName)
    {
        Debug.WriteLine($"FCC: WriteProgramName({connectedPortId}, {programName})");
        if (_hardFailMode)
            throw new UsbOperationException("SIMULATED HARD FAILURE");

        s_programNameOverridesByPort[connectedPortId] = programName;
    }
}
