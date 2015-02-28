using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace WpfInterface
{
    class TcpClient
    {
        private ClientListener listener;
        private NetworkStream serverStream;

        public TcpClient(string ip, int port, ClientListener listener)
        {
            this.listener = listener;
            System.Net.Sockets.TcpClient clientSocket = new System.Net.Sockets.TcpClient();
            clientSocket.Connect(ip, port);
            serverStream = clientSocket.GetStream();
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
                    Debug.WriteLine("asd");
                    BinaryFormatter serializer = new BinaryFormatter();
                    listener.dataArrived(serializer.Deserialize(serverStream));
                }
            }            
        }
    }
}
