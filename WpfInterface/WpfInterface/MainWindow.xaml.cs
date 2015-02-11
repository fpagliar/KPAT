using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;
using System.Diagnostics;
using System.Net.Sockets;
using System.IO;
using System.Collections.Concurrent;
using System.Threading;

namespace WpfInterface
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static ConcurrentBag<NetworkStream> listenerStreams = new ConcurrentBag<NetworkStream>();

        public MainWindow()
        {
            InitializeComponent();
            setUpSocket();

            KinectSensor sensor = KinectSensor.KinectSensors.Where(s => s.Status == KinectStatus.Connected).FirstOrDefault();

            if (sensor != null)
            {
                sensor.ColorStream.Enable();
                sensor.DepthStream.Enable();
                sensor.SkeletonStream.Enable();

                sensor.ColorFrameReady += Sensor_ColorFrameReady;
                sensor.SkeletonFrameReady += Sensor_SkeletonFrameReady;


                VoiceController voiceController = new VoiceController();
                voiceController.SpeechRecognized += Recognizer_SpeechRecognized;
                voiceController.SpeechHypothesized += Recognizer_SpeechHypothezised;
                voiceController.SpeechDetected += Recognizer_SpeechDetected;
                voiceController.SpeechRejected += Recognizer_SpeechRejected;
                voiceController.RecognitionConfidence = 0.50;

                List<String> phrases = new List<string>();
                phrases.Add("OK KINECT, START RECORDING");
                phrases.Add("SHUTDOWN");
                phrases.Add("STOP");
                sensor.Start();
                
                voiceController.StartRecognition(sensor, phrases);
                Debug.WriteLine("RECOGNITION STARTED");
            }
        }

        #region VoiceRecognition

        void Recognizer_SpeechRecognized(object sender, Microsoft.Speech.Recognition.SpeechRecognizedEventArgs e)
        {
            speechRecognized.Text = e.Result.Text + "\n" + e.Result.Confidence;
        }

        void Recognizer_SpeechHypothezised(object sender, Microsoft.Speech.Recognition.SpeechHypothesizedEventArgs e)
        {
            speechHypothezised.Text = e.Result.Text + "\n" + e.Result.Confidence;
        }

        void Recognizer_SpeechDetected(object sender, Microsoft.Speech.Recognition.SpeechDetectedEventArgs e)
        {
            speechDetected.Text = e.AudioPosition.ToString();
        }

        void Recognizer_SpeechRejected(object sender, Microsoft.Speech.Recognition.SpeechRecognitionRejectedEventArgs e)
        {
            speechRejected.Text = e.Result.Text + "\n" + e.Result.Confidence;
        }

        #endregion

        void Sensor_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (var frame = e.OpenColorImageFrame())
            {
                if (frame != null)
                {
                    MainImage.Source = WindowUtils.ToBitmap(frame);
                }
            }
        }

        void Sensor_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
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
            DrawingUtils.ClearSkeletons(skeletonCanvas);
            foreach(Skeleton skel in skeletons)
            {
                if (skel.TrackingState == SkeletonTrackingState.Tracked)
                {
                    DrawingUtils.DrawSkeleton(skeletonCanvas, skel);
                    fixSkeleton(skel);
                }
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

        private static void fixSkeleton(Skeleton skeleton)
        {
            Array types = Enum.GetValues(typeof(JointType));

            string[] lines = new string[types.Length + 1];
            lines[0] = "SKELETON:" + skeleton.TrackingId + " -> " + skeleton.TrackingState;
            int i = 1;
            foreach (JointType type in types)
            {
                lines[i++] = getFileFormat(type, skeleton);
            }

            informListeners(lines);
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

        #endregion

        private static string getFileFormat(JointType type, Skeleton skeleton)
        {
            Vector4 actualPos = getPosition(type, skeleton);

            return type + " + " + skeleton.Joints[type].TrackingState + " -> " + 
                actualPos.X + "|" + actualPos.Y + "|" + actualPos.Z + "|" + actualPos.W;
        }

        public static Vector4 getPosition(JointType type, Skeleton skel)
        {
            return skel.BoneOrientations[type].HierarchicalRotation.Quaternion;
        }
    }
}
