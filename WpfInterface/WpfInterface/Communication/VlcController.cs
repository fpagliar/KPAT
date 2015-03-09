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
        private static int VOL_STEP = 10;
        // To run the vlc's, from console:
        // $>..../vlc.exe -I qt --rc-host localhost:9999
        // C:\Program Files (x86)\VideoLAN\VLC>vlc --extraintf rc --rc-host 192.168.0.169:9999
        //                                                                  My Public IP : port

        private int currentVolLevel;
        private bool stopped;
        private DateTime lastCommand;

        public VlcController(string ip, int port)
        {
            System.Net.Sockets.TcpClient clientSocket = new System.Net.Sockets.TcpClient();
            clientSocket.Connect(ip, port);
            serverStream = clientSocket.GetStream();
            stopped = true;
            currentVolLevel = 0;
            this.port = port;
            lastCommand = DateTime.Now;
        }

        //public void play()
        //{
        //    if (stopped)
        //    {
        //        string ans = runCommandAndGetAnswer("play");
        //    }
        //    else
        //    {
        //        Debug.WriteLine(port + "not playing on unstopped");
        //    }
        //}

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
            //if (stopped)
            //{
            //    string ans = runCommandAndGetAnswer("play");
            //    //if (!ans.Contains("status change: ( play state: 2 ): Play") || !ans.Contains("status change: ( play state: 1 ): Opening"))
            //    //{
            //    //    Debug.WriteLine("error playing:\n" + ans);
            //    //} 
            //    stopped = false;
            //    paused = false;
            //}
            //else
            //{
            //    string ans = runCommandAndGetAnswer("pause");
            //    if(paused)
            //    {
            //        //if (!ans.Contains("status change: ( play state: 2 ): Play"))
            //        //{
            //        //    Debug.WriteLine("error unpausing:\n" + ans);
            //        //}
            //    }
            //    else
            //    {
            //        //if (!ans.Contains("status change: ( pause state: 3 ): Pause"))
            //        //{
            //        //    Debug.WriteLine("error pausing:\n" + ans);
            //        //}

            //    }
            //    paused = !paused;
            //}

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
            string ans = runCommandAndGetAnswer("volume 256");
            //if (!ans.Contains("volume: returned 0 (no error)"))
            //{
            //    Debug.WriteLine("error setting the volume:\n" + ans);
            //}
            currentVolLevel = 256;
        }

        public void noVolume()
        {
            string ans = runCommandAndGetAnswer("volume 1");
            //if (!ans.Contains("volume: returned 0 (no error)"))
            //{
            //    Debug.WriteLine("error setting the volume:\n" + ans);
            //}
            currentVolLevel = 0;
        }

        //public void stop()
        //{
        //    runCommandAndGetAnswer("stop");
        //}

        public void faster()
        {
            string ans = runCommandAndGetAnswer("faster");
            //if (!ans.Contains("faster: returned 0 (no error)") || !ans.Contains("status change: ( new rate:"))
            //{
            //    Debug.WriteLine("error setting faster:\n" + ans);
            //}
        }

        public void slower()
        {
            string ans = runCommandAndGetAnswer("slower");
            //if (!ans.Contains("slower: returned 0 (no error)") || !ans.Contains("status change: ( new rate:"))
            //{
            //    Debug.WriteLine("error setting slower:\n" + ans);
            //}
        }

        public void normal()
        {
            string ans = runCommandAndGetAnswer("normal");
            //if (!ans.Contains("normal: returned 0 (no error)") || !ans.Contains("status change: ( new rate:"))
            //{
            //    Debug.WriteLine("error setting normal:\n" + ans);
            //}
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

            //Debug.WriteLine(port + " will run " + command);
            byte[] bytes = Encoding.ASCII.GetBytes(command + "\n");
            serverStream.Write(bytes, 0, bytes.Length);
            serverStream.Flush();
            byte[] read = new byte[1024];
            string ans = "";
            while (serverStream.DataAvailable)
            {
                int length = serverStream.Read(read, 0, read.Length);
                ans += Encoding.ASCII.GetString(read, 0, length);
            }
            //Debug.WriteLine(port + " answered \n" + ans);
            return ans;
        }
    }
}
