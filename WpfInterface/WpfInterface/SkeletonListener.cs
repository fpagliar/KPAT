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

        private static string drawingTag = "basicUISkeleton";
        private static Color drawingColor = Colors.Cyan;

        private Canvas skeletonCanvas;

        public SkeletonListener(Canvas skeletonCanvas)
        {
            this.skeletonCanvas = skeletonCanvas;
        }

        public void dataArrived(object data)
        {
            Skeleton defaultSkeleton = SkeletonUtils.defaultSkeleton(data);

            if (defaultSkeleton == null)
            {
                return;
            }

            SkeletonUtils.redraw(skeletonCanvas, defaultSkeleton, drawingTag, drawingColor);

            //if (replaying)
            //{
            //    //DrawingUtils.deleteElements(skeletonCanvas, replayingTag);
            //    //SkeletonUtils.DrawSkeleton(skeletonCanvas, replayer.next(), Colors.Blue, replayingTag);
            //    Application.Current.Dispatcher.BeginInvoke(new ThreadStart(() => DrawingUtils.deleteElements(skeletonCanvas, replayingTag)));
            //    Application.Current.Dispatcher.BeginInvoke(new ThreadStart(() => SkeletonUtils.DrawSkeleton(skeletonCanvas, replayer.next(), Colors.Blue, replayingTag)));
            //    stream.add(defaultSkeleton);
            //    if (stream.size() == replayer.size())
            //    {
            //        float diff = SkeletonUtils.difference(stream, replayer);
            //        if (diff < 170)
            //        {
            //            leftArmAnalyzer.fullVolume();
            //            rightArmAnalyzer.fullVolume();
            //        }
            //        if (bestReproductionDiff > diff)
            //        {
            //            bestReproductionDiff = diff;
            //        }
            //    }
            //}

        }
    }
}
