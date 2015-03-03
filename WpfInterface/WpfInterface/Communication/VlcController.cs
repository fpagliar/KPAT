using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace WpfInterface
{
    public class VlcController
    {
        private NetworkStream serverStream;
        private static int VOL_STEP = 10;
        // To run the vlc's, from console:
        // $>..../vlc.exe -I qt --rc-host localhost:9999
        private int currentVolLevel;

        public VlcController(string ip, int port = 9999)
        {
            System.Net.Sockets.TcpClient clientSocket = new System.Net.Sockets.TcpClient();
            clientSocket.Connect(ip, port);
            serverStream = clientSocket.GetStream();
        }

        public void togglePlay()
        {
            runCommandAndGetAnswer("pause");
        }

        //public void volumeStepUp()
        //{
        //    runCommandAndGetAnswer("volup " + VOL_STEP);
        //}

        //public void volumeStepDown()
        //{
        //    runCommandAndGetAnswer("voldown " + VOL_STEP);
        //}

        public void fullVolume()
        {
            currentVolLevel = 256;
            runCommandAndGetAnswer("volume 256");
        }

        public void noVolume()
        {
            currentVolLevel = 0;
            runCommandAndGetAnswer("volume 1");
        }

        public void stop()
        {
            runCommandAndGetAnswer("stop");
        }

        public void faster()
        {
            runCommandAndGetAnswer("faster");
        }

        public void slower()
        {
            runCommandAndGetAnswer("slower");
        }

        public void normal()
        {
            runCommandAndGetAnswer("normal");
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

        private string runCommandAndGetAnswer(string command)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(command + "\n");
            serverStream.Write(bytes, 0, bytes.Length);
            serverStream.Flush();
            byte[] read = new byte[1000];
            int length = serverStream.Read(read, 0, read.Length);
            return Encoding.ASCII.GetString(read, 0, length);
        }
    }
}
