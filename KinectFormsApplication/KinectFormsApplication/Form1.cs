using Microsoft.Kinect;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace KinectFormsApplication
{
    public partial class Form1 : Form
    {
        //private float sumX = 0;
        //private float sumY = 0;
        //private float sumZ = 0;
        private float[] sumX = new float[10];
        private float[] sumY = new float[10];
        private float[] sumZ = new float[10];
        private float bucket1 = 45;
        private float bucket2 = 90;
        private float bucket3 = 135;
        private float offset = 15;
        private int cant = 0;

        public Form1()
        {
            InitializeComponent();
            UnityProxy.init(this);
        }
        private void button1_Click(object sender, EventArgs e)
        {
            DrawIt(0, 0, 0);
        }

        public void DrawIt(float xAngle, float yAngle, float zAngle)
        {
            System.Drawing.Graphics graphics = this.CreateGraphics();
            graphics.FillRectangle(new System.Drawing.SolidBrush(System.Drawing.Color.Wheat), new Rectangle(0, 0, 400, 400));
            //if (cant != 0)
            //{
            //    label1.Text = "X: " + sumX / cant;
            //    label2.Text = "Y: " + sumY / cant;
            //    label3.Text = "Z: " + sumZ / cant;
            //}
            sumX[cant % 10] = xAngle;
            sumY[cant % 10] = yAngle;
            sumZ[cant % 10] = zAngle;
            cant++;

            float mediaZ = 0;
            for(int i = 0; i < sumZ.Length ; i++)
            {
                mediaZ += sumZ[i];
            }
            mediaZ /= sumZ.Length;
            mediaZ = (-mediaZ);

            if (cant != 0)
            {
                label3.Text = "Z: " + mediaZ;
            }

            System.Drawing.Pen pen = System.Drawing.Pens.Black;
            if (mediaZ > (bucket1 - offset) && mediaZ < (bucket1 + offset))
            {
                pen = System.Drawing.Pens.Yellow;
            } else if (mediaZ > (bucket2 - offset) && mediaZ < (bucket2 + offset))
            {
                pen = System.Drawing.Pens.Cyan;
            } else if (mediaZ > (bucket3 - offset) && mediaZ < (bucket3 + offset))
            {
                pen = System.Drawing.Pens.Red;
            }

            graphics.DrawLine(pen, 100.0f, 100.0f, 300.0f, (float)(100.0f - Math.Tan((mediaZ - 90) * Math.PI / 180.0) * 200.0f));
//            graphics.DrawLine(System.Drawing.Pens.Cyan, 100.0f, 100.0f, 300.0f, (float)(100.0f + Math.Tan(yAngle) * 200.0f));
//            graphics.DrawLine(System.Drawing.Pens.Brown, 100.0f, 100.0f, 300.0f, (float)(100.0f + Math.Tan(xAngle) * 200.0f));
            /*
            System.Drawing.Rectangle rectangle = new System.Drawing.Rectangle(
                50, 100, 150, 150);
            graphics.DrawEllipse(System.Drawing.Pens.Black, rectangle);
            graphics.DrawRectangle(System.Drawing.Pens.Red, rectangle);
             */
        }

        private void label1_Click(object sender, System.EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            //sumX = 0;
            //sumY = 0;
            //sumZ = 0;
            //cant = 0;
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
        private static long previous = 0;

        private static ConcurrentBag<NetworkStream> listenerStreams = new ConcurrentBag<NetworkStream>();
        private static Form1 form;

        public static void init(Form1 form)
        {
            UnityProxy.form = form;
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
                    previous = skeletonFrame.Timestamp;
                    skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(skeletons);
                }
            }
            foreach (Skeleton skel in skeletons)
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

            double xRot;
            double yRot;
            double zRot;
            UnityProxy.ExtractFaceRotationInDegrees(getPosition((int)JointType.WristLeft), out xRot, out yRot, out zRot);
            UnityProxy.form.DrawIt((float)xRot, (float)yRot, (float)zRot);

            informListeners(lines);

        }

        /// <summary>
        /// Converts rotation quaternion to Euler angles 
        /// And then maps them to a specified range of values to control the refresh rate
        /// </summary>
        /// <param name="rotQuaternion">face rotation quaternion</param>
        /// <param name="pitch">rotation about the X-axis</param>
        /// <param name="yaw">rotation about the Y-axis</param>
        /// <param name="roll">rotation about the Z-axis</param>
        private static void ExtractFaceRotationInDegrees(Vector4 rotQuaternion, out double pitch, out double yaw, out double roll)
        {
            double x = rotQuaternion.X;
            double y = rotQuaternion.Y;
            double z = rotQuaternion.Z;
            double w = rotQuaternion.W;

            // convert face rotation quaternion to Euler angles in degrees
            pitch = Math.Atan2(2 * ((y * z) + (w * x)), (w * w) - (x * x) - (y * y) + (z * z)) / Math.PI * 180.0;
            yaw = Math.Asin(2 * ((w * y) - (x * z))) / Math.PI * 180.0;
            roll = Math.Atan2(2 * ((x * y) + (w * z)), (w * w) + (x * x) - (y * y) - (z * z)) / Math.PI * 180.0;
        }

        private static void informListeners(string[] lines)
        {
            List<NetworkStream> fuckedStreams = new List<NetworkStream>();
            foreach (NetworkStream stream in listenerStreams)
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
                    //Debug.WriteLine("SENDING to: " + stream + "length: " + sendBytes.Length);
                    Byte[] length = BitConverter.GetBytes(sendBytes.Length);
                    stream.Write(length, 0, length.Length);
                    //Debug.WriteLine("LENGTH LENGTH IS :" + length.Length);
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
