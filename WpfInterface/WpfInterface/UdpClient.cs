using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.IO;

namespace WpfInterface
{
    class UdpReceiver
    {
        public UdpReceiver(int port)
        {
            UdpClient receiver = new UdpClient(port);
            receiver.BeginReceive(DataReceived, receiver);
        }

        private void DataReceived(IAsyncResult ar)
        {
            UdpClient c = (UdpClient)ar.AsyncState;
            IPEndPoint receivedIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
            Byte[] receivedBytes = c.EndReceive(ar, ref receivedIpEndPoint);
            //// Convert data to ASCII and print in console
            //string receivedText = ASCIIEncoding.ASCII.GetString(receivedBytes);
            //Console.Write(receivedIpEndPoint + ": " + receivedText + Environment.NewLine);

            // Restart listening for udp data packages
            c.BeginReceive(DataReceived, ar.AsyncState);
        }

    }
}
