using Microsoft.Kinect;
using System;
using System.Threading;
using System.Windows.Media;
using System.Collections.Generic;
using System.Windows;
using System.Diagnostics;

namespace WpfInterface
{
    class PositionAnalyzer
    {
        public const int MAX_MEDIA = 100;
        public const int DEFAULT_MEDIA = 10;
        public const int DEFAULT_OFFSET = 7;

        private const float bucket1 = 20;
        private const float bucket2 = 80;
        private const float bucket3 = 130;

        private int secsDelay = 1;
        private float offset;
        private int mediaSize;

        private float[] sumZ = new float[MAX_MEDIA];
        private DateTime[] lastUses = new DateTime[6];
        private bool[] volumeBucketStatus = new bool[6];

        private int cant = 0;
        private JointType joint;
        private DateTime lastUpdate = DateTime.Now;
        private bool right;
        private MainWindow container;

        public PositionAnalyzer(int mediaSize, JointType joint, float offset, bool right, MainWindow container)
        {
            this.mediaSize = mediaSize;
            this.joint = joint;
            this.offset = offset;
            this.container = container;
            for (int i = 0; i < 6; i++)
            {
                lastUses[i] = DateTime.Now;
                volumeBucketStatus[i] = true;
            }
            this.right = right;
        }

        public void setMediaSize(int value)
        {
            if(value > 0 && value < MAX_MEDIA)
                this.mediaSize = value;
        }

        public void setOffset(int offset)
        {
            if(offset > 0)
                this.offset = offset;
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
            for (int i = 0; i < mediaSize; i++)
            {
                mediaZ += sumZ[i];
            }
            mediaZ /= mediaSize;
            int plusIndex = 0;
            if (!right)
            {
                plusIndex = 3;
                mediaZ = (-mediaZ);
            }

            if (mediaZ > (bucket1 - offset) && mediaZ < (bucket1 + offset))
            {
                if (lastUses[0].AddSeconds(secsDelay) < DateTime.Now)
                {
                    Thread thread;
                    MainWindow.BucketPosition position = (right) ? MainWindow.BucketPosition.RIGHT_DOWN : MainWindow.BucketPosition.LEFT_DOWN; 
                    if (volumeBucketStatus[0 + plusIndex])
                    {
                        thread = new Thread(container.getController(position).noVolume);
                    }
                    else
                    {
                        thread = new Thread(container.getController(position).fullVolume);
                    }
                    thread.Start();
                    container.selectBucket(position);

                    lastUses[0] = DateTime.Now;
                    volumeBucketStatus[0 + plusIndex] = !volumeBucketStatus[0 + plusIndex];
                }
                return Colors.Cyan;
            }
            else if (mediaZ > (bucket2 - offset) && mediaZ < (bucket2 + offset))
            {
                if (lastUses[1].AddSeconds(secsDelay) < DateTime.Now)
                {
                    Thread thread;
                    MainWindow.BucketPosition position = (right) ? MainWindow.BucketPosition.RIGHT_CENTER : MainWindow.BucketPosition.LEFT_CENTER;
                    if (volumeBucketStatus[1 + plusIndex])
                    {
                        thread = new Thread(container.getController(position).noVolume);
                    }
                    else
                    {
                        thread = new Thread(container.getController(position).fullVolume);
                    }
                    thread.Start();
                    container.selectBucket(position);

                    lastUses[1] = DateTime.Now;
                    volumeBucketStatus[1 + plusIndex] = !volumeBucketStatus[1 + plusIndex];
                }
                return Colors.Purple;
            }
            else if (mediaZ > (bucket3 - offset) && mediaZ < (bucket3 + offset))
            {
                if (lastUses[2].AddSeconds(secsDelay) < DateTime.Now)
                {
                    Thread thread;
                    MainWindow.BucketPosition position = (right) ? MainWindow.BucketPosition.RIGHT_UP : MainWindow.BucketPosition.LEFT_UP;
                    if (volumeBucketStatus[2 + plusIndex])
                    {
                        thread = new Thread(container.getController(position).noVolume);
                    }
                    else
                    {
                        thread = new Thread(container.getController(position).fullVolume);
                    }
                    thread.Start();
                    container.selectBucket(position);

                    lastUses[2] = DateTime.Now;
                    volumeBucketStatus[2 + plusIndex] = !volumeBucketStatus[2 + plusIndex];
                }
                return Colors.Red;
            }


            return Colors.Brown;
        }
    }
}
