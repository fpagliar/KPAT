using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;

namespace WpfInterface
{
    class ArmAnalyzerListener : ClientListener
    {
        private PositionAnalyzer analyzer;

        public ArmAnalyzerListener(int mediaSize, JointType joint, float delta, List<VlcController> vlcControllers, bool right, 
            Dictionary<int, System.Windows.Controls.TextBox> UIControls)
        {
            analyzer = new PositionAnalyzer(mediaSize, joint, delta, vlcControllers, right, UIControls);
        }


        public void dataArrived(object data)
        {
            Skeleton skeleton = SkeletonUtils.defaultSkeleton(data);
            if (skeleton == null)
            {
                return;
            }
            analyzer.checkPosition(skeleton);

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
