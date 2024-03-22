using System.Collections.Generic;
using System.Configuration;
using System.Threading;

namespace AutomationSandbox
{
    public struct ConnectedDevice
    {
        public string deviceId;
        public string name;
        public string groupName;
    }
    
    public interface IClubConnection
    {
        List<ConnectedDevice> ListConnectedClubs();

        string ReadName(string deviceId);
        void WriteName(string deviceId, string name);

        string ReadGroupName(string deviceId);
        void WriteGroupName(string deviceId, string groupName);

        string ReadProgramName(string deviceId);
        void WriteProgramName(string deviceId, string programName);
        byte[] ReadProgram(string deviceId);
        void WriteProgram(string deviceId, byte[] programData);

        void Start(string deviceId);
        void Stop(string deviceId);
        void SetColor(string deviceId, byte r, byte g, byte b);
    }
}