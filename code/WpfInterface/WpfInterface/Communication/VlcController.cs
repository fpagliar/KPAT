using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace WpfInterface
{
    public class VlcController
    {
        private NetworkStream serverStream;
        private int port;
        // To run the vlc's, from console:
        // C:\Program Files (x86)\VideoLAN\VLC>vlc --extraintf rc --rc-host 192.168.0.169:9999
        //                                                                  My Public IP : port

        private int currentVolLevel;
        private bool stopped;
        private DateTime lastCommand;

        public VlcController(string ip, int port)
        {
            // TODO: lower the connection timeout
            System.Net.Sockets.TcpClient clientSocket = new System.Net.Sockets.TcpClient();
            clientSocket.Connect(ip, port);
            serverStream = clientSocket.GetStream();
            stopped = true;
            currentVolLevel = 0;
            this.port = port;
            lastCommand = DateTime.Now;
        }

        public void setup()
        {
            runCommandAndGetAnswer("stop");
            Thread.Sleep(100);
            runCommandAndGetAnswer("pause");
            Thread.Sleep(100); 
            runCommandAndGetAnswer("stop");
            Thread.Sleep(100);
            runCommandAndGetAnswer("play");
            Thread.Sleep(100);
            normal();
            Thread.Sleep(100);
            fullVolume();
            Thread.Sleep(100);
            runCommandAndGetAnswer("stop");
            stopped = true;
        }

        public void stop()
        {
            string ans = runCommandAndGetAnswer("stop");
            stopped = true;
        }

        public void togglePlayPause()
        {
            if (stopped)
            {
                string ans = runCommandAndGetAnswer("play");
            }
            else
            {
                string ans = runCommandAndGetAnswer("pause");
            }
            stopped = false;
        }

        public void fullVolume()
        {
            string ans = runCommandAndGetAnswer("volume 256");
            currentVolLevel = 256;
        }

        public void noVolume()
        {
            string ans = runCommandAndGetAnswer("volume 1");
            currentVolLevel = 0;
        }

        public void faster()
        {
            string ans = runCommandAndGetAnswer("faster");
        }

        public void slower()
        {
            string ans = runCommandAndGetAnswer("slower");
        }

        public void normal()
        {
            string ans = runCommandAndGetAnswer("normal");
        }

        public void toggleVolume()
        {
            if (currentVolLevel == 0)
            {
                fullVolume();
            }
            else
            {
                noVolume();
            }
        }

        public int getVolume()
        {
            return currentVolLevel;
        }

        public void shutdown()
        {
            if(serverStream != null)
                serverStream.Dispose();
            serverStream = null;
        }

        private string runCommandAndGetAnswer(string command)
        {
            if (lastCommand.AddMilliseconds(100) > DateTime.Now)
            {
                return "";
            }
            lastCommand = DateTime.Now;
            // Vlc commands must end in \n
            byte[] bytes = Encoding.ASCII.GetBytes(command + "\n");
            try
            {
                serverStream.Write(bytes, 0, bytes.Length);
                serverStream.Flush();
                byte[] read = new byte[1024];
                string ans = "";
                while (serverStream.DataAvailable)
                {
                    int length = serverStream.Read(read, 0, read.Length);
                    ans += Encoding.ASCII.GetString(read, 0, length);
                }
                return ans;
            }
            catch (Exception) { }
            return "";
        }
    }
}
