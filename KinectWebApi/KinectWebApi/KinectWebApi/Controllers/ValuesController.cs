using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Microsoft.Kinect;
using System.Diagnostics;
using System.Text;
using System.Net.Sockets;
using System.IO;
using System.Collections.Concurrent;
using System.Threading;

namespace KinectWebApi.Controllers
{
    public class ValuesController : ApiController
    {
        // GET api/values
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/values/5
        public string Get(int id)
        {
            Vector4 vec = UnityProxy.getPosition(id);
            return vec.X + "|" + vec.Y + "|" + vec.Z + "|" + vec.W;
        }

        // POST api/values
        public void Post([FromBody]string value)
        {
        }

        // PUT api/values/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        public void Delete(int id)
        {
        }
    }

    class UnityProxy
    {
        private static KinectSensor sensor;

        private static Skeleton actualSkeleton;
        private static DateTime lastUpdate;

        private static DateTime startTime;
        private static Skeleton tposeSkeleton;
        private static Skeleton[] tposeSamples = new Skeleton[100];

        private static ConcurrentBag<NetworkStream> listenerStreams = new ConcurrentBag<NetworkStream>();
        private static bool flag = true;

        public static void init()
        {
            //Remove this horrible flag, I don't know why it calls init twice :S
            if(!flag)
            {
                return;
            }
            flag = false;
            Debug.WriteLine("SETTING UP KINECT");
            setBasicSkeleton();
            setUpSocket();
            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    sensor = potentialSensor;
                    break;
                }
            }

            if (null != sensor)
            {
                Debug.WriteLine("SENSOR FOUND");
                sensor.SkeletonStream.Enable();
                Debug.WriteLine("SENSOR ENABLED");

                sensor.SkeletonFrameReady += SensorSkeletonFrameReady;

                // Start the sensor!
                try
                {
                    sensor.Start();
                    Debug.WriteLine("SENSOR STARTED");
                }
                catch (System.IO.IOException)
                {
                    sensor = null;
                }
            }
            startTime = DateTime.Now;
        }

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
                    Debug.WriteLine(" >> Accept connection from client");
                    NetworkStream networkStream = clientSocket.GetStream();
                    //Added it to the list of streams I'm communicating with
                    listenerStreams.Add(networkStream);
                }
            }
        }


        private static void setUpSocket()
        {
            Debug.WriteLine("SETTING UP SOCKET");
            TcpListener serverSocket = new TcpListener(8888);
            serverSocket.Start();
            Debug.WriteLine(" >> Server Started");
            Thread thread = new Thread(new ThreadStart(new ThreadServer(serverSocket, listenerStreams).run));
            thread.Start();
        }


        private static void SensorSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            Skeleton[] skeletons = new Skeleton[0];
            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(skeletons);
                }
            }
            foreach(Skeleton skel in skeletons)
            {
                if (skel.TrackingState == SkeletonTrackingState.Tracked)
                {
                    fixSkeleton(skel);
                    return;
                }
            }
        }

        private static void fixSkeleton(Skeleton skeleton)
        {
            lastUpdate = DateTime.Now;
            actualSkeleton = skeleton;

            if (startTime.AddSeconds(5) > DateTime.Now)
            {
                Debug.WriteLine("WAITING");
                return; // At start, not serializing
            }

            if (tposeSkeleton == null)
            {
                Debug.WriteLine("TPOSE FIXED");
                tposeSkeleton = skeleton;
            }

            Array types = Enum.GetValues(typeof(JointType));

            string[] lines = new string[types.Length];
            int i = 0;
            foreach (JointType type in types)
            {
                lines[i++] = getFileFormat(type);
            }

            informListeners(lines);

        }

        private static void informListeners(string[] lines)
        {
            List<NetworkStream> fuckedStreams = new List<NetworkStream>();
            foreach(NetworkStream stream in listenerStreams)
            {
                try
                {
                    StringBuilder all = new StringBuilder();
                    foreach (string line in lines)
                    {
                        all.AppendLine(line);
                    }
                    // Communication protocol: serialize the skeleton
                    // Send the size of the package first (packages have variable size)
                    // Send the serialized skeleton afterwards

                    Byte[] sendBytes = Encoding.ASCII.GetBytes(all.ToString());
                    Debug.WriteLine("SENDING to: " + stream + "length: " + sendBytes.Length);
                    Byte[] length = BitConverter.GetBytes(sendBytes.Length);
                    stream.Write(length, 0, length.Length);
                    Debug.WriteLine("LENGTH LENGTH IS :" + length.Length);
                    stream.Write(sendBytes, 0, sendBytes.Length);
                    stream.Flush();
                }
                catch (IOException)
                {
                    //IOException can happen for multiple reasons, but basically, that channel
                    //has problems and I don't want it, kicking it out. If you want to reconnect,
                    //just get a new one.
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

        private static string getFileFormat(JointType type)
        {
            Vector4 tposePos = getPosition((int)type, tposeSkeleton);
            Vector4 actualPos = getPosition((int)type, actualSkeleton);
            
            return (int)type + "*" +
                tposePos.X + "|" + tposePos.Y + "|" + tposePos.Z + "|" + tposePos.W + "#" +
                actualPos.X + "|" + actualPos.Y + "|" + actualPos.Z + "|" + actualPos.W;
        }

        public static Vector4 getPosition(int type)
        {
            return actualSkeleton.BoneOrientations[(JointType)type].AbsoluteRotation.Quaternion;
        }

        public static Vector4 getPosition(int type, Skeleton skel)
        {
            return skel.BoneOrientations[(JointType)type].AbsoluteRotation.Quaternion;
        }

        public static DateTime getLastUpdate()
        {
            return lastUpdate;
        }

        private static void setBasicSkeleton()
        {
            actualSkeleton = new Skeleton();
            foreach (BoneOrientation bone in actualSkeleton.BoneOrientations)
            {
                Vector4 val = new Vector4();
                val.W = 0;
                val.X = 0;
                val.Y = 0;
                val.Z = 0;
                bone.AbsoluteRotation.Quaternion = val;
            }
        }
    }

}