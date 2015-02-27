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
namespace WpfInterface
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
        private static TcpClient cameraClient;
        private static TcpClient skeletonClient;

        public MainWindow()
        {
            InitializeComponent();

            //string[] leftArmIps = new string[] { "192.168.0.41:8080", "192.168.0.36:8080", "192.168.0.68:8080" };
            //string[] rightArmIps = new string[] { "192.168.0.33:8080", "192.168.0.37:8080", "192.168.0.34:8080" };
            //rightArmAnalyzer = new PositionAnalyzer(5, JointType.ElbowRight, 6, 10, false, rightArmIps, true);
            //leftArmAnalyzer = new PositionAnalyzer(10, JointType.ElbowLeft, 6, 10, false, leftArmIps, false);

            skeletonClient = new TcpClient("192.168.0.81", 8081, new SkeletonListener(skeletonCanvas));
            //cameraClient = new TcpClient("192.168.0.81", 8082, new CameraListener(MainImage));

            addTrackingJoints();
            //Thread thread = new Thread(new ThreadStart(cameraClient.runLoop));
            //thread.Start();
            Thread other = new Thread(new ThreadStart(skeletonClient.runLoop));
            other.Start();

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
            //loop();
        }

        #region Button Actions

        private void RecordingButton_Click(object sender, RoutedEventArgs e)
        {
            //if (recording)
            //{
            //    // I was recording, I will now stop it
            //    recording = false;
            //    RecordingButton.Content = "Start recording";
            //}
            //else
            //{
            //    // I was not recording, I will now start recording
            //    recording = true;
            //    recorder = new SkeletonRecorder(recordingTag);
            //    RecordingButton.Content = "Stop recording";
            //}
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            //if (playing)
            //{
            //    // I was playing, I will now stop it
            //    playing = false;
            //    recorder.restart();
            //    PlayButton.Content = "Play";
            //}
            //else
            //{
            //    // I was stopped, I will now start playing
            //    playing = true;
            //    recording = false;
            //    PlayButton.Content = "Stop";
            //}
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            //Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            //if (dlg.ShowDialog() == true)
            //{
            //    recorder.saveToFile(dlg.FileName);
            //}

        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            //Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            //if (dlg.ShowDialog() == true)
            //{
            //    replayer.loadFromFile(dlg.FileName);
            //    replaying = true;
            //    stream = new SkeletonRecorder(streamTag, replayer.size());
            //}
        }

        private void CompareButton_Click(object sender, RoutedEventArgs e)
        {
            //bool ans = SkeletonUtils.match(recorder, replayer);
            //float diff = SkeletonUtils.difference(recorder, replayer);
            //speechRejected.Text = "ANS: " + ans + " differece: " + diff + " in " + recorder.size() + " frames" +
            //    " vs " + replayer.size() + " frames";
        }

        private void SaveBestButton_Click(object sender, RoutedEventArgs e)
        {
            //Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            //if (dlg.ShowDialog() == true)
            //{
            //    bestReproduction.saveToFile(dlg.FileName);
            //}
        }

        #endregion


        private void Slider_MouseUp(object sender, System.EventArgs e)
        {
            Debug.WriteLine("asd");
        }
        private void Slider_MouseDown(object sender, MouseEventArgs e)
        {
            Debug.WriteLine("abf");
        }

        private void Slider_ValueChanged_2(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

            //timer.Interval = 1000;
           
            //// ... Get Slider reference.
            //var slider = sender as Slider;
            
            //// ... Get Value.
            //double value = slider.Value;
                
            //if (sensor != null)
            //{
            //    timer.Start();
            //    timer.Tick += timer_Tick;
                    
            //    // ... Set Window Title.
            //    this.Title = "Value: " + value.ToString("0.0") + "/" + slider.Maximum;
            //    try
            //    {
            //        sensor.ElevationAngle = Convert.ToInt32(value);
            //    }
            //    catch (Exception)
            //    {
            //    }
            //    finally {
            //        sliderAngle.IsEnabled = false;
            //    }
                
            //}

        }

        void timer_Tick(object sender, System.EventArgs e)
        {
            sliderAngle.IsEnabled = true;
            timer.Stop();
        }

      
    }
}
