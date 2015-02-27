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

namespace WpfInterface
{
    static class SkeletonUtils
    {
        private static float delta = 2000f;
        private static float deltaAccum = 20000f;

        private static int screenWidth = 700;
        private static int screenHeight = 420;

        private static List<JointType> targetJoints = new List<JointType>();
        private static List<Tuple<JointType, JointType>> connections = setUpConnections();

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

        public static Joint ScaleTo(Joint joint, double width, double height)
        {
            return ScaleTo(joint, width, height, 1.0f, 1.0f);
        }

        private static Joint ScaleTo(Joint joint, double width, double height, float skeletonMaxX, float skeletonMaxY)
        {
            joint.Position = new SkeletonPoint()
            {
                X = Scale(width, skeletonMaxX, joint.Position.X),
                Y = Scale(height, skeletonMaxY, -joint.Position.Y),
                Z = joint.Position.Z
            };

            return joint;
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Returns the scaled value of the specified position.
        /// </summary>
        /// <param name="maxPixel">Width or height.</param>
        /// <param name="maxSkeleton">Border (X or Y).</param>
        /// <param name="position">Original position (X or Y).</param>
        /// <returns>The scaled value of the specified position.</returns>
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
            foreach (Joint joint in skeleton.Joints)
            {
                Joint scaledJoint = ScaleTo(joint, screenWidth, screenHeight);
                DrawingUtils.DrawPoint(canvas, new Point { X = scaledJoint.Position.X, Y = scaledJoint.Position.Y }, color, tag);
            }

            foreach (Tuple<JointType, JointType> tuple in getAllConnections())
            {
                DrawingUtils.DrawLine(canvas, new Point
                {
                    X = ScaleTo(skeleton.Joints[tuple.Item1], screenWidth, screenHeight).Position.X,
                    Y = ScaleTo(skeleton.Joints[tuple.Item1], screenWidth, screenHeight).Position.Y
                },
                new Point
                {
                    X = ScaleTo(skeleton.Joints[tuple.Item2], screenWidth, screenHeight).Position.X,
                    Y = ScaleTo(skeleton.Joints[tuple.Item2], screenWidth, screenHeight).Position.Y
                }, color, tag);
            }
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

        public static bool match(SkeletonRecorder original, SkeletonRecorder imitation)
        {
            float accumError = 0;
            SkeletonRecorder safeOriginal = new SkeletonRecorder(original);
            SkeletonRecorder safeImitation = new SkeletonRecorder(imitation);

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

        public static float difference(SkeletonRecorder original, SkeletonRecorder imitation)
        {
            float accumError = 0;

            SkeletonRecorder safeOriginal = new SkeletonRecorder(original);
            SkeletonRecorder safeImitation = new SkeletonRecorder(imitation);

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

                double origAngle = Math.Atan(diffYOrig / diffXOrig);
                double imitAngle = Math.Atan(diffYImit / diffXImit);

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
                    //Debug.WriteLine(type + "X: " + errorX);
                    accumError += errorX;
                }
                if (errorY > 0.05)
                {
                    //Debug.WriteLine(type + "Y: " + errorY);
                    accumError += errorY;
                }
                if (errorZ > 0.05)
                {
                    //Debug.WriteLine(type + "Z: " + errorZ);
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

    }
}
