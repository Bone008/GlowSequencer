using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GlowSequencer.Model;
using GlowSequencer.Usb;

namespace GlowSequencer;

public class TransferDirectlyController
{
    public class TransferOptions
    {
        public string documentName;
        public float startTime;
        public IProgress<float> progress;
        public IProgress<string> log;
        public int maxConcurrentTransfers;
        public int maxRetries;
    }

    private readonly IClubConnection usbController;

    private HashSet<string> _knownConnectedPorts = new();

    public TransferDirectlyController()
    {
        if (Environment.GetCommandLineArgs().Contains("--fake-usb"))
            usbController = new FakeClubConnection();
        else
            usbController = new ClubConnectionUtility();
    }

    public bool HaveConnectedDevicesChanged()
    {
        List<string> connectedPorts = usbController.GetConnectedPortIds();
        return !_knownConnectedPorts.SetEquals(connectedPorts);
    }

    /// <summary>Reads data about all connected devices and returns them as a list.</summary>
    public async Task<List<ConnectedDevice>> RefreshDevicesAsync()
    {
        // TODO: catch read errors here or not?
        List<ConnectedDevice> connectedDevices = await Task.Run(
            () => usbController.ListConnectedClubs());
        _knownConnectedPorts = connectedDevices.Select(device => device.connectedPortId).ToHashSet();
        return connectedDevices;
    }

    public Task RenameDeviceAsync(string portId, string newName)
    {
        return Task.Run(() => usbController.WriteName(portId, newName));
    }

    public void StartDevices(IEnumerable<string> portIds)
    {
        // TODO: change to bulk start for better sync
        foreach (string portId in portIds)
        {
            usbController.Start(portId);
        }
    }

    public void StopDevices(IEnumerable<string> portIds)
    {
        foreach (string portId in portIds)
        {
            usbController.Stop(portId);
        }
    }

    public Task<bool> SendProgramsAsync(IDictionary<string, Track> tracksByPortId, TransferOptions options)
    {
        return Task.Run(() => SendPrograms(tracksByPortId, options));
    }

    private bool SendPrograms(IDictionary<string, Track> tracksByPortId, TransferOptions options)
    {
        var sw = new Stopwatch();
        sw.Start();
        options.log.Report("Starting transmission ...");

        int totalCount = tracksByPortId.Count;
        int successCount = 0;
        int totalRetries = 0;
        void ReportSuccess(int numRetries)
        {
            Interlocked.Add(ref totalRetries, numRetries);
            int c = Interlocked.Increment(ref successCount);
            options.progress.Report((float)c / tracksByPortId.Count);
        }

        string versionId = GenerateRandomString(3);
        string sanitizedDocumentName = FileSerializer.SanitizeString(
            Path.GetFileNameWithoutExtension(options.documentName));

        var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = options.maxConcurrentTransfers };
        Parallel.ForEach(tracksByPortId, parallelOptions, kvp =>
        {
            string portId = kvp.Key;
            Track track = kvp.Value;

            // TODO: byte limit truncation
            string programName = string.Format("{0}_{1}_{2}.bin",
                FileSerializer.SanitizeString(track.Label),
                versionId,
                sanitizedDocumentName);

            GloCommand program = FileSerializer.ExportTrackToContainer(track, options.startTime);
            byte[] programData = ProgramConverter.ConvertToBytes(program);

            string deviceName = "unknown";
            int failures = 0;
            bool success = false;
            do
            {
                try
                {
                    deviceName = usbController.ReadName(portId);
                    usbController.WriteProgram(portId, programData);
                    usbController.WriteProgramName(portId, programName);
                    options.log.Report($"Sent to {deviceName} the program \"{programName}\" ({programData.Length:#,###} bytes).");
                    ReportSuccess(failures);
                    success = true;
                }
                catch (UsbOperationException e)
                {
                    Debug.WriteLine($"Transmission failure: {e}");
                    failures++;
                    string retrying = failures <= options.maxRetries
                        ? $"RETRYING {failures}/{options.maxRetries}"
                        : "FAILED TOO OFTEN";
                    options.log.Report($"({retrying}) Transmission failure to {deviceName}: {e.Message}");
                }
            } while (!success && failures <= options.maxRetries);
        });

        sw.Stop();
        double duration = sw.Elapsed.TotalSeconds;
        bool success = successCount >= totalCount;
        string successStr = success ? "SUCCESS" : "FAILURE";
        options.log.Report($"{successStr}: Transferred {successCount} of {totalCount} programs! "
            + $"(total retries: {totalRetries}, duration: {duration:0.0} s)");

        return success;
    }

    private static string GenerateRandomString(int length)
    {
        const string ALPHABET = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuwxyz";
        var r = new Random();
        return new string(Enumerable.Range(0, length)
            .Select(_ => ALPHABET[r.Next(ALPHABET.Length)])
            .ToArray());
    }
}
