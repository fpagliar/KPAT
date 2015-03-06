using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;
using System.Diagnostics;

namespace WpfInterface
{
    class MovementAnalyzer : ClientListener
    {
        public const int DEFAULT_THRESHOLD = 210;


        private SkeletonRecording movement;
        private SkeletonRecording stream;
        private int threshold = DEFAULT_THRESHOLD;
        private Action action;
        private DateTime lastUse;

        public MovementAnalyzer(SkeletonRecording movement, string tag, Action action)
        {
            stream = new SkeletonRecording(tag, movement.size());
            this.movement = movement;
            this.action = action;
            lastUse = DateTime.Now;
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
                if (lastUse.AddSeconds(5) < DateTime.Now)
                {
                    if (diff < threshold)
                    {
                        Debug.WriteLine("Gesture Detected");
                        action.perform();
                        lastUse = DateTime.Now;
                    }
                }
            }
        }

    }
}
