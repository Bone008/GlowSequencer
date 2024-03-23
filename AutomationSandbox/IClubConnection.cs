using System.Collections.Generic;
using System.Configuration;
using System.Threading;

namespace AutomationSandbox
{
    public struct ConnectedDevice
    {
        public string connectedPortId; //unique id for the usb port (more testing needed with hubs)
        public string name;
        public string groupName;
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
        /// Opens all connected club devices and retrieves the names and group_names
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
        byte[] ReadProgram(string connectedPortId);
        void WriteProgram(string connectedPortId, byte[] programData);

        void Start(string connectedPortId);
        void Stop(string connectedPortId);
        void SetColor(string connectedPortId, byte r, byte g, byte b);
    }
}