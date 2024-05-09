using System;
using System.Collections.Generic;

#nullable enable

namespace GlowSequencer.Usb
{
    /// <summary>
    /// An exception that is thrown when any USB operation fails.
    /// </summary>
    public class UsbOperationException : Exception
    {
        public UsbOperationException(string message) : base(message) { }
    }

    public struct ConnectedDevice
    {
        public string connectedPortId; //unique id for the usb port (more testing needed with hubs)
        public string name;
        public string groupName;
        public string programName;
    }

    public interface IClubConnection
    {
        /// <summary>Safely releases all remaining open device connections.</summary>
        void DisconnectAll();

        /// <summary>Invalidates the cached metadata for the given port id without disconnecting.</summary>
        void InvalidateDeviceData(string connectedPortId);


        /// <summary>
        /// lookup in the usb registry for all port-device ids connected. Does not open device (?).
        /// The portId is not specific for a device! Switching the usb ports of two devices will result in the same ids but for the other device.
        /// </summary>
        /// <returns></returns>
        List<string> GetConnectedPortIds();
        /// <summary>
        /// Opens all connected club devices and retrieves the names, group_names and program_names
        /// </summary>
        /// <returns></returns>
        List<ConnectedDevice> ListConnectedClubs();

        ConnectedDevice GetConnectedClubByPortId(string connectedPortId);

        string ReadName(string connectedPortId);
        void WriteName(string connectedPortId, string name);

        string ReadGroupName(string connectedPortId);
        void WriteGroupName(string connectedPortId, string groupName);

        string ReadProgramName(string connectedPortId);
        void WriteProgramName(string connectedPortId, string programName);
        byte[] ReadProgramAutoDetect(string connectedPortId);
        byte[] ReadProgram(string connectedPortId, int amountOfBytes);
        void WriteProgram(string connectedPortId, byte[] programData);

        void Start(string connectedPortId);
        void StartSync(IEnumerable<string> connectedPortIds);
        void Stop(string connectedPortId);
        void SetColor(string connectedPortId, byte r, byte g, byte b);
    }
}
