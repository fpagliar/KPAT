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

        private const float bucket1 = 38;
        private const float bucket2 = 90;
        private const float bucket3 = 125;

        private int secsDelay = 1;
        private float offset;
        private int mediaSize;
        private bool right;
        private MainWindow container;

        private float[] sumZ = new float[MAX_MEDIA];
        private DateTime[] lastUses = new DateTime[6];

        private DateTime lastUpdate = DateTime.Now;
        private int cant = 0;

        public PositionAnalyzer(int mediaSize, float offset, bool right, MainWindow container)
        {
            this.mediaSize = mediaSize;
            this.offset = offset;
            this.container = container;
            for (int i = 0; i < 6; i++)
            {
                lastUses[i] = DateTime.Now;
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
                if (Math.Abs(skeleton.Joints[JointType.WristRight].Position.X) - Math.Abs(skeleton.Joints[JointType.ElbowRight].Position.X) < 0.1)
                {
                    return Colors.Peru;
                }
            }
            else
            {
                if (Math.Abs(skeleton.Joints[JointType.WristLeft].Position.X) - Math.Abs(skeleton.Joints[JointType.ElbowLeft].Position.X) < 0.1)
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
            //double xRot;
            //double yRot;
            //double zRot;
            //SkeletonUtils.ExtractRotationInDegrees(skeleton.BoneOrientations[joint].AbsoluteRotation.Quaternion, out xRot, out yRot, out zRot);
            JointType joint1;
            JointType joint2;
            if (right)
            {
                joint1 = JointType.ElbowRight;
                joint2 = JointType.WristRight;
            }
            else
            {
                joint1 = JointType.ElbowLeft;
                joint2 = JointType.WristLeft;
            }
            float diffXOrig = skeleton.Joints[joint1].Position.X - skeleton.Joints[joint2].Position.X;
            float diffYOrig = skeleton.Joints[joint1].Position.Y - skeleton.Joints[joint2].Position.Y;
            double zRot = Math.Atan(diffYOrig / ((right) ? diffXOrig : -diffXOrig)) * 180.0 / Math.PI + 90;

            sumZ[cant % mediaSize] = (float)zRot;
            cant++;

            if (cant < mediaSize)
            {
                return Colors.Peru;
            }

            float mediaZ = 0;
            mediaZ = getAverage(mediaZ);

            if (mediaZ > (bucket1 - offset) && mediaZ < (bucket1 + offset))
            {
                if (lastUses[0].AddSeconds(secsDelay) < DateTime.Now)
                {
                    MainWindow.BucketPosition position = (right) ? MainWindow.BucketPosition.RIGHT_DOWN : MainWindow.BucketPosition.LEFT_DOWN; 
                    VlcController controller = container.getController(position);
                    if (controller != null)
                    {
                        new Thread(container.getController(position).toggleVolume).Start();
                        container.selectBucket(position);
                    }
                    lastUses[0] = DateTime.Now;
                }
                return Colors.Cyan;
            }
            else if (mediaZ > (bucket2 - offset) && mediaZ < (bucket2 + offset))
            {
                if (lastUses[1].AddSeconds(secsDelay) < DateTime.Now)
                {
                    MainWindow.BucketPosition position = (right) ? MainWindow.BucketPosition.RIGHT_CENTER : MainWindow.BucketPosition.LEFT_CENTER;
                    VlcController controller = container.getController(position);
                    if (controller != null)
                    {
                        new Thread(container.getController(position).toggleVolume).Start();
                        container.selectBucket(position);
                    }
                    lastUses[1] = DateTime.Now;
                }
                return Colors.Purple;
            }
            else if (mediaZ > (bucket3 - offset) && mediaZ < (bucket3 + offset))
            {
                if (lastUses[2].AddSeconds(secsDelay) < DateTime.Now)
                {
                    MainWindow.BucketPosition position = (right) ? MainWindow.BucketPosition.RIGHT_UP : MainWindow.BucketPosition.LEFT_UP;
                    VlcController controller = container.getController(position);
                    if (controller != null)
                    {
                        new Thread(container.getController(position).toggleVolume).Start();
                        container.selectBucket(position);
                    }
                    lastUses[2] = DateTime.Now;
                }
                return Colors.Red;
            }
            return Colors.Brown;
        }

        private float getAverage(float mediaZ)
        {
            for (int i = 0; i < mediaSize; i++)
            {
                mediaZ += sumZ[i];
            }
            mediaZ /= mediaSize;
            return mediaZ;
        }
    }
}
