using Microsoft.Kinect;
using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Media;
namespace WpfInterface
{
    class PositionAnalyzer
    {

        private int secsDelay = 1;

        private float[] sumZ;
        private DateTime[] lastUses = new DateTime[6];
        private string[] ipaddresses;
        private int mediaSize;
        
        private float bucket1 = 20;
        private float bucket2 = 80;
        private float bucket3 = 130;
        private float offset = 7;

        private int cant = 0;
        private JointType joint;
        private int buckets;
        private float delta;
        private bool bucketSpacing;
        private DateTime lastUpdate = DateTime.Now;
        private bool right;

        public PositionAnalyzer(int mediaSize, JointType joint, int buckets, float delta, bool bucketSpacing, string[] ipaddresses, bool right)
        {
            sumZ = new float[mediaSize];
            this.mediaSize = mediaSize;
            this.joint = joint;
            this.buckets = buckets;
            this.delta = delta;
            this.bucketSpacing = bucketSpacing;
            this.ipaddresses = ipaddresses;
            for (int i = 0; i < 6; i++)
            {
                lastUses[i] = DateTime.Now;
            }
            this.right = right;
        }

        public Color checkPosition(Skeleton skeleton)
        {
            if (right)
            {
                if (Math.Abs(skeleton.Joints[JointType.WristRight].Position.X) - Math.Abs(skeleton.Joints[JointType.ElbowRight].Position.X) < 0)
                {
                    return Colors.Peru;
                }
            }
            else
            {
                if (Math.Abs(skeleton.Joints[JointType.WristLeft].Position.X) - Math.Abs(skeleton.Joints[JointType.ElbowLeft].Position.X) < 0)
                {
                    return Colors.Peru;
                }
            }

            if (lastUpdate.AddSeconds(secsDelay) < DateTime.Now)
            {
                cant = 0;
            }
            lastUpdate = DateTime.Now;

            // START POSITION ANALYSIS
            double xRot;
            double yRot;
            double zRot;
            SkeletonUtils.ExtractRotationInDegrees(skeleton.BoneOrientations[joint].AbsoluteRotation.Quaternion,
                out xRot, out yRot, out zRot);

            sumZ[cant % mediaSize] = (float)zRot;
            cant++;

            if (cant < mediaSize)
            {
                return Colors.Peru;
            }

            float mediaZ = 0;
            for (int i = 0; i < sumZ.Length; i++)
            {
                mediaZ += sumZ[i];
            }
            mediaZ /= sumZ.Length;
            if (!right)
            {
                mediaZ = (-mediaZ);
            }
            //Debug.WriteLine(mediaZ);

            if (mediaZ > (bucket1 - offset) && mediaZ < (bucket1 + offset))
            {
                if (lastUses[0].AddSeconds(secsDelay) < DateTime.Now)
                {
                    Thread thread = new Thread(new VlcController(ipaddresses[0]).run);
                    thread.Start();
                    lastUses[0] = DateTime.Now;
                }
                return Colors.Cyan;
            }
            else if (mediaZ > (bucket2 - offset) && mediaZ < (bucket2 + offset))
            {
                if (lastUses[1].AddSeconds(secsDelay) < DateTime.Now)
                {
                    Thread thread = new Thread(new VlcController(ipaddresses[1]).run);
                    thread.Start();
                    lastUses[1] = DateTime.Now;
                }
                return Colors.Purple;
            }
            else if (mediaZ > (bucket3 - offset) && mediaZ < (bucket3 + offset))
            {
                if (lastUses[2].AddSeconds(secsDelay) < DateTime.Now)
                {
                    Thread thread = new Thread(new VlcController(ipaddresses[2]).run);
                    thread.Start();
                    lastUses[2] = DateTime.Now;
                }
                return Colors.Red;
            }
            return Colors.Brown;
        }

    }
}
