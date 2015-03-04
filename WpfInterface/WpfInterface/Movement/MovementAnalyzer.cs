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
        public const int DEFAULT_THRESHOLD = 170;


        private SkeletonRecording movement;
        private SkeletonRecording stream;
        private int threshold = DEFAULT_THRESHOLD;
        private Action action;

        public MovementAnalyzer(SkeletonRecording movement, string tag, Action action)
        {
            stream = new SkeletonRecording(tag, movement.size());
            this.movement = movement;
            this.action = action;
        }

        public void setThreshold(int value)
        {
            if (value > 0)
                this.threshold = value;
        }

        public SkeletonRecording getMovement()
        {
            return movement;
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
                    action.perform();
                }
            }
        }

    }
}
