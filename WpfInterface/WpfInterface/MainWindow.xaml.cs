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
using System.Windows.Controls;
using System.Windows.Forms;
using Microsoft.VisualBasic;
using System.Text.RegularExpressions;
using System.Timers;

namespace WpfInterface
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const String defaultVLCIp = "127.0.0.1";
        private const string DEFAULT_SERVER_IP = "127.0.0.1";

        private static TcpClient skeletonClient;
        private static TcpClient voiceClient;

        private static CurrentRecording recordingStream;
        private static SkeletonRecording loadedMovement;
        private static bool recording = false;

        private VlcController[] allControllers = new VlcController[6];

        System.Windows.Controls.ProgressBar[] progressBars = new System.Windows.Controls.ProgressBar[6];
        System.Windows.Shapes.Rectangle[] boxes = new System.Windows.Shapes.Rectangle[6];

        public enum BucketPosition
        {
            LEFT_UP      = 0,
            LEFT_CENTER  = 1,
            LEFT_DOWN    = 2,
            RIGHT_UP     = 3,
            RIGHT_CENTER = 4,
            RIGHT_DOWN   = 5
        }

        private ArmAnalyzerListener rightArmAnalyzer;
        private ArmAnalyzerListener leftArmAnalyzer;

        public MainWindow()
        {
            InitializeComponent();

            setupBoxes();
            setupBars();

            addTrackingJoints();
        }

        private void setupBoxes()
        {
            boxes[(int)BucketPosition.LEFT_UP]     = UpperLeftBox;
            boxes[(int)BucketPosition.LEFT_CENTER] = MiddleLeftBox;
            boxes[(int)BucketPosition.LEFT_DOWN]   = LowerLeftBox;

            boxes[(int)BucketPosition.RIGHT_UP]     = UpperRightBox;
            boxes[(int)BucketPosition.RIGHT_CENTER] = MiddleRightBox;
            boxes[(int)BucketPosition.RIGHT_DOWN]   = LowerRightBox;
            for (int i = 0; i < boxes.Length; i++)
            {
                boxes[i].Fill = new SolidColorBrush(Colors.Black);
            }
        }

        private void setupBars()
        {
            progressBars[(int)BucketPosition.LEFT_UP]     = UpperLeftBar;
            progressBars[(int)BucketPosition.LEFT_CENTER] = MiddleLeftBar;
            progressBars[(int)BucketPosition.LEFT_DOWN]   = LowerLeftBar;

            progressBars[(int)BucketPosition.RIGHT_UP]     = UpperRightBar;
            progressBars[(int)BucketPosition.RIGHT_CENTER] = MiddleRightBar;
            progressBars[(int)BucketPosition.RIGHT_DOWN]   = LowerRightBar;
        }

        private static void addTrackingJoints()
        {
            // Adding the joints I want to track
            SkeletonUtils.addJoint(JointType.ShoulderLeft);
            SkeletonUtils.addJoint(JointType.ShoulderRight);
            SkeletonUtils.addJoint(JointType.ElbowLeft);
            SkeletonUtils.addJoint(JointType.ElbowRight);
            SkeletonUtils.addJoint(JointType.WristLeft);
            SkeletonUtils.addJoint(JointType.WristRight);
        }

        public Canvas getCanvas()
        {
            return skeletonCanvas;
        }

        public VlcController getController(BucketPosition pos)
        {
            return allControllers[(int)pos];
        }

        public void selectBucket(BucketPosition pos)
        {
            System.Windows.Application.Current.Dispatcher.BeginInvoke(new ThreadStart(() => boxes[(int)pos].Fill = new SolidColorBrush(Colors.Cyan)));
            System.Timers.Timer myTimer = new System.Timers.Timer();
            myTimer.Elapsed += delegate { UnselectBucket(pos); };
            myTimer.Interval = 1000; // 1s
            myTimer.Start();
        }

        private void UnselectBucket(BucketPosition pos)
        {
            System.Windows.Application.Current.Dispatcher.BeginInvoke(new ThreadStart(() => boxes[(int)pos].Fill = new SolidColorBrush(Colors.Black)));
        }

        private void setupServer(string serverIP)
        {
            if (voiceClient == null)
            {
                setupVoiceClient(serverIP);
            }
            else
            {
                TcpClient oldClient = voiceClient;
                setupVoiceClient(serverIP);
                foreach (ClientListener listener in oldClient.getListerners())
                    voiceClient.subscribe(listener);
                oldClient.shutdown();
            }
            if (skeletonClient == null)
            {
                setupSkeletonClient(serverIP);
                skeletonClient.subscribe(new SkeletonListener(skeletonCanvas));
                recordingStream = new CurrentRecording();
                skeletonClient.subscribe(recordingStream);
            }
            else
            {
                TcpClient oldClient = skeletonClient;
                setupSkeletonClient(serverIP);
                foreach (ClientListener listener in oldClient.getListerners())
                    skeletonClient.subscribe(listener);
                oldClient.shutdown();
            }
            //cameraClient = new TcpClient(serverIP, 8082, new CameraListener(MainImage));
        }

        private void setupVoiceClient(string serverIP)
        {
            voiceClient = new TcpClient(serverIP, 8083);
            Thread voiceThread = new Thread(new ThreadStart(voiceClient.runLoop));
            voiceThread.Start();
        }

        private void setupSkeletonClient(string serverIP)
        {
            skeletonClient = new TcpClient(serverIP, 8081);
            Thread skeletonThread = new Thread(new ThreadStart(skeletonClient.runLoop));
            skeletonThread.Start();
        }

        #region Button Actions

        private void RecordingButton_Click(object sender, RoutedEventArgs e)
        {
            if (recording)
            {
                // I was recording, I will now stop it
                recording = false;
                RecordingButton.Content = "Start recording";
                recordingStream.stopRecording();
            }
            else
            {
                // I was not recording, I will now start recording
                recording = true;
                RecordingButton.Content = "Stop recording";
                recordingStream.startRecording();
            }
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            SkeletonRecording recording = recordingStream.getCurrentRecording();
            if (recording != null)
            {
                skeletonClient.subscribe(new RecordingReproducer(skeletonCanvas, recording,
                    recording.getTag(), Colors.Blue, skeletonClient));
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            if (dlg.ShowDialog() == true)
            {
                recordingStream.saveRecording(dlg.FileName);
            }
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            if (dlg.ShowDialog() == true)
            {
                loadedMovement = new SkeletonRecording("loadedMovement");
                loadedMovement.loadFromFile(dlg.FileName);
                skeletonClient.subscribe(new RecordingReproducer(skeletonCanvas, loadedMovement, loadedMovement.getTag(),
                    Colors.Black, skeletonClient));

                // Loading movement to analyzer
                skeletonClient.subscribe(new MovementAnalyzer(loadedMovement, "movementAnalyzerStream", 
                    new SlowerAction(Array.AsReadOnly<VlcController>(allControllers))));
            }
        }

        private void replayLoadedMovement()
        {
            skeletonClient.subscribe(new RecordingReproducer(skeletonCanvas, loadedMovement,
                loadedMovement.getTag(), Colors.Black, skeletonClient));
        }

        #endregion

        private void mnuServerip(object sender, RoutedEventArgs e)
        {
            String IP = Microsoft.VisualBasic.Interaction.InputBox("Enter the Server IP Address", "KPAT", DEFAULT_SERVER_IP);
            Match match = Regex.Match(IP, @"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}");
            if (match.Success)
            {
                setupServer(IP);
                System.Windows.MessageBox.Show("Succesfully Changed Server IP Address to: " + IP, "Success", MessageBoxButton.OK, MessageBoxImage.Asterisk);
            }
            else {
                System.Windows.MessageBox.Show("Error - You Have Provided an Invalid IP", "Error" ,  MessageBoxButton.OK,  MessageBoxImage.Error);
            }
    
        }

        private void mnuVLCip(object sender, RoutedEventArgs e)
        {
            String IP = Microsoft.VisualBasic.Interaction.InputBox("Enter the 6 VLC Ips Separated by ';' Ex: 192.168.0.1;192.168.0.2 ... etc", "KPAT",
                defaultVLCIp + ";" + defaultVLCIp + ";" + defaultVLCIp + ";" + defaultVLCIp + ";" + defaultVLCIp + ";" + defaultVLCIp);
            String[] IPs = IP.Split(';');
            if (IPs.Length != 6) {
                System.Windows.MessageBox.Show("Error - You Have to Provide 6 Valid IPs", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;         
            }   
            foreach (String i in IPs)
            {
                Match match = Regex.Match(IP, @"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}");
                if (!match.Success) {
                    System.Windows.MessageBox.Show("Error - You Have to Provide 6 Valid IPs", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;         
                }
            }

            if (rightArmAnalyzer != null)
            {
                skeletonClient.unsubscribe(rightArmAnalyzer);
            }
            if (leftArmAnalyzer != null)
            {
                skeletonClient.unsubscribe(leftArmAnalyzer);
            }
            allControllers[(int)BucketPosition.LEFT_UP] = new VlcController(IPs[(int)BucketPosition.LEFT_UP]);
            allControllers[(int)BucketPosition.LEFT_CENTER] = new VlcController(IPs[(int)BucketPosition.LEFT_CENTER]);
            allControllers[(int)BucketPosition.LEFT_DOWN] = new VlcController(IPs[(int)BucketPosition.LEFT_DOWN]);

            allControllers[(int)BucketPosition.RIGHT_UP] = new VlcController(IPs[(int)BucketPosition.RIGHT_UP]);
            allControllers[(int)BucketPosition.RIGHT_CENTER] = new VlcController(IPs[(int)BucketPosition.RIGHT_CENTER]);
            allControllers[(int)BucketPosition.RIGHT_DOWN] = new VlcController(IPs[(int)BucketPosition.RIGHT_DOWN]);

            rightArmAnalyzer = new ArmAnalyzerListener(PositionAnalyzer.DEFAULT_MEDIA, JointType.ElbowRight, PositionAnalyzer.DEFAULT_OFFSET, true, this);
            leftArmAnalyzer = new ArmAnalyzerListener(PositionAnalyzer.DEFAULT_MEDIA, JointType.ElbowLeft, PositionAnalyzer.DEFAULT_OFFSET, false, this);

            skeletonClient.subscribe(rightArmAnalyzer);
            skeletonClient.subscribe(leftArmAnalyzer);
            voiceClient.subscribe(new VoiceListener(null, Array.AsReadOnly<VlcController>(allControllers)));
        }

        private void mnuBucketoffset(object sender, RoutedEventArgs e)
        {
            String resp = Microsoft.VisualBasic.Interaction.InputBox("Enter the bucket offset between 1 and 45", "KPAT", 
                PositionAnalyzer.DEFAULT_OFFSET.ToString());
            int n;
            bool isNumeric = int.TryParse(resp, out n);
            if (isNumeric && n > 0 && n < 45)
            {
                rightArmAnalyzer.setOffset(n);
                leftArmAnalyzer.setOffset(n);
            }
            else
            {
                System.Windows.MessageBox.Show("Error - You Have Provided an Invalid Value", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void mnuBucketMediaSize(object sender, RoutedEventArgs e)
        {
            String resp = Microsoft.VisualBasic.Interaction.InputBox("Enter the media size between 1 and 100", "KPAT", PositionAnalyzer.DEFAULT_MEDIA.ToString());
            int n;
            bool isNumeric = int.TryParse(resp, out n);
            if (isNumeric && n > 0 && n < 100)
            {
                rightArmAnalyzer.setMediaSize(n);
                leftArmAnalyzer.setMediaSize(n);
            }
            else
            {
                System.Windows.MessageBox.Show("Error - You Have Provided an Invalid Value", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void mnuGestureprecision(object sender, RoutedEventArgs e)
        {
            String resp = Microsoft.VisualBasic.Interaction.InputBox("Enter the Gesture Precision", "KPAT", MovementAnalyzer.DEFAULT_THRESHOLD.ToString());
            int n;
            bool isNumeric = int.TryParse(resp, out n);
            if (isNumeric)
            {
                //this.gesturePrecision = n;
                // TODO: set it 
            }
            else {
                System.Windows.MessageBox.Show("Error - You Have Provided an Invalid Value", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void mnuExit(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }

        private void mnuAbout(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("Final Project - KPAT - Ver1.0 ");
        }
    }
}
