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
using System.ComponentModel;

namespace WpfInterface
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static ConcurrentBag<NetworkStream> listenerStreams = new ConcurrentBag<NetworkStream>();
        private static SkeletonRecorder recorder = new SkeletonRecorder(recordingTag);
        private static SkeletonRecorder replayer = new SkeletonRecorder(replayingTag);

        private static SkeletonRecorder stream;
        private static SkeletonRecorder bestReproduction;
        private static double bestReproductionDiff = 1000;

        private static VoiceController voiceController;
        private static KinectSensor sensor;

        private static bool recording = false;
        private static bool playing = false;
        private static bool replaying = false;
        private static string recordingTag = "recording";
        private static string replayingTag = "replaying";
        private static string streamTag = "stream";
        private static string bestReproductionTag = "bestReproduction";

        public MainWindow()
        {
            InitializeComponent();
            setUpSocket();

            sensor = KinectSensor.KinectSensors.Where(s => s.Status == KinectStatus.Connected).FirstOrDefault();

            if (sensor != null)
            {
                sensor.ColorStream.Enable();
                sensor.DepthStream.Enable();
                sensor.SkeletonStream.Enable();

                sensor.ColorFrameReady += Sensor_ColorFrameReady;
                sensor.SkeletonFrameReady += Sensor_SkeletonFrameReady;


                voiceController = new VoiceController();
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

            // Adding the joints I want to track
            SkeletonUtils.addJoint(JointType.ShoulderLeft);
            SkeletonUtils.addJoint(JointType.ShoulderRight);
            SkeletonUtils.addJoint(JointType.ElbowLeft);
            SkeletonUtils.addJoint(JointType.ElbowRight);
            SkeletonUtils.addJoint(JointType.WristLeft);
            SkeletonUtils.addJoint(JointType.WristRight);

        }

        void MainWindow_Closing(object sender, CancelEventArgs e)
        {   
            voiceController.StopRecognition();
            sensor.Stop();
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
            //speechRejected.Text = e.Result.Text + "\n" + e.Result.Confidence;
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
            Skeleton defaultSkeleton = skeletons.Where(s => s.TrackingState == SkeletonTrackingState.Tracked).FirstOrDefault();
            if (recording)
            {
                recorder.add(defaultSkeleton);
            }

            if(playing)
            {
                DrawingUtils.deleteElements(skeletonCanvas, recordingTag);
                SkeletonUtils.DrawSkeleton(skeletonCanvas, recorder.next(), Colors.Black, recordingTag);
            }

            if (replaying)
            {
                if (replayer.finished())
                {
                    replayer.restart();
                }
                DrawingUtils.deleteElements(skeletonCanvas, replayingTag);
                SkeletonUtils.DrawSkeleton(skeletonCanvas, replayer.next(), Colors.Blue, replayingTag);
                stream.add(defaultSkeleton);
                if (stream.size() == replayer.size())
                {
                    float diff = SkeletonUtils.difference(stream, replayer);
                    bestDiff.Text = "Best diff: " + bestReproductionDiff + " current: " + diff ;
                    if (bestReproductionDiff > diff)
                    {
                        bestReproductionDiff = diff;
                        bestReproduction = new SkeletonRecorder(stream);
                    }                    
                }
                if (bestReproduction != null)
                {
                    DrawingUtils.deleteElements(skeletonCanvas, bestReproductionTag);
                    SkeletonUtils.DrawSkeleton(skeletonCanvas, bestReproduction.next(), Colors.Pink, bestReproductionTag);
                }
            }


            foreach (Skeleton skel in skeletons)
            {
                DrawingUtils.deleteElements(skeletonCanvas, skel.TrackingId.ToString());
                if (skel.TrackingState == SkeletonTrackingState.Tracked)
                {
                    SkeletonUtils.DrawSkeleton(skeletonCanvas, skel, Colors.Cyan, skel.TrackingId.ToString());
                    fixSkeleton(skel);
                }
            }
        }

        #region Button Actions

        private void RecordingButton_Click(object sender, RoutedEventArgs e)
        {
            if (recording)
            {
                // I was recording, I will now stop it
                recording = false;
                RecordingButton.Content = "Start recording";
            }
            else
            {
                // I was not recording, I will now start recording
                recording = true;
                recorder = new SkeletonRecorder(recordingTag);
                RecordingButton.Content = "Stop recording";
            }
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (playing)
            {
                // I was playing, I will now stop it
                playing = false;
                recorder.restart();
                PlayButton.Content = "Play";
            }
            else
            {
                // I was stopped, I will now start playing
                playing = true;
                recording = false;
                PlayButton.Content = "Stop";
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            if (dlg.ShowDialog() == true)
            {
                recorder.saveToFile(dlg.FileName);
            }

        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            if (dlg.ShowDialog() == true)
            {
                replayer.loadFromFile(dlg.FileName);
                replaying = true;
                stream = new SkeletonRecorder(streamTag, replayer.size());
            }

        }

        private void CompareButton_Click(object sender, RoutedEventArgs e)
        {
            bool ans = SkeletonUtils.match(recorder, replayer);
            float diff = SkeletonUtils.difference(recorder, replayer);
            speechRejected.Text = "ANS: " + ans + " differece: " + diff + " in " + recorder.size() + " frames" +
                " vs " + replayer.size() + " frames";
        }
        private void SaveBestButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            if (dlg.ShowDialog() == true)
            {
                bestReproduction.saveToFile(dlg.FileName);
            }
        }


        #endregion

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
            informListeners(SkeletonUtils.toString(skeleton).ToString());
        }

        private static void informListeners(string lines)
        {
            List<NetworkStream> fuckedStreams = new List<NetworkStream>();
            foreach (NetworkStream stream in listenerStreams)
            {
                try
                {
                    // Communication protocol: serialize the skeleton
                    // Send the size of the package first (packages have variable size)
                    // Send the serialized skeleton afterwards

                    Byte[] sendBytes = Encoding.ASCII.GetBytes(lines);
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

    }
}
