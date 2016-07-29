using LibUsbDotNet;
using LibUsbDotNet.Main;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AutomationSandbox
{
    class LibUsbProgram
    {
        static void Main(string[] args)
        {
            Console.WriteLine("All devices:");
            UsbDevice.AllDevices.ToList().ForEach(usbRegistry =>
            {
                /*foreach (var p in usbRegistry.GetType().GetProperties().Where(p => !p.GetIndexParameters().Any()))
                    Console.WriteLine(p.Name + " = " + p.GetValue(usbRegistry));

                Console.WriteLine("props:");
                foreach (var x in usbRegistry.DeviceProperties)
                    Console.WriteLine("  " + x.Key + " = " + x.Value);*/


                

                UsbDevice device;
                if(!usbRegistry.Open(out device))
                {
                    Console.WriteLine("Unable to open device!");
                    return;
                }

                bool success;

                LibUsbDotNet.Main.UsbSetupPacket packet = new LibUsbDotNet.Main.UsbSetupPacket(66, 209, 0, 0, 0);
                int lengthTransferred;
                success = device.ControlTransfer(ref packet, null, 0, out lengthTransferred);
                Console.WriteLine("ctl transfer: " + success);

                Thread.Sleep(2000);

                Console.WriteLine("Trying to write sth ...");

                UsbEndpointWriter writer = device.OpenEndpointWriter(WriteEndpointID.Ep01, EndpointType.Bulk);
                transferSomething(writer, 0);


                success = device.Close();
                Console.WriteLine("close: " + success);
            });
            Console.WriteLine("Done!");
            Console.ReadKey(true);
        }

        private static void transferSomething(UsbEndpointWriter writer, int dat)
        {
            //doBulkTransferSend(writer, 99, 255, -1);

            byte[] buf = new byte[32];

            // command
            buf[0] = 99;
            // payload
            buf[1] = (byte)((0xff0000 & dat) >> 16);
            buf[2] = (byte)((0xff00 & dat) >> 8);
            buf[3] = (byte)(dat & 0xff);

            sendBuffer(writer, buf);

            // TODO doBulkTransferReceive(0, false);
        }

        private static ErrorCode doBulkTransferSend(UsbEndpointWriter writer, int command, int dat2_8bit, int dat3_24bit)
        {
            byte[] buf = new byte[32];

            // command
            buf[0] = (byte)(command & 0xff);

            // payload
            if (dat2_8bit != 255)
                buf[1] = (byte)(dat2_8bit & 0xff);
            if (dat3_24bit != -1)
            {
                buf[2] = (byte)(dat3_24bit & 0xff);
                buf[3] = (byte)(dat3_24bit >> 8 & 0xff);
                buf[4] = (byte)(dat3_24bit >> 16 & 0xff);
                buf[5] = 0;
            }

            return sendBuffer(writer, buf);
        }

        private static ErrorCode sendBuffer(UsbEndpointWriter writer, byte[] buf)
        {
            int transferLength;
            return writer.Write(buf, 1000, out transferLength);
        }
    }
}
