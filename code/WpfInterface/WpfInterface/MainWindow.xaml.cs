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
        private const string defaultVLCPort = "9999";
        private const string defaultVLCIp = "192.168.0.1";
        private const string DEFAULT_SERVER_IP = "127.0.0.1";
        private const int progressBarDelta = 100;
        private const int gesturePrecision = 250;

        private static TcpClient skeletonClient;
        private static TcpClient voiceClient;
        private static CurrentRecording recordingStream;
        private static SkeletonRecording loadedMovement;
        private static ArmAnalyzerListener rightArmAnalyzer;
        private static ArmAnalyzerListener leftArmAnalyzer;
        private static int bucketPrecision = PositionAnalyzer.DEFAULT_OFFSET;
        private static int bucketMedia = PositionAnalyzer.DEFAULT_MEDIA;

        private static bool recording = false;

        public enum BucketPosition
        {
            LEFT_UP = 0,
            LEFT_CENTER = 1,
            LEFT_DOWN = 2,
            RIGHT_UP = 3,
            RIGHT_CENTER = 4,
            RIGHT_DOWN = 5
        }
        private VlcController[] allControllers = new VlcController[6];
        private bool bucketsEnabled = true;
        private bool playing = false;

        System.Windows.Controls.ProgressBar[] progressBars = new System.Windows.Controls.ProgressBar[6];
        System.Windows.Shapes.Rectangle[] boxes = new System.Windows.Shapes.Rectangle[6];
        System.Windows.Controls.Button[] enablers = new System.Windows.Controls.Button[6];

        public enum Movement
        {
            FASTER = 0,
            SLOWER = 1,
            NORMAL = 2
        }
        MovementAnalyzer[] movements = new MovementAnalyzer[3];
        double[] lastMovementValues = new double[3];
        int[] movementThresholds = { 100, 100, 100 };

        public MainWindow()
        {
            InitializeComponent();

            setupBoxes();
            setupBars();
            setupEnablers();

            addTrackingJoints();
            System.Windows.Application.Current.MainWindow.Closing += new CancelEventHandler(shutdown);
        }

        public void shutdown(object sender, CancelEventArgs e)
        {
            // Closing all connections
            foreach (VlcController controller in allControllers)
            {
                if (controller != null)
                {
                    try
                    {
                        controller.shutdown();
                    }
                    catch (Exception) { }
                }
            }
            if (voiceClient != null)
            {
                try
                {
                    voiceClient.shutdown();
                }
                catch (Exception) { }
            }
            if (skeletonClient != null)
            {
                try
                {
                    skeletonClient.shutdown();
                }
                catch (Exception) { }
            }
        }

        #region setup

        private void setupBoxes()
        {
            boxes[(int)BucketPosition.LEFT_UP] = UpperLeftBox;
            boxes[(int)BucketPosition.LEFT_CENTER] = MiddleLeftBox;
            boxes[(int)BucketPosition.LEFT_DOWN] = LowerLeftBox;

            boxes[(int)BucketPosition.RIGHT_UP] = UpperRightBox;
            boxes[(int)BucketPosition.RIGHT_CENTER] = MiddleRightBox;
            boxes[(int)BucketPosition.RIGHT_DOWN] = LowerRightBox;
            for (int i = 0; i < boxes.Length; i++)
            {
                boxes[i].Fill = new SolidColorBrush(Colors.Black);
            }
        }

        private void setupBars()
        {
            progressBars[(int)BucketPosition.LEFT_UP] = UpperLeftBar;
            progressBars[(int)BucketPosition.LEFT_CENTER] = MiddleLeftBar;
            progressBars[(int)BucketPosition.LEFT_DOWN] = LowerLeftBar;

            progressBars[(int)BucketPosition.RIGHT_UP] = UpperRightBar;
            progressBars[(int)BucketPosition.RIGHT_CENTER] = MiddleRightBar;
            progressBars[(int)BucketPosition.RIGHT_DOWN] = LowerRightBar;
        }

        private void setupEnablers()
        {
            enablers[(int)BucketPosition.LEFT_UP] = UpperLeftEnabler;
            enablers[(int)BucketPosition.LEFT_CENTER] = MiddleLeftEnabler;
            enablers[(int)BucketPosition.LEFT_DOWN] = LowerLeftEnabler;

            enablers[(int)BucketPosition.RIGHT_UP] = UpperRightEnabler;
            enablers[(int)BucketPosition.RIGHT_CENTER] = MiddleRightEnabler;
            enablers[(int)BucketPosition.RIGHT_DOWN] = LowerRightEnabler;
            for (int i = 0; i < enablers.Length; i++)
                enablers[i].Background = new SolidColorBrush(Colors.Red);
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

        private void setupServer(string serverIP)
        {
            if (voiceClient == null)
            {
                setupVoiceClient(serverIP);
                voiceClient.subscribe(new VoiceListener(this));
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

                rightArmAnalyzer = new ArmAnalyzerListener(bucketMedia, bucketPrecision, true, this);
                leftArmAnalyzer = new ArmAnalyzerListener(bucketMedia, bucketPrecision, false, this);
            }
            else
            {
                TcpClient oldClient = skeletonClient;
                setupSkeletonClient(serverIP);
                foreach (ClientListener listener in oldClient.getListerners())
                    skeletonClient.subscribe(listener);
                oldClient.shutdown();
            }
            // Not using camera because it is too slow over the network
            //cameraClient = new TcpClient(serverIP, 8082, new CameraListener(MainImage));
        }

        private void setupVoiceClient(string serverIP)
        {
            voiceClient = new TcpClient(serverIP, 8083);
            Thread voiceThread = new Thread(new ThreadStart(voiceClient.runLoop));
            voiceThread.IsBackground = true;
            voiceThread.Start();
        }

        private void setupSkeletonClient(string serverIP)
        {
            skeletonClient = new TcpClient(serverIP, 8081);
            Thread skeletonThread = new Thread(new ThreadStart(skeletonClient.runLoop));
            skeletonThread.IsBackground = true;
            skeletonThread.Start();
        }

        #endregion

        #region Backend UI Interaction Methods
        public Canvas getCanvas()
        {
            return skeletonCanvas;
        }

        public VlcController getController(BucketPosition pos)
        {
            return allControllers[(int)pos];
        }

        public IReadOnlyList<VlcController> getControllers()
        {
            return Array.AsReadOnly<VlcController>(allControllers);
        }

        public void selectBucket(BucketPosition pos)
        {
            SkeletonUtils.modifyUI(
                new ThreadStart(() =>
                {
                    try
                    {
                        boxes[(int)pos].Fill = new SolidColorBrush(Colors.Cyan);
                    }
                    catch (Exception) { }
            }));

            System.Timers.Timer myTimer = new System.Timers.Timer();
            myTimer.Elapsed += delegate { UnselectBucket(pos); };
            myTimer.Interval = 1000; // 1s
            myTimer.Start();
        }

        private void UnselectBucket(BucketPosition pos)
        {
            SkeletonUtils.modifyUI(
                new ThreadStart(() =>
                {
                    try
                    {
                        boxes[(int)pos].Fill = new SolidColorBrush((getController(pos).getVolume() > 100) ? Colors.Green : Colors.White);
                        progressBars[(int)pos].Value = getController(pos).getVolume();
                    }
                    catch (Exception) { }
                }));
        }

        public void setMovementValue(Movement mov, double val)
        {
            // Exact movement is 250
            // Close movement is 350
            if (val > 1000)
                val = 1000;
            lastMovementValues[(int)mov] = val;
            double min = val;
            foreach (Movement pos in Enum.GetValues(typeof(Movement)))
            {
                double actual = lastMovementValues[(int)pos];
                if (actual < min && actual > 0)
                    min = actual;
            }
            SkeletonUtils.modifyUI(
                new ThreadStart(() =>
                {
                    try
                    {
                        MovementDiff.Value = min;
                    }
                    catch (Exception) { }
            }));

            if (min < gesturePrecision + progressBarDelta)
            {
                SkeletonUtils.modifyUI(
                    new ThreadStart(() =>
                    {
                        try
                        {
                            MovementDiffClose.Value = min;
                        }
                        catch (Exception) { }
                    }));
            }
            else
            {
                SkeletonUtils.modifyUI(
                    new ThreadStart(() =>
                    {
                        try
                        {
                            MovementDiffClose.Value = gesturePrecision + progressBarDelta;
                        }
                        catch (Exception) { }
                    }));
            }
            if (min < gesturePrecision)
            {
                SkeletonUtils.modifyUI(
                    new ThreadStart(() =>
                    {
                        try
                        {
                            MovementPrecision.Value = min;
                        }
                        catch (Exception) { }
                    }));
            }
            else 
            {
                SkeletonUtils.modifyUI(
                    new ThreadStart(() =>
                    {
                        try
                        {
                            MovementPrecision.Value = gesturePrecision;
                        }
                        catch (Exception) { }
                    }));
            }
        }

        #endregion

        #region Voice Command Actions

        public void setUpRecognized()
        {
            SkeletonUtils.modifyUI(
                new ThreadStart(() =>
                {
                    try
                    {
                        voiceControl.Fill = new SolidColorBrush(Colors.Green);
                    }
                    catch (Exception) { }
                }));

            if (skeletonClient != null)
            {
                skeletonClient.unsubscribeAll();
                skeletonClient.subscribe(new SkeletonListener(skeletonCanvas));
                recordingStream = new CurrentRecording();
                skeletonClient.subscribe(recordingStream);
            }

            foreach (BucketPosition pos in Enum.GetValues(typeof(BucketPosition)))
            {
                if (getController(pos) != null)
                {
                    SkeletonUtils.modifyUI(
                        new ThreadStart(() =>
                        {
                            try
                            {
                                progressBars[(int)pos].Value = getController(pos).getVolume();
                            }
                            catch (Exception) { }
                        }));

                    SkeletonUtils.modifyUI(
                        new ThreadStart(() =>
                        {
                            try
                            {
                                boxes[(int)pos].Fill = new SolidColorBrush((getController(pos).getVolume() > 100) ? Colors.Green : Colors.White);
                            }
                            catch (Exception) { }
                        }));
                }
            }
            playing = false;
        }
        public void startRecognized()
        {
            if (playing)
            {
                // Will stop playing now
                playing = false;
                if (leftArmAnalyzer != null)
                    skeletonClient.unsubscribe(leftArmAnalyzer);
                if (rightArmAnalyzer != null)
                    skeletonClient.unsubscribe(rightArmAnalyzer);
                for (int i = 0; i < movements.Length; i++)
                    if (movements[i] != null)
                        skeletonClient.unsubscribe(movements[i]);
            }
            else
            {
                // Will start playing now
                playing = true;

                if (bucketsEnabled)
                {
                    if (leftArmAnalyzer != null)
                        skeletonClient.subscribe(leftArmAnalyzer);
                    if (rightArmAnalyzer != null)
                        skeletonClient.subscribe(rightArmAnalyzer);
                    SkeletonUtils.modifyUI(
                        new ThreadStart(() =>
                        {
                            try
                            {
                                voiceControl.Fill = new SolidColorBrush(Colors.Cyan);
                            }
                            catch (Exception) { }
                        }));
                }
                else
                {
                    SkeletonUtils.modifyUI(
                        new ThreadStart(() =>
                        {
                            try
                            {
                                voiceControl.Fill = new SolidColorBrush(Colors.Orange);
                            }
                            catch (Exception) { }
                        }));
                }
                for (int i = 0; i < movements.Length; i++)
                    if (movements[i] != null)
                        skeletonClient.subscribe(movements[i]);
            }
        }

        public void stopRecognized()
        {
            SkeletonUtils.modifyUI(
                new ThreadStart(() =>
                {
                    try
                    {
                        voiceControl.Fill = new SolidColorBrush(Colors.Red);
                    }
                    catch (Exception) { }
                }));

            if (skeletonClient != null)
            {
                if (leftArmAnalyzer != null)
                    skeletonClient.unsubscribe(leftArmAnalyzer);
                if (rightArmAnalyzer != null)
                    skeletonClient.unsubscribe(rightArmAnalyzer);
                for (int i = 0; i < movements.Length; i++)
                    if (movements[i] != null)
                        skeletonClient.unsubscribe(movements[i]);
            }
        }

        public void toggleBuckets()
        {
            if (bucketsEnabled)
            {
                // Deactivate them
                foreach (BucketPosition pos in Enum.GetValues(typeof(BucketPosition)))
                {
                    if (allControllers[(int)pos] != null)
                    {
                        SkeletonUtils.modifyUI(
                            new ThreadStart(() =>
                            {
                                try
                                {
                                    enablers[(int)pos].IsEnabled = false;
                                }
                                catch (Exception) { }
                            }));
                    }
                }
                if (leftArmAnalyzer != null)
                    skeletonClient.unsubscribe(leftArmAnalyzer);
                if (rightArmAnalyzer != null)
                    skeletonClient.unsubscribe(rightArmAnalyzer);

                SkeletonUtils.modifyUI(
                    new ThreadStart(() =>
                    {
                        try
                        {
                            voiceControl.Fill = new SolidColorBrush(Colors.Orange);
                        }
                        catch (Exception) { }
                    }));
                bucketsEnabled = false;
            }
            else
            {
                // Activate them
                foreach (BucketPosition pos in Enum.GetValues(typeof(BucketPosition)))
                {
                    if (allControllers[(int)pos] != null)
                    {
                        SkeletonUtils.modifyUI(
                            new ThreadStart(() =>
                            {
                                try
                                {
                                    enablers[(int)pos].IsEnabled = true;
                                }
                                catch (Exception) { }
                            }));
                    }
                }
                if (leftArmAnalyzer != null)
                    skeletonClient.subscribe(leftArmAnalyzer);
                if (rightArmAnalyzer != null)
                    skeletonClient.subscribe(rightArmAnalyzer);

                SkeletonUtils.modifyUI(
                    new ThreadStart(() =>
                    {
                        try
                        {
                            voiceControl.Fill = new SolidColorBrush(Colors.Cyan);
                        }
                        catch (Exception) { }
                    }));
                bucketsEnabled = true;
            }
        }

        #endregion

        #region Button Actions

        private void RecordingButton_Click(object sender, RoutedEventArgs e)
        {
            PlayButton.IsEnabled = true;
            SaveButton.IsEnabled = true;
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
                    recording.getTag(), Colors.Black, skeletonClient));
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
            }
        }
        #endregion

        #region Movement Button Click

        private void FasterLoadButton_Click(object sender, RoutedEventArgs e)
        {
            if (movements[(int)Movement.FASTER] != null)
            {
                SkeletonRecording movement = movements[(int)Movement.FASTER].getMovement();
                skeletonClient.subscribe(new RecordingReproducer(skeletonCanvas, movement, movement.getTag(),
                    Colors.Black, skeletonClient));
                return;
            }
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            if (dlg.ShowDialog() == true)
            {
                SkeletonRecording movement = new SkeletonRecording("fasterLoadedMovement");
                movement.loadFromFile(dlg.FileName);
                skeletonClient.subscribe(new RecordingReproducer(skeletonCanvas, movement, movement.getTag(),
                    Colors.Black, skeletonClient));

                // Loading movement to analyzer
                movements[(int)Movement.FASTER] = new MovementAnalyzer(movement, "FasterMovementAnalyzerStream", new FasterAction(this), Movement.FASTER, this);
                movements[(int)Movement.FASTER].setThreshold(movementThresholds[(int)Movement.FASTER]);
                skeletonClient.subscribe(movements[(int)Movement.FASTER]);
                fasterEnabler.Background = new SolidColorBrush(Colors.Red);
            }
        }

        private void SlowerLoadButton_Click(object sender, RoutedEventArgs e)
        {
            if (movements[(int)Movement.SLOWER] != null)
            {
                SkeletonRecording movement = movements[(int)Movement.SLOWER].getMovement();
                skeletonClient.subscribe(new RecordingReproducer(skeletonCanvas, movement, movement.getTag(),
                    Colors.Black, skeletonClient));
                return;
            }
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            if (dlg.ShowDialog() == true)
            {
                SkeletonRecording movement = new SkeletonRecording("slowerLoadedMovement");
                movement.loadFromFile(dlg.FileName);
                skeletonClient.subscribe(new RecordingReproducer(skeletonCanvas, movement, movement.getTag(),
                    Colors.Black, skeletonClient));

                // Loading movement to analyzer
                movements[(int)Movement.SLOWER] = new MovementAnalyzer(movement, "SlowerMovementAnalyzerStream",
                    new SlowerAction(Array.AsReadOnly<VlcController>(allControllers)), Movement.SLOWER, this);
                movements[(int)Movement.SLOWER].setThreshold(movementThresholds[(int)Movement.SLOWER]);
                skeletonClient.subscribe(movements[(int)Movement.SLOWER]);
                slowerEnabler.Background = new SolidColorBrush(Colors.Red);
            }
        }

        private void NormalSpeedLoadButton_Click(object sender, RoutedEventArgs e)
        {
            if (movements[(int)Movement.NORMAL] != null)
            {
                SkeletonRecording movement = movements[(int)Movement.NORMAL].getMovement();
                skeletonClient.subscribe(new RecordingReproducer(skeletonCanvas, movement, movement.getTag(),
                    Colors.Black, skeletonClient));
                return;
            }
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            if (dlg.ShowDialog() == true)
            {
                SkeletonRecording movement = new SkeletonRecording("normalSpeedLoadedMovement");
                movement.loadFromFile(dlg.FileName);
                skeletonClient.subscribe(new RecordingReproducer(skeletonCanvas, movement, movement.getTag(),
                    Colors.Black, skeletonClient));

                // Loading movement to analyzer
                movements[(int)Movement.NORMAL] = new MovementAnalyzer(movement, "NormalMovementAnalyzerStream",
                    new NormalSpeedAction(Array.AsReadOnly<VlcController>(allControllers)), Movement.NORMAL, this);
                movements[(int)Movement.NORMAL].setThreshold(movementThresholds[(int)Movement.NORMAL]);
                skeletonClient.subscribe(movements[(int)Movement.NORMAL]);

                normalEnabler.Background = new SolidColorBrush(Colors.Red);
            }
        }

        private void replayLoadedMovement()
        {
            skeletonClient.subscribe(new RecordingReproducer(skeletonCanvas, loadedMovement,
                loadedMovement.getTag(), Colors.Black, skeletonClient));
        }

        private void FasterButton_Click(object sender, RoutedEventArgs e)
        {
            if (movements[(int)Movement.FASTER] != null)
            {
                fasterEnabler.Background = new SolidColorBrush(Colors.Black);
                skeletonClient.unsubscribe(movements[(int)Movement.FASTER]);
                movements[(int)Movement.FASTER] = null;
            }
        }

        private void NormalButton_Click(object sender, RoutedEventArgs e)
        {
            if (movements[(int)Movement.NORMAL] != null)
            {
                normalEnabler.Background = new SolidColorBrush(Colors.Black);
                skeletonClient.unsubscribe(movements[(int)Movement.NORMAL]);
                movements[(int)Movement.NORMAL] = null;
            }
        }

        private void SlowerButton_Click(object sender, RoutedEventArgs e)
        {
            if (movements[(int)Movement.SLOWER] != null)
            {
                slowerEnabler.Background = new SolidColorBrush(Colors.Black);
                skeletonClient.unsubscribe(movements[(int)Movement.SLOWER]);
                movements[(int)Movement.SLOWER] = null;
            }
        }

        #endregion

        #region Menu items

        private void mnuServerip(object sender, RoutedEventArgs e)
        {
            String IP = Microsoft.VisualBasic.Interaction.InputBox("Enter the Server IP Address", "KPAT", DEFAULT_SERVER_IP);
            Match match = Regex.Match(IP, @"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}");
            if (match.Success)
            {
                setupServer(IP);
                System.Windows.MessageBox.Show("Succesfully Changed Server IP Address to: " + IP, "Success", MessageBoxButton.OK, MessageBoxImage.Asterisk);
            }
            else
            {
                System.Windows.MessageBox.Show("Error - You Have Provided an Invalid IP", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private void mnuBucketoffset(object sender, RoutedEventArgs e)
        {
            String resp = Microsoft.VisualBasic.Interaction.InputBox("Enter the bucket offset between 1 and 45", "KPAT",
                PositionAnalyzer.DEFAULT_OFFSET.ToString());
            int n;
            bool isNumeric = int.TryParse(resp, out n);
            if (isNumeric && n > 0 && n < 45)
            {
                if (rightArmAnalyzer != null)
                    rightArmAnalyzer.setOffset(n);
                if (leftArmAnalyzer != null)
                    leftArmAnalyzer.setOffset(n);
                bucketPrecision = n;
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
            if (isNumeric && n > 0 && n < PositionAnalyzer.MAX_MEDIA)
            {
                if (rightArmAnalyzer != null)
                    rightArmAnalyzer.setMediaSize(n);
                if (leftArmAnalyzer != null)
                    leftArmAnalyzer.setMediaSize(n);
                bucketMedia = n;
            }
            else
            {
                System.Windows.MessageBox.Show("Error - You Have Provided an Invalid Value", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void mnuFasterGestureprecision(object sender, RoutedEventArgs e)
        {
            mnuGestureprecision(movements[(int)Movement.FASTER]);
        }
        private void mnuNormalGestureprecision(object sender, RoutedEventArgs e)
        {
            mnuGestureprecision(movements[(int)Movement.NORMAL]);
        }
        private void mnuSlowerGestureprecision(object sender, RoutedEventArgs e)
        {
            mnuGestureprecision(movements[(int)Movement.SLOWER]);
        }

        private void mnuGestureprecision(MovementAnalyzer analyzer)
        {
            String resp = Microsoft.VisualBasic.Interaction.InputBox("Enter the Gesture Precision", "KPAT", MovementAnalyzer.DEFAULT_THRESHOLD.ToString());
            int n;
            bool isNumeric = int.TryParse(resp, out n);
            if (isNumeric)
            {
                analyzer.setThreshold(n);
            }
            else
            {
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

        #endregion

        #region Enabler Buttons

        private void UpperRightEnabler_Click(object sender, RoutedEventArgs e)
        {
            enablerClick(BucketPosition.RIGHT_UP);
        }

        private void MiddleRightEnabler_Click(object sender, RoutedEventArgs e)
        {
            enablerClick(BucketPosition.RIGHT_CENTER);
        }

        private void LowerRightEnabler_Click(object sender, RoutedEventArgs e)
        {
            enablerClick(BucketPosition.RIGHT_DOWN);
        }

        private void LowerLeftEnabler_Click(object sender, RoutedEventArgs e)
        {
            enablerClick(BucketPosition.LEFT_DOWN);
        }

        private void MiddleLeftEnabler_Click(object sender, RoutedEventArgs e)
        {
            enablerClick(BucketPosition.LEFT_CENTER);
        }

        private void UpperLeftEnabler_Click(object sender, RoutedEventArgs e)
        {
            enablerClick(BucketPosition.LEFT_UP);
        }

        private void enablerClick(BucketPosition pos)
        {
            if (allControllers[(int)pos] == null)
            {
                if (enable(pos))
                {
                    enablers[(int)pos].Background = new SolidColorBrush(Colors.Green);
                }
            }
            else
            {
                allControllers[(int)pos] = null;
                enablers[(int)pos].Background = new SolidColorBrush(Colors.Red);
            }
        }

        private bool enable(BucketPosition pos)
        {
            String IP = Microsoft.VisualBasic.Interaction.InputBox("Enter the IP and Port Ex: 192.168.0.1:9999", "KPAT", defaultVLCIp + ":" + defaultVLCPort);
            String[] IPs = IP.Split(':');
            if (IPs.Length != 2)
            {
                System.Windows.MessageBox.Show("Error - Invalid format", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            Match match = Regex.Match(IPs[0], @"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}");
            if (!match.Success)
            {
                System.Windows.MessageBox.Show("Error - Invalid format", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            int port;
            bool isNumeric = int.TryParse(IPs[1], out port);
            if (!isNumeric || port <= 0)
            {
                System.Windows.MessageBox.Show("Error - Invalid format", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // We now have a valid IP and port.
            try
            {
                VlcController newController = new VlcController(IPs[0], port);
                VlcController oldController = allControllers[(int)pos];
                allControllers[(int)pos] = newController;
                if (oldController != null)
                    oldController.shutdown();
            }
            catch (Exception)
            {
                System.Windows.MessageBox.Show("Error - Could not connect to a vlc instance with the data provided", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return false;
            }
            return true;
        }

        #endregion
    }
}
