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

namespace KinectServer
{
    static class SkeletonUtils
    {

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
    }
}
