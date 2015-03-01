using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;

namespace WpfInterface
{
    class MovementAnalyzer : ClientListener
    {
        private SkeletonRecording movement;
        private SkeletonRecording stream;
        private int threshold = 170;

        public MovementAnalyzer(SkeletonRecording movement, string tag)
        {
            stream = new SkeletonRecording(tag, movement.size());
            this.movement = movement;
        }

        public void dataArrived(object data)
        {
            Skeleton skeleton = SkeletonUtils.defaultSkeleton(data);
            if (skeleton == null)
            {
                return;
            }

            stream.add(skeleton);
            if (stream.size() == movement.size())
            {
                float diff = SkeletonUtils.difference(stream, movement);
                if (diff < threshold)
                {
                    // Run action
                }
            }
        }

    }
}
