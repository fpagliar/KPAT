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
                //voiceController.RecognitionConfidence = 0.50;

                List<String> phrases = new List<string>();
                phrases.Add("SET UP");    // TODO: properties file
                phrases.Add("STOP");    // TODO: properties file
                phrases.Add("PAUSE");   // TODO: properties file
                phrases.Add("START");   // TODO: properties file
                phrases.Add("BUCKETS"); // TODO: properties file

                sensor.Start();
                sensor.ElevationAngle = 10;

                voiceController.StartRecognition(sensor, phrases);
            }
            Application.Current.MainWindow.Closing += new CancelEventHandler(shutdown);
        }

        void Recognizer_SpeechRecognized(object sender, Microsoft.Speech.Recognition.SpeechRecognizedEventArgs e)
        {
            //if (e.Result.Confidence >= voiceController.RecognitionConfidence)
            //{
            voiceServer.informListeners(e.Result.Confidence + "#" + e.Result.Text);
            //}
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
            if (sensor != null)
            {
                sensor.Stop();
            }
            if (voiceController != null)
            {
                voiceController.StopRecognition();
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
                    catch (Exception) { } // It may fail, fuck it.

                    System.Timers.Timer myTimer = new System.Timers.Timer();
                    myTimer.Elapsed += new ElapsedEventHandler(ReenableSlider);
                    myTimer.Interval = 2000; // 2s
                    myTimer.Start();
                }
            }
        }

        private void ReenableSlider(object source, ElapsedEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke(new ThreadStart(() => cameraAngleSlider.IsEnabled = true));
        }

    }
}
