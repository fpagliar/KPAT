using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace WpfInterface
{
    class TcpClient
    {
        private ConcurrentBag<ClientListener> listeners = new ConcurrentBag<ClientListener>();
        private NetworkStream serverStream;

        public TcpClient(string ip, int port)
        {
            System.Net.Sockets.TcpClient clientSocket = new System.Net.Sockets.TcpClient();
            clientSocket.Connect(ip, port);
            serverStream = clientSocket.GetStream();
        }

        public void subscribe(ClientListener listener)
        {
            listeners.Add(listener);
        }

        public void unsubscribe(ClientListener listener)
        {
            ClientListener l = listener;
            listeners.TryTake(out l);
        }

        public bool hasData()
        {
            return serverStream.DataAvailable;
        }

        /// <summary>
        /// Calls the listener only if there is data available. Non-blocking.
        /// </summary>
        public void tryRun()
        {
            if (serverStream.DataAvailable)
            {
                BinaryFormatter serializer = new BinaryFormatter();
                foreach(ClientListener listener in listeners)
                    listener.dataArrived(serializer.Deserialize(serverStream));
            }            
        }

        /// <summary>
        /// Runs in an infinite loop, calling when new data is available.
        /// </summary>
        public void runLoop()
        {
            while (true)
            {
                if (serverStream.DataAvailable)
                {
                    BinaryFormatter serializer = new BinaryFormatter();
                    foreach (ClientListener listener in listeners)
                        listener.dataArrived(serializer.Deserialize(serverStream));
                }
            }            
        }
    }
}
