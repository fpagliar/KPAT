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

        private static TcpServer tcpServer = new TcpServer(8888);
        private static PositionAnalyzer leftArmAnalyzer;
        private static PositionAnalyzer rightArmAnalyzer;

        public MainWindow()
        {
            InitializeComponent();

            sensor = KinectSensor.KinectSensors.Where(s => s.Status == KinectStatus.Connected).FirstOrDefault();
            string[] leftArmIps = new string[] {"192.168.0.41:8080", "192.168.0.36:8080", "192.168.0.68:8080"};
            string[] rightArmIps = new string[] {"192.168.0.33:8080", "192.168.0.37:8080", "192.168.0.34:8080"};
            rightArmAnalyzer = new PositionAnalyzer(5, JointType.ElbowRight, 6, 10, false, rightArmIps, true);
            leftArmAnalyzer = new PositionAnalyzer(10, JointType.ElbowLeft, 6, 10, false, leftArmIps, false);

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
                phrases.Add("STOP");
                //phrases.Add("SHUTDOWN");
                //phrases.Add("STOP");
                sensor.Start();
                sensor.ElevationAngle = 7;
                
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
            if (e.Result.Confidence > 0.70)
            {
                leftArmAnalyzer.stop();
                rightArmAnalyzer.stop();
            }
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
            if (defaultSkeleton == null)
            {
                return;
            }

            //leftArmAnalyzer.checkPosition(defaultSkeleton);
            //Color color = rightArmAnalyzer.checkPosition(defaultSkeleton);
            //if(color != Colors.Peru)
            //    SkeletonUtils.DrawSkeleton(skeletonCanvas, defaultSkeleton, color, "posskells");

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
                    if (diff < 170)
                    {
                        leftArmAnalyzer.fullVolume();
                        rightArmAnalyzer.fullVolume();
                    }
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


        private static void fixSkeleton(Skeleton skeleton)
        {
            tcpServer.informListeners(SkeletonUtils.toString(skeleton).ToString());
        }

    }
}
