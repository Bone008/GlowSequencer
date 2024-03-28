using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GlowSequencer.Model;
using GlowSequencer.Usb;
using GlowSequencer.ViewModel;

namespace GlowSequencer;

public class TransferDirectlyController
{
    private readonly IClubConnection usbController;

    private HashSet<string> knownConnectedPorts = new();

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
        return !knownConnectedPorts.SetEquals(connectedPorts);
    }

    /// <summary>Reads data about all connected devices and returns them as a list.</summary>
    public async Task<List<ConnectedDevice>> RefreshDevicesAsync()
    {
        // TODO: catch read errors here or not?
        List<ConnectedDevice> connectedDevices = await Task.Run(
            () => usbController.ListConnectedClubs().Unwrap());
        knownConnectedPorts = connectedDevices.Select(device => device.connectedPortId).ToHashSet();
        return connectedDevices;
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

}
