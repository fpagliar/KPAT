﻿using Microsoft.Kinect;
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
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;
using System.Timers;

namespace KinectServer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private static KinectSensor sensor;
        private static VoiceController voiceController;
        private static TcpServer skeletonServer = new TcpServer(8081);
        private static TcpServer cameraServer = new TcpServer(8082);
        private static TcpServer voiceServer = new TcpServer(8083);

        private readonly object movementLock = new object();

        public MainWindow()
        {
            InitializeComponent();
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

                List<String> phrases = loadPhrases();

                sensor.Start();
                sensor.ElevationAngle = 10;

                voiceController.StartRecognition(sensor, phrases);
            }
            else
            {
                System.Windows.MessageBox.Show("Could not connect with the Kinect Sensor. Check it is correctly connected and run again", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            }
            Application.Current.MainWindow.Closing += new CancelEventHandler(shutdown);
        }

        private List<String> loadPhrases()
        {
            try
            {
                string[] lines = System.IO.File.ReadAllLines("phrases.properties");
                return new List<string>(lines);
            }
            catch (Exception)
            {
                System.Windows.MessageBox.Show("Could not load phrases.properties file, will load default phrases", "Information", MessageBoxButton.OK, 
                    MessageBoxImage.Information);
                List<String> phrases = new List<string>();
                phrases.Add("SET UP");
                phrases.Add("STOP");
                phrases.Add("PAUSE");
                phrases.Add("PLAY");
                phrases.Add("BUCKETS");
                return phrases;
            }
        }

        void Recognizer_SpeechRecognized(object sender, Microsoft.Speech.Recognition.SpeechRecognizedEventArgs e)
        {
            voiceServer.informListeners(e.Result.Confidence + "#" + e.Result.Text);
        }

        void Sensor_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (var frame = e.OpenColorImageFrame())
            {
                if (frame != null)
                {
                    BitmapSource source = WindowUtils.ToBitmap(frame);
                    MainImage.Source = source;
                    int _width = (int)source.Width;
                    int _height = (int)source.Height;
                    byte[] _pixels = new byte[_width * _height * WindowUtils.BYTES_PER_PIXEL];
                    frame.CopyPixelDataTo(_pixels);
                    List<Object> data = new List<object>();
                    data.Add(_width);
                    data.Add(_height);
                    data.Add(_pixels);
                    cameraServer.informListeners(data);
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
            skeletonServer.informListeners(skeletons);
        }

        public void shutdown(object sender, CancelEventArgs e)
        {
            if (voiceController != null)
            {
                voiceController.StopRecognition();
            }
            if (sensor != null)
            {
                sensor.Stop();
                sensor.Dispose();
            }
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            lock (movementLock)
            {
                if (!cameraAngleSlider.IsEnabled)
                {
                    return;
                }
                if (sensor != null)
                {
                    cameraAngleSlider.IsEnabled = false;
                    try
                    {
                        sensor.ElevationAngle = (int)e.NewValue;
                    }
                    catch (Exception) { } 

                    System.Timers.Timer myTimer = new System.Timers.Timer();
                    myTimer.Elapsed += new ElapsedEventHandler(ReenableSlider);
                    myTimer.Interval = 2000; // 2s
                    myTimer.Start();
                }
            }
        }

        private void ReenableSlider(object source, ElapsedEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke(
                new ThreadStart(() =>
                {
                    try
                    {
                        cameraAngleSlider.IsEnabled = true;
                    }
                    catch (Exception) { }
                }));
            //Application.Current.Dispatcher.BeginInvoke(new ThreadStart(() => cameraAngleSlider.IsEnabled = true));
        }

    }
}
