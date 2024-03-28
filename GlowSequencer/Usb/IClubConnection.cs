using System.Collections.Generic;

#nullable enable

namespace GlowSequencer.Usb
{
    public struct ConnectedDevice
    {
        public string connectedPortId; //unique id for the usb port (more testing needed with hubs)
        public string name;
        public string groupName;
        public string programName;
    }

    public interface IClubConnection
    {
        /// <summary>
        /// lookup in the usb registry for all port-device ids connected. Does not open device (?).
        /// The portId is not specific for a device! Switching the usb ports of two devices will result in the same ids but for the other device.
        /// Untested: Can be used to detect connection changes which should potentially trigger a new ListConnectedClubs or GetConnectedClubByPortId call.
        /// </summary>
        /// <returns></returns>
        List<string> GetConnectedPortIds();
        /// <summary>
        /// Opens all connected club devices and retrieves the names, group_names and program_names
        /// </summary>
        /// <returns></returns>
        OperationResult<List<ConnectedDevice>?> ListConnectedClubs();

        OperationResult<ConnectedDevice> GetConnectedClubByPortId(string connectedPortId);

        OperationResult<string> ReadName(string connectedPortId);
        OperationResult WriteName(string connectedPortId, string name);

        OperationResult<string> ReadGroupName(string connectedPortId);
        OperationResult WriteGroupName(string connectedPortId, string groupName);

        OperationResult<string> ReadProgramName(string connectedPortId);
        OperationResult WriteProgramName(string connectedPortId, string programName);
        OperationResult<byte[]> ReadProgramAutoDetect(string connectedPortId);
        OperationResult<byte[]> ReadProgram(string connectedPortId, int amountOfBytes);
        OperationResult WriteProgram(string connectedPortId, byte[] programData);

        OperationResult Start(string connectedPortId);
        OperationResult Stop(string connectedPortId);
        OperationResult SetColor(string connectedPortId, byte r, byte g, byte b);
    }
}
