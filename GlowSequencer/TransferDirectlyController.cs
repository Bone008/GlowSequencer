using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using GlowSequencer.Model;
using GlowSequencer.Usb;

namespace GlowSequencer;

public class TransferDirectlyController
{
    public class TransferOptions
    {
        public string documentName;
        public float startTime;
        public ColorTransformMode colorMode;
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

    public async Task<bool> HaveConnectedDevicesChangedAsync()
    {
        List<string> connectedPorts = await Task.Run(usbController.GetConnectedPortIds);
        return !_knownConnectedPorts.SetEquals(connectedPorts);
    }

    /// <summary>Reads data about all connected devices and returns them as a list.</summary>
    public async Task<List<ConnectedDevice>> RefreshDevicesAsync()
    {
        List<ConnectedDevice> connectedDevices = await Task.Run(
            () => usbController.ListConnectedClubs());
        _knownConnectedPorts = connectedDevices.Select(device => device.connectedPortId).ToHashSet();
        return connectedDevices;
    }

    public Task RenameDeviceAsync(string portId, string newName)
    {
        return Task.Run(() => usbController.WriteName(portId, newName));
    }

    public Task StartDevicesAsync(IEnumerable<string> portIds)
    {
        return Task.Run(() => usbController.StartSync(portIds));
    }

    public Task StopDevicesAsync(IEnumerable<string> portIds)
    {
        return Task.Run(() =>
        {
            foreach (string portId in portIds)
            {
                usbController.Stop(portId);
            }
        });
    }

    /// <summary>
    /// NOTE: This method does its own error handling!
    /// For all OTHER communications, UsbOperationException must be caught.
    /// </summary>
    /// <returns>A dictionary containing the success/fail status for each portId.</returns>
    public Task<Dictionary<string, bool>> SetDeviceColorsAsync(Dictionary<string, Color?> colorsByPortId, int maxConcurrentTransfers)
    {
        return Task.Run(() => SetDeviceColors(colorsByPortId, maxConcurrentTransfers));
    }

    private Dictionary<string, bool> SetDeviceColors(Dictionary<string, Color?> colorsByPortId, int maxConcurrentTransfers)
    {
        var results = new ConcurrentDictionary<string, bool>();

        var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = maxConcurrentTransfers };
        Parallel.ForEach(colorsByPortId, parallelOptions, kvp =>
        {
            string portId = kvp.Key;
            Color? color = kvp.Value;
            try
            {
                if (color.HasValue)
                {
                    Color c = color.Value;
                    usbController.SetColor(portId, c.R, c.G, c.B);
                }
                else
                {
                    usbController.Stop(portId);
                }
                results[portId] = true;
            }
            catch (UsbOperationException e)
            {
                Debug.WriteLine($"Failed to set color for {portId}:\n{e}");
                results[portId] = false;
            }
        });
        return new Dictionary<string, bool>(results);
    }

    /// <summary>
    /// NOTE: This method does its own error handling!
    /// For all OTHER communications, UsbOperationException must be caught.
    /// </summary>
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
        void ReportSuccess()
        {
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

            string programName = string.Format("{0}_{1}_{2}.bin",
                FileSerializer.SanitizeString(track.Label),
                versionId,
                sanitizedDocumentName);

            GloCommand program = FileSerializer.ExportTrackToContainer(track, options.startTime, options.colorMode);
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
                    ReportSuccess();
                    success = true;
                }
                catch (UsbOperationException e)
                {
                    Debug.WriteLine($"Transmission failure: {e}");
                    failures++;
                    bool retrying = failures <= options.maxRetries;
                    if (retrying)
                        Interlocked.Increment(ref totalRetries);
                    string prefix = retrying
                        ? $"RETRYING {failures}/{options.maxRetries}"
                        : "FAILED TOO OFTEN";
                    options.log.Report($"({prefix}) Transmission failure to {deviceName}: {e.Message}");
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

    public void InvalidateDeviceDataForPorts(IEnumerable<string> connectedPortIds)
    {
        foreach (string portId in connectedPortIds)
        {
            usbController.InvalidateDeviceData(portId);
        }
    }

    public Task DisconnectAllAsync()
    {
        return Task.Run(usbController.DisconnectAll);
    }
}
