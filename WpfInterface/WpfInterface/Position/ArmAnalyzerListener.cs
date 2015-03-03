using System;
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

        public ArmAnalyzerListener(int mediaSize, JointType joint, float delta, IReadOnlyList<VlcController> vlcControllers, 
            bool right, Dictionary<int, System.Windows.Controls.TextBox> UIControls, Canvas skelcanvas)
        {
            analyzer = new PositionAnalyzer(mediaSize, joint, delta, vlcControllers, right, UIControls);
            this.skelcanvas = skelcanvas;
        }


        public void dataArrived(object data)
        {
            Skeleton skeleton = SkeletonUtils.defaultSkeleton(data);
            if (skeleton == null)
            {
                return;
            }
            Color color = analyzer.checkPosition(skeleton);
            SkeletonUtils.redraw(skelcanvas, skeleton, "armdsdfanalyzer", color);
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
