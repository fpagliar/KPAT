using Microsoft.Kinect;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace KinectServer
{
    class TcpServer
    {
        private ConcurrentBag<NetworkStream> listenerStreams = new ConcurrentBag<NetworkStream>();

        public TcpServer(int port)
        {
            setUpSocket(port);
        }

        private void setUpSocket(int port)
        {
            TcpListener serverSocket = new TcpListener(port);
            serverSocket.Start();
            Thread thread = new Thread(new ThreadStart(new ThreadServer(serverSocket, listenerStreams).run));
            thread.Start();
        }

        public void informListeners(Object data)
        {
            List<NetworkStream> fuckedStreams = new List<NetworkStream>();
            foreach (NetworkStream stream in listenerStreams)
            {
                try
                {
                    BinaryFormatter serializer = new BinaryFormatter();
                    serializer.Serialize(stream, data);
                    stream.Flush();
                }
                catch (IOException)
                {
                    // IOException can happen for multiple reasons, but basically, that channel
                    // has problems and I don't want it, kicking it out. If you want to reconnect,
                    // just get a new one.
                    fuckedStreams.Add(stream);
                }
            }

            foreach (NetworkStream stream in fuckedStreams)
            {
                NetworkStream temp = stream;
                temp.Dispose();
                listenerStreams.TryTake(out temp);
            }            
        }

        #region ThreadServer

        private class ThreadServer
        {
            private TcpListener serverSocket;
            private ConcurrentBag<NetworkStream> listenerStreams;

            public ThreadServer(TcpListener serverSocket, ConcurrentBag<NetworkStream> listenerStreams)
            {
                this.serverSocket = serverSocket;
                this.listenerStreams = listenerStreams;
            }

            public void run()
            {
                while (true)
                {
                    TcpClient clientSocket = default(TcpClient);
                    //Locking in accept, waiting to get a new client
                    clientSocket = serverSocket.AcceptTcpClient();
                    NetworkStream networkStream = clientSocket.GetStream();
                    //Added it to the list of streams I'm communicating with
                    listenerStreams.Add(networkStream);
                }
            }
        }

        #endregion
    }
}
