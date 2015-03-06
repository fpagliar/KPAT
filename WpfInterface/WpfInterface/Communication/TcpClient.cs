using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
namespace WpfInterface
{
    class TcpClient
    {
        private ConcurrentBag<ClientListener> listeners = new ConcurrentBag<ClientListener>();
        private NetworkStream serverStream;
        private bool active = true;

        public TcpClient(string ip, int port)
        {
            System.Net.Sockets.TcpClient clientSocket = new System.Net.Sockets.TcpClient();
            clientSocket.Connect(ip, port);
            serverStream = clientSocket.GetStream();
        }

        public void subscribe(ClientListener listener)
        {
            if (!active)
                throw new System.Exception("NOT ACTIVE");
            listeners.Add(listener);
        }

        public void unsubscribe(ClientListener listener)
        {
            // Improvised, Trytake was failing...
            ConcurrentBag<ClientListener> newBag = new ConcurrentBag<ClientListener>();
            foreach (ClientListener l in listeners)
                if (l != listener)
                    newBag.Add(l);
            listeners = newBag;
        }

        public void unsubscribeAll()
        {
            listeners = new ConcurrentBag<ClientListener>();
        }

        public List<ClientListener> getListerners()
        {
            if (!active)
                throw new System.Exception("NOT ACTIVE");
            return new List<ClientListener>(listeners);
        }

        public bool hasData()
        {
            if (!active)
                throw new System.Exception("NOT ACTIVE");
            return serverStream.DataAvailable;
        }

        /// <summary>
        /// Calls the listener only if there is data available. Non-blocking.
        /// </summary>
        public void tryRun()
        {
            if (!active)
                throw new System.Exception("NOT ACTIVE");
            if (serverStream.DataAvailable)
            {
                BinaryFormatter serializer = new BinaryFormatter();
                object data = serializer.Deserialize(serverStream);
                informListeners(data);
            }            
        }

        /// <summary>
        /// Runs in an infinite loop, calling when new data is available.
        /// </summary>
        public void runLoop()
        {
            if (!active)
                throw new System.Exception("NOT ACTIVE");
            while (active)
            {
                if (serverStream.DataAvailable)
                {
                    BinaryFormatter serializer = new BinaryFormatter();
                    object data = serializer.Deserialize(serverStream);
                    informListeners(data);
                }
            }
        }

        private void informListeners(object data)
        {
            foreach (ClientListener listener in listeners)
            {
                //try
                //{
                    listener.dataArrived(data);
                //}
                //catch (System.Exception e) 
                //{
                //    Debug.WriteLine("Exception: " + e.ToString());
                //} // Ignore
            }
        }

        public void shutdown()
        {
            active = false;
            serverStream.Dispose();
        }
    }
}
