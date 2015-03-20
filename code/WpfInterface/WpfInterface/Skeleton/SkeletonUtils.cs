using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using Microsoft.Kinect;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using System.Threading;

namespace WpfInterface
{
    static class SkeletonUtils
    {
        /// <summary>
        /// Values used in the match comparison to determine when a movement is or isn't a match.
        /// </summary>
        private static float delta = 200f;
        private static float deltaAccum = 200f;

        private static int screenWidth = 700;
        private static int screenHeight = 420;

        /// <summary>
        /// Lists the joints that will be used in the movement comparisons.
        /// </summary>
        private static List<JointType> targetJoints = new List<JointType>();

        /// <summary>
        /// Used to set the connections that will be drawn to form the skeleton.
        /// </summary>
        private static List<Tuple<JointType, JointType>> connections = setUpConnections();

        #region Set Up

        private static List<Tuple<JointType, JointType>> setUpConnections()
        {
            List<Tuple<JointType, JointType>> ans = new List<Tuple<JointType, JointType>>();

            // Center
            ans.Add(new Tuple<JointType, JointType>(JointType.Head, JointType.ShoulderCenter));
            ans.Add(new Tuple<JointType, JointType>(JointType.ShoulderCenter, JointType.Spine));
            ans.Add(new Tuple<JointType, JointType>(JointType.Spine, JointType.HipCenter));

            // Left Shoulder
            ans.Add(new Tuple<JointType, JointType>(JointType.ShoulderCenter, JointType.ShoulderLeft));
            ans.Add(new Tuple<JointType, JointType>(JointType.ShoulderLeft, JointType.ElbowLeft));
            ans.Add(new Tuple<JointType, JointType>(JointType.ElbowLeft, JointType.WristLeft));
            ans.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.HandLeft));

            // Right Shoulder
            ans.Add(new Tuple<JointType, JointType>(JointType.ShoulderCenter, JointType.ShoulderRight));
            ans.Add(new Tuple<JointType, JointType>(JointType.ShoulderRight, JointType.ElbowRight));
            ans.Add(new Tuple<JointType, JointType>(JointType.ElbowRight, JointType.WristRight));
            ans.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.HandRight));

            // Left Leg
            ans.Add(new Tuple<JointType, JointType>(JointType.HipCenter, JointType.HipLeft));
            ans.Add(new Tuple<JointType, JointType>(JointType.HipLeft, JointType.KneeLeft));
            ans.Add(new Tuple<JointType, JointType>(JointType.KneeLeft, JointType.AnkleLeft));
            ans.Add(new Tuple<JointType, JointType>(JointType.AnkleLeft, JointType.FootLeft));

            // Right Leg
            ans.Add(new Tuple<JointType, JointType>(JointType.HipCenter, JointType.HipRight));
            ans.Add(new Tuple<JointType, JointType>(JointType.HipRight, JointType.KneeRight));
            ans.Add(new Tuple<JointType, JointType>(JointType.KneeRight, JointType.AnkleRight));
            ans.Add(new Tuple<JointType, JointType>(JointType.AnkleRight, JointType.FootRight));

            return ans;
        }

        #endregion

        public static List<Tuple<JointType, JointType>> getConnections()
        {
            List<Tuple<JointType, JointType>> ans = new List<Tuple<JointType, JointType>>();
            foreach(Tuple<JointType, JointType> tuple in connections)
            {
                if (targetJoints.Contains(tuple.Item1) && targetJoints.Contains(tuple.Item2))
                {
                    ans.Add(tuple);
                }
            }
            return connections;
        }

        public static List<Tuple<JointType, JointType>> getAllConnections()
        {
            return connections;
        }

        public static List<JointType> getJoints()
        {
            return targetJoints;
        }

        public static void addJoint(JointType joint)
        {
            targetJoints.Add(joint);
        }

        #region Scaling methods

        public static Joint ScaleTo(Joint joint, double width, double height, SkeletonPoint skeletonPos)
        {
            return ScaleTo(joint, width, height, 1.0f, 1.0f, skeletonPos);
        }

        private static Joint ScaleTo(Joint joint, double width, double height, float skeletonMaxX, float skeletonMaxY, SkeletonPoint skeletonPos)
        {
            joint.Position = new SkeletonPoint()
            {
                                                            // Translation to origin (0, 0)
                X = Scale(width, skeletonMaxX, (float)(joint.Position.X - skeletonPos.X)),
                                                            // Translation to origin (0, 0)
                Y = Scale(height, skeletonMaxY, (float)(-joint.Position.Y - skeletonPos.Y)),
                Z = joint.Position.Z
            };

            return joint;
        }

        private static float Scale(double maxPixel, double maxSkeleton, float position)
        {
            float value = (float)((((maxPixel / maxSkeleton) / 2) * position) + (maxPixel / 2));

            if (value > maxPixel)
            {
                return (float)maxPixel;
            }

            if (value < 0)
            {
                return 0;
            }

            return value;
        }

        #endregion

        #region Drawing

        public static void DrawSkeleton(Canvas canvas, Skeleton skeleton, Color color, String tag)
        {
            if (skeleton == null)
            {
                return;
            }
            // TODO: mark with different colors the infered joints
            // TODO: skip the joints not in targetJoints?

            // Draw a point for each joint position
            foreach (Joint joint in skeleton.Joints)
            {
                Joint scaledJoint = ScaleTo(joint, screenWidth, screenHeight, skeleton.Position);
                DrawingUtils.DrawPoint(canvas, new Point { X = scaledJoint.Position.X, Y = scaledJoint.Position.Y }, color, tag);
            }

            // Draw lines between connected joints
            foreach (Tuple<JointType, JointType> tuple in getAllConnections())
            {
                DrawingUtils.DrawLine(canvas, 
                new Point
                {
                    X = ScaleTo(skeleton.Joints[tuple.Item1], screenWidth, screenHeight, skeleton.Position).Position.X,
                    Y = ScaleTo(skeleton.Joints[tuple.Item1], screenWidth, screenHeight, skeleton.Position).Position.Y
                },
                new Point
                {
                    X = ScaleTo(skeleton.Joints[tuple.Item2], screenWidth, screenHeight, skeleton.Position).Position.X,
                    Y = ScaleTo(skeleton.Joints[tuple.Item2], screenWidth, screenHeight, skeleton.Position).Position.Y
                }, color, tag);
            }
        }

        /// <summary>
        /// Utility funcion to avoid repetition of this action, remove the previous skeleton and draw the new one.
        /// </summary>
        public static void redraw(Canvas canvas, Skeleton actual, string tag, Color color)
        {
            modifyUI(new ThreadStart(() => {
                try
                {
                    DrawingUtils.deleteElements(canvas, tag);
                }
                catch (Exception) { }
            }));
            modifyUI(new ThreadStart(() => {
                try
                {
                    SkeletonUtils.DrawSkeleton(canvas, actual, color, tag);
                }
                catch (Exception) { }
            }));
        }

        public static void modifyUI(ThreadStart threadStart)
        {
            try
            {
                System.Windows.Application.Current.Dispatcher.BeginInvoke(threadStart);
            }
            catch (Exception) { }
        }
        #endregion

        #region Serialization

        public static void serialize(List<Skeleton> skeletons, Stream outStream)
        {
            BinaryFormatter serializer = new BinaryFormatter();
            serializer.Serialize(outStream, skeletons);
        }

        public static void serialize(List<Skeleton> skeletons, string filePath)
        {
            Stream outStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            serialize(skeletons, outStream);
            outStream.Close();
        }

        public static void serialize(Skeleton skeleton, string filePath)
        {
            List<Skeleton> list = new List<Skeleton>();
            list.Add(skeleton);
            serialize(list, filePath);
        }

        public static List<Skeleton> deserialize(string filePath)
        {
            Stream outStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None);
            List<Skeleton> ans = deserialize(outStream);
            outStream.Close();
            return ans;
        }

        public static List<Skeleton> deserialize(Stream outStream)
        {
            BinaryFormatter serializer = new BinaryFormatter();
            return (List<Skeleton>)serializer.Deserialize(outStream);
        }

        #endregion

        #region String Representation

        public static StringBuilder toString(Skeleton skeleton)
        {
            Array types = Enum.GetValues(typeof(JointType));
            StringBuilder serialized = new StringBuilder();

            serialized.AppendLine("SKELETON:" + skeleton.TrackingId + " > " + skeleton.TrackingState);
            foreach (JointType type in types)
            {
                serialized.AppendLine(type + " + " + skeleton.Joints[type].TrackingState + " > " + 
                    getFormattedRotation(type, skeleton) + " # " + getFormattedPosition(type, skeleton));
            }

            return serialized;
        }

        private static string getFormattedRotation(JointType type, Skeleton skeleton)
        {
            Vector4 actualPos = getRotation(type, skeleton);

            return actualPos.X + "|" + actualPos.Y + "|" + actualPos.Z + "|" + actualPos.W;
        }

        private static string getFormattedPosition(JointType type, Skeleton skeleton)
        {
            SkeletonPoint actualPos = getPosition(type, skeleton);

            return actualPos.X + "|" + actualPos.Y + "|" + actualPos.Z;
        }

        public static Vector4 getRotation(JointType type, Skeleton skel)
        {
            return skel.BoneOrientations[type].HierarchicalRotation.Quaternion;
        }

        public static SkeletonPoint getPosition(JointType type, Skeleton skel)
        {
            return skel.Joints[type].Position;
        }

        #endregion

        #region Comparison

        /// <summary>
        /// Checks if the two movements correspond to the same by comparing each frame.
        /// </summary>
        public static bool match(SkeletonRecording original, SkeletonRecording imitation)
        {
            float accumError = 0;
            SkeletonRecording safeOriginal = new SkeletonRecording(original);
            SkeletonRecording safeImitation = new SkeletonRecording(imitation);

            safeOriginal.restart();
            safeImitation.restart();

            while (!safeOriginal.finished())
            {
                Skeleton orig = safeOriginal.next();
                Skeleton imit = safeImitation.next();
                float error = compareAngles(orig, imit);
                if (error > delta)
                {
                    return false;
                }
                accumError += error;
                if (accumError > deltaAccum)
                {
                    return false;
                }
            }
            return accumError < deltaAccum;
        }

        /// <summary>
        /// Calculates the difference between the two movements as the absolute sum of each frame error.
        /// </summary>
        public static float difference(SkeletonRecording original, SkeletonRecording imitation)
        {
            float accumError = 0;

            SkeletonRecording safeOriginal = new SkeletonRecording(original);
            SkeletonRecording safeImitation = new SkeletonRecording(imitation);

            safeOriginal.restart();
            safeImitation.restart();
            while (!safeOriginal.finished())
            {
                Skeleton orig = safeOriginal.next();
                Skeleton imit = safeImitation.next();
                float error = compareAngles(orig, imit);
                accumError += error;
            }
            return accumError;
        }

        public static float compareAngles(Skeleton original, Skeleton imitation)
        {
            float accumError = 0;
            foreach(Tuple<JointType, JointType> tuple in connections)
            {
                if (!targetJoints.Contains(tuple.Item1) || !targetJoints.Contains(tuple.Item2))
                {
                    continue;
                }

                float diffXOrig = original.Joints[tuple.Item1].Position.X - original.Joints[tuple.Item2].Position.X;
                float diffXImit = imitation.Joints[tuple.Item1].Position.X - imitation.Joints[tuple.Item2].Position.X;

                float diffYOrig = original.Joints[tuple.Item1].Position.Y - original.Joints[tuple.Item2].Position.Y;
                float diffYImit = imitation.Joints[tuple.Item1].Position.Y - imitation.Joints[tuple.Item2].Position.Y;

                double origAngle = 90;
                if (diffXOrig != 0)
                {
                    origAngle = Math.Atan(diffYOrig / diffXOrig);
                }
                double imitAngle = 90;
                if (diffXImit != 0)
                {
                    imitAngle = Math.Atan(diffYImit / diffXImit);
                }

                accumError += (float) Math.Abs(origAngle - imitAngle);

            }
            return accumError;
        }


        public static float comparePositions(Skeleton original, Skeleton imitation)
        {
            float accumError = 0;
            foreach (JointType type in Enum.GetValues(typeof(JointType)))
            {
                if (!targetJoints.Contains(type))
                {
                    continue;
                }

                float errorX = Math.Abs(original.Joints[type].Position.X - imitation.Joints[type].Position.X);
                float errorY = Math.Abs(original.Joints[type].Position.Y - imitation.Joints[type].Position.Y);
                float errorZ = Math.Abs(original.Joints[type].Position.Z - imitation.Joints[type].Position.Z);
                if (errorX > 0.05)
                {
                    accumError += errorX;
                }
                if (errorY > 0.05)
                {
                    accumError += errorY;
                }
                if (errorZ > 0.05)
                {
                    accumError += errorZ;
                }

            }
            return accumError;
        }

        #endregion

        public static void ExtractRotationInDegrees(Vector4 rotQuaternion, out double pitch, out double yaw, out double roll)
        {
            double x = rotQuaternion.X;
            double y = rotQuaternion.Y;
            double z = rotQuaternion.Z;
            double w = rotQuaternion.W;

            // convert face rotation quaternion to Euler angles in degrees
            pitch = Math.Atan2(2 * ((y * z) + (w * x)), (w * w) - (x * x) - (y * y) + (z * z)) / Math.PI * 180.0;
            yaw = Math.Asin(2 * ((w * y) - (x * z))) / Math.PI * 180.0;
            roll = Math.Atan2(2 * ((x * y) + (w * z)), (w * w) + (x * x) - (y * y) - (z * z)) / Math.PI * 180.0;
        }

        public static Skeleton defaultSkeleton(object data)
        {
            Skeleton[] skeletons = (Skeleton[])data;
            return skeletons.Where(s => s.TrackingState == SkeletonTrackingState.Tracked).FirstOrDefault();
        }

    }
}
