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

namespace WpfInterface
{
    class SkeletonListener : ClientListener
    {
        private SkeletonRecorder recorder = new SkeletonRecorder(recordingTag);
        private SkeletonRecorder replayer = new SkeletonRecorder(replayingTag);

        private SkeletonRecorder stream = new SkeletonRecorder(streamTag);
        private SkeletonRecorder bestReproduction;
        private double bestReproductionDiff = 1000;

        private bool recording = false;
        private bool playing = false;
        private bool replaying = false;
        private static string recordingTag = "recording";
        private static string replayingTag = "replaying";
        private static string streamTag = "stream";
        private static string bestReproductionTag = "bestReproduction";

        private static PositionAnalyzer leftArmAnalyzer;
        private static PositionAnalyzer rightArmAnalyzer;
        private Canvas skeletonCanvas;

        public SkeletonListener(Canvas skeletonCanvas)
        {
            this.skeletonCanvas = skeletonCanvas;
            //string[] leftArmIps = new string[] { "192.168 .0.41:8080", "192.168.0.36:8080", "192.168.0.68:8080" };
            string[] rightArmIps = new string[] { "127.0.0.1", "127.0.0.1", "127.0.0.1" };
            rightArmAnalyzer = new PositionAnalyzer(5, JointType.ElbowRight, 6, 10, false, rightArmIps, true);
            //leftArmAnalyzer = new PositionAnalyzer(10, JointType.ElbowLeft, 6, 10, false, leftArmIps, false);
        }

        public void dataArrived(object data)
        {
            Skeleton[] skeletons = (Skeleton[])data;

            Skeleton defaultSkeleton = skeletons.Where(s => s.TrackingState == SkeletonTrackingState.Tracked).FirstOrDefault();
            if (defaultSkeleton == null)
            {
                return;
            }

            //leftArmAnalyzer.checkPosition(defaultSkeleton);
            Color color = rightArmAnalyzer.checkPosition(defaultSkeleton);
            //if(color != Colors.Peru)
            //    SkeletonUtils.DrawSkeleton(skeletonCanvas, defaultSkeleton, color, "posskells");

            if (recording)
            {
                recorder.add(defaultSkeleton);
            }

            if (playing)
            {
                Application.Current.Dispatcher.BeginInvoke(new ThreadStart(() => DrawingUtils.deleteElements(skeletonCanvas, recordingTag)));
                //DrawingUtils.deleteElements(skeletonCanvas, recordingTag);
                //SkeletonUtils.DrawSkeleton(skeletonCanvas, recorder.next(), Colors.Black, recordingTag);
                Application.Current.Dispatcher.BeginInvoke(new ThreadStart(() => SkeletonUtils.DrawSkeleton(skeletonCanvas, recorder.next(), Colors.Black, recordingTag)));
            }

            if (replaying)
            {
                //DrawingUtils.deleteElements(skeletonCanvas, replayingTag);
                //SkeletonUtils.DrawSkeleton(skeletonCanvas, replayer.next(), Colors.Blue, replayingTag);
                Application.Current.Dispatcher.BeginInvoke(new ThreadStart(() => DrawingUtils.deleteElements(skeletonCanvas, replayingTag)));
                Application.Current.Dispatcher.BeginInvoke(new ThreadStart(() => SkeletonUtils.DrawSkeleton(skeletonCanvas, replayer.next(), Colors.Blue, replayingTag)));
                stream.add(defaultSkeleton);
                if (stream.size() == replayer.size())
                {
                    float diff = SkeletonUtils.difference(stream, replayer);
                    if (diff < 170)
                    {
                        leftArmAnalyzer.fullVolume();
                        rightArmAnalyzer.fullVolume();
                    }
                    if (bestReproductionDiff > diff)
                    {
                        bestReproductionDiff = diff;
                    }
                }
            }
            Application.Current.Dispatcher.BeginInvoke(new ThreadStart(() => 
                DrawingUtils.deleteElements(skeletonCanvas, defaultSkeleton.TrackingId.ToString())));
            Application.Current.Dispatcher.BeginInvoke(new ThreadStart(() => 
                SkeletonUtils.DrawSkeleton(skeletonCanvas, defaultSkeleton, Colors.Cyan, defaultSkeleton.TrackingId.ToString())));
        }
    }
}
