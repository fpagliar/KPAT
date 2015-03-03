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

        private float[] sumZ = new float[MAX_MEDIA];
        private DateTime[] lastUses = new DateTime[6];
        private bool[] volumeBucketStatus = new bool[6];
        private IReadOnlyList<VlcController> vlcControllers;
        private int mediaSize;

        private int cant = 0;
        private JointType joint;
        private DateTime lastUpdate = DateTime.Now;
        private bool right;
        private Dictionary<int, System.Windows.Controls.TextBox> UIControls;

        public PositionAnalyzer(int mediaSize, JointType joint, float offset, IReadOnlyList<VlcController> vlcControllers, bool right, 
            Dictionary<int, System.Windows.Controls.TextBox> UIControls)
        {
            this.mediaSize = mediaSize;
            this.joint = joint;
            this.offset = offset;
            this.vlcControllers = vlcControllers;
            this.UIControls = UIControls;
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

        private void stop()
        {
            for (int i = 0; i < vlcControllers.Count; i++)
            {
                Thread thread = new Thread(vlcControllers[i].stop);
                thread.Start();
            }

        }

        private void fullVolume()
        {
            for (int i = 0; i < vlcControllers.Count; i++)
            {
                Thread thread = new Thread(vlcControllers[i].fullVolume);
                thread.Start();
            }
        }

        private void noVolume()
        {
            for (int i = 0; i < vlcControllers.Count; i++)
            {
                Thread thread = new Thread(vlcControllers[i].noVolume);
                thread.Start();
            }
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
            System.Windows.Controls.TextBox t = null;

            if (mediaZ > (bucket1 - offset) && mediaZ < (bucket1 + offset))
            {
                if (lastUses[0].AddSeconds(secsDelay) < DateTime.Now)
                {
                    Thread thread;
                    if (volumeBucketStatus[0 + plusIndex])
                    {

                        thread = new Thread(vlcControllers[(int)MainWindow.BucketPosition.LEFT_DOWN + plusIndex].noVolume);
                    }
                    else
                    {
                        thread = new Thread(vlcControllers[(int)MainWindow.BucketPosition.LEFT_DOWN + plusIndex].fullVolume);
                    }
                    thread.Start();
                    if (right)
                    {
                        UIControls.TryGetValue(1, out t);
                    }
                    else
                    {
                        UIControls.TryGetValue(0, out t);
                    }
                    lastUses[0] = DateTime.Now;
                    volumeBucketStatus[0 + plusIndex] = !volumeBucketStatus[0 + plusIndex];

                }
                setCurrentBucket(t, 0 + plusIndex);
                return Colors.Cyan;
            }
            else if (mediaZ > (bucket2 - offset) && mediaZ < (bucket2 + offset))
            {
                if (lastUses[1].AddSeconds(secsDelay) < DateTime.Now)
                {
                    Thread thread;
                    if (volumeBucketStatus[1 + plusIndex])
                    {
                        thread = new Thread(vlcControllers[(int)MainWindow.BucketPosition.LEFT_CENTER + plusIndex].noVolume);
                    }
                    else
                    {
                        thread = new Thread(vlcControllers[(int)MainWindow.BucketPosition.LEFT_CENTER + plusIndex].fullVolume);
                    }
                    thread.Start();
                    if (right)
                    {
                        UIControls.TryGetValue(3, out t);
                    }
                    else
                    {
                        UIControls.TryGetValue(2, out t);
                    }
                    lastUses[1] = DateTime.Now;
                    volumeBucketStatus[1 + plusIndex] = !volumeBucketStatus[1 + plusIndex];

                }
                setCurrentBucket(t, 1 + plusIndex);
                return Colors.Purple;
            }
            else if (mediaZ > (bucket3 - offset) && mediaZ < (bucket3 + offset))
            {
                if (lastUses[2].AddSeconds(secsDelay) < DateTime.Now)
                {
                    Thread thread;
                    if (volumeBucketStatus[2 + plusIndex])
                    {
                        thread = new Thread(vlcControllers[(int)MainWindow.BucketPosition.LEFT_UP + plusIndex].noVolume);
                    }
                    else
                    {
                        thread = new Thread(vlcControllers[(int)MainWindow.BucketPosition.LEFT_UP + plusIndex].fullVolume);
                    }
                    thread.Start();
                    if (right)
                    {
                        UIControls.TryGetValue(5, out t);
                    }
                    else
                    {
                        UIControls.TryGetValue(4, out t);
                    }
                    lastUses[2] = DateTime.Now;
                    volumeBucketStatus[2 + plusIndex] = !volumeBucketStatus[2 + plusIndex];

                }
                setCurrentBucket(t, 2 + plusIndex);
                return Colors.Red;
            }


            return Colors.Brown;
        }

        private void setCurrentBucket(System.Windows.Controls.TextBox t, int index)
        {
            if (t != null)
            {
                Application.Current.Dispatcher.BeginInvoke(new ThreadStart(() =>
               t.BorderThickness = new Thickness(5, 5, 15, 20)));
                Application.Current.Dispatcher.BeginInvoke(new ThreadStart(() =>
              t.Text = volumeBucketStatus[index] == true ? "Volume ON" : "Volume OFF"));
                foreach (System.Windows.Controls.TextBox c in UIControls.Values)
                {
                    if (c.Equals(t))
                        continue;
                    Application.Current.Dispatcher.BeginInvoke(new ThreadStart(() =>
                   c.BorderThickness = new Thickness(5, 5, 5, 5)));
                    Application.Current.Dispatcher.BeginInvoke(new ThreadStart(() =>
                   c.Text = volumeBucketStatus[index] == true ? "Volume ON" : "Volume OFF"));
                }
            }
        }

    }
}
