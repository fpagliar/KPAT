﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;
using System.Windows.Media;
using System.Windows;
using System.Threading;
using System.Windows.Controls;

namespace WpfInterface
{
    class ArmAnalyzerListener : ClientListener
    {
        private PositionAnalyzer analyzer;
        private Canvas skelcanvas;

        public ArmAnalyzerListener(int mediaSize, float delta, bool right, MainWindow container)
        {
            analyzer = new PositionAnalyzer(mediaSize, delta, right, container);
            this.skelcanvas = container.getCanvas();
        }

        public void dataArrived(object data)
        {
            Skeleton skeleton = SkeletonUtils.defaultSkeleton(data);
            if (skeleton == null)
            {
                return;
            }
            Color color = analyzer.checkPosition(skeleton);
            // Use this instead of redraw to make the image that shows the buckets
            //Application.Current.Dispatcher.BeginInvoke(new ThreadStart(() =>
            //    SkeletonUtils.DrawSkeleton(skelcanvas, skeleton, color, "armdsdfanalyzer")));
            //SkeletonUtils.redraw(skelcanvas, skeleton, "armdsdfanalyzer", Colors.Cyan);
        }

        public void setMediaSize(int value)
        {
            analyzer.setMediaSize(value);
        }

        public void setOffset(int value)
        {
            analyzer.setOffset(value);
        }

    }
}
