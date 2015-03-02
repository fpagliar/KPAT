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

namespace WpfInterface
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
        private static TcpClient skeletonClient;
        private static TcpClient voiceClient;

        private static CurrentRecording recordingStream;
        private static SkeletonRecording loadedMovement;
        private static bool recording = false;
        private String serverIP = "192.168.0.81";
        private int gesturePrecision = 10;
        private int bucketOffset = 15;

        List<VlcController> allVLCControllers = new List<VlcController>();

        public MainWindow()
        {
            InitializeComponent();

            Dictionary<int, System.Windows.Controls.TextBox> UIControlsSkeleton = new Dictionary<int, System.Windows.Controls.TextBox>();
            Dictionary<int, System.Windows.Controls.TextBox> UIControlsVoiceControl = new Dictionary<int, System.Windows.Controls.TextBox>();
            addWinFormsControlsSkeleton(UIControlsSkeleton);
            addWinFormsControlsVoiceControl(UIControlsVoiceControl);

            string[] leftArmIps = new string[] { "127.0.0.1", "127.0.0.1", "127.0.0.1" };
            string[] rightArmIps = new string[] { "127.0.0.1", "127.0.0.1", "127.0.0.1" };

            List<VlcController> RightVLCControllers = new List<VlcController>();
            List<VlcController> LeftVLCControllers = new List<VlcController>();
            foreach (String ip in leftArmIps)
            {
                VlcController tmp = new VlcController(ip);
                LeftVLCControllers.Add(tmp);
                allVLCControllers.Add(tmp);
            }
            foreach (String ip in rightArmIps)
            {
                VlcController tmp = new VlcController(ip);
                RightVLCControllers.Add(tmp);
                allVLCControllers.Add(tmp);
            }

            voiceClient = new TcpClient(serverIP, 8083);
            voiceClient.subscribe(new VoiceListener(UIControlsVoiceControl, LeftVLCControllers , RightVLCControllers));
            //cameraClient = new TcpClient(serverIP, 8082, new CameraListener(MainImage));

            skeletonClient = new TcpClient(serverIP, 8081);
            skeletonClient.subscribe(new SkeletonListener(skeletonCanvas));
            recordingStream = new CurrentRecording();
            skeletonClient.subscribe(recordingStream);

            addTrackingJoints();

            ArmAnalyzerListener rightArmAnalyzer = new ArmAnalyzerListener(5, JointType.ElbowRight, 10, RightVLCControllers, true,
                UIControlsSkeleton);
            ArmAnalyzerListener leftArmAnalyzer = new ArmAnalyzerListener(5, JointType.ElbowRight, 10, LeftVLCControllers, true,
                UIControlsSkeleton);

            //skeletonClient.subscribe(rightArmAnalyzer);

            Thread skeletonThread = new Thread(new ThreadStart(skeletonClient.runLoop));
            skeletonThread.Start();
            Thread voiceThread = new Thread(new ThreadStart(voiceClient.runLoop));
            voiceThread.Start();
        }

        private void addWinFormsControlsSkeleton(Dictionary<int, System.Windows.Controls.TextBox> UIControls)
        {
            UIControls.Add(0, lowerLeft);
            UIControls.Add(1, lowerRight);
            UIControls.Add(2, middleLeft);
            UIControls.Add(3, middleRight);
            UIControls.Add(4, upperLeft);
            UIControls.Add(5, upperRight);
        }

        private void addWinFormsControlsVoiceControl(Dictionary<int, System.Windows.Controls.TextBox> UIControls)
        {
            UIControls.Add(0, speechRecognized);
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
                    new SlowerAction(allVLCControllers)));
            }
        }

        private void replayLoadedMovement()
        {
            skeletonClient.subscribe(new RecordingReproducer(skeletonCanvas, loadedMovement,
                loadedMovement.getTag(), Colors.Black, skeletonClient));
        }

        #endregion

        private void CompareButton_Click(object sender, RoutedEventArgs e)
        {

        }


        private void mnuServerip(object sender, RoutedEventArgs e)
        {
            String IP = Microsoft.VisualBasic.Interaction.InputBox("Enter the Server IP Address", "KPAT",this.serverIP);
            Match match = Regex.Match(IP, @"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}");
            if (match.Success)
            {
                this.serverIP = IP;
                System.Windows.MessageBox.Show("Succesfully Changed Server IP Address to: " + this.serverIP, "Success", MessageBoxButton.OK, MessageBoxImage.Asterisk);
            }
            else {
                System.Windows.MessageBox.Show("Error - You Have Provided an Invalid IP", "Error" ,  MessageBoxButton.OK,  MessageBoxImage.Error);
            }
    
        }
        private void mnuVLCip(object sender, RoutedEventArgs e)
        {
            String IP = Microsoft.VisualBasic.Interaction.InputBox("Enter the 6 VLC Ips Separated by ';' Ex: 192.168.0.1;192.168.0.2 ... etc", "KPAT", this.serverIP);
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
            foreach (String i in IPs)
            {
                //DO Something here with the 6 Valid IPs
            }
        }

        private void mnuBucketoffset(object sender, RoutedEventArgs e)
        {
            String resp = Microsoft.VisualBasic.Interaction.InputBox("Enter the Bucket Offset", "KPAT", this.bucketOffset.ToString());
            int n;
            bool isNumeric = int.TryParse(resp, out n);
            if (isNumeric)
            {
                this.bucketOffset = n;
            }
            else
            {
                System.Windows.MessageBox.Show("Error - You Have Provided an Invalid Value", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void mnuGestureprecision(object sender, RoutedEventArgs e)
        {
            String resp = Microsoft.VisualBasic.Interaction.InputBox("Enter the Gesture Precision", "KPAT", this.gesturePrecision.ToString());
            int n;
            bool isNumeric = int.TryParse(resp, out n);
            if (isNumeric)
            {
                this.gesturePrecision = n;
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
