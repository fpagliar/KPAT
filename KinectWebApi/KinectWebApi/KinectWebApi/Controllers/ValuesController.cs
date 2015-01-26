using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Microsoft.Kinect;
using System.Diagnostics;

namespace KinectWebApi.Controllers
{
    public class ValuesController : ApiController
    {
        // GET api/values
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/values/5
        public string Get(int id)
        {
            Vector4 vec = UnityProxy.getPosition(id);
            return vec.X + "|" + vec.Y + "|" + vec.Z + "|" + vec.W;
        }

        // POST api/values
        public void Post([FromBody]string value)
        {
        }

        // PUT api/values/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        public void Delete(int id)
        {
        }
    }

    class UnityProxy
    {
        private static KinectSensor sensor;

        private static Skeleton actualSkeleton;
        private static DateTime lastUpdate;

        private static DateTime startTime;
        private static Skeleton tposeSkeleton;
        private static Skeleton[] tposeSamples = new Skeleton[100];
        private static int index = 0;

        public static void init()
        {
            Debug.WriteLine("SETTING UP KINECT");
            setBasicSkeleton();
            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    sensor = potentialSensor;
                    break;
                }
            }

            if (null != sensor)
            {
                Debug.WriteLine("SENSOR FOUND");
                sensor.SkeletonStream.Enable();
                Debug.WriteLine("SENSOR ENABLED");

                sensor.SkeletonFrameReady += SensorSkeletonFrameReady;

                // Start the sensor!
                try
                {
                    sensor.Start();
                    Debug.WriteLine("SENSOR STARTED");
                }
                catch (System.IO.IOException)
                {
                    sensor = null;
                }
            }
            startTime = DateTime.Now;
        }

        /// Event handler for Kinect sensor's SkeletonFrameReady event
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private static void SensorSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            Skeleton[] skeletons = new Skeleton[0];
            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(skeletons);
                }
            }
            foreach(Skeleton skel in skeletons)
            {
                if (skel.TrackingState == SkeletonTrackingState.Tracked)
                {
                    //Debug.WriteLine("Fixed skeleton");
                    fixSkeleton(skel);
                    return;
                }
            }
        }

        private static void fixSkeleton(Skeleton skeleton)
        {
            lastUpdate = DateTime.Now;
            actualSkeleton = skeleton;

            if (startTime.AddSeconds(15) > DateTime.Now)
            {
                Debug.WriteLine("WAITING");
                return; // At start, not serializing
            }

            if (tposeSkeleton == null)
            {
                Debug.WriteLine("TPOSE FIXED");
                tposeSkeleton = skeleton;
            }

            if (index < 100)
            {
                Debug.WriteLine("Sampling...");
                tposeSamples[index] = skeleton;
                index++;
            }
            Array types = Enum.GetValues(typeof(JointType));
            if(index == 100)
            {
                foreach (JointType type in types)
                {
                    printVariation(type);
                }
                index++;
            }

            string[] lines = new string[types.Length];
            int i = 0;
            foreach (JointType type in types)
            {
                lines[i++] = getFileFormat(type);
            }

            while (true)
            {
                try
                {
                    System.IO.File.WriteAllLines(@"C:\Users\sheetah\Documents\skeleton.txt", lines);
                    return;
                }
                catch (Exception)
                {
                    //Ignore
                }
            }
        }

        private static void printVariation(JointType type)
        {
            float maxX = -10000, maxY = -1000, maxZ = -1000, maxW = -1000;
            float minX = 10000, minY = 1000, minZ = 1000, minW = 1000;
            float sumX = 0, sumY = 0, sumZ = 0, sumW = 0;
            for(int i = 0; i < 100; i++)
            {
                Vector4 quat = getPosition((int)type, tposeSamples[i]);
                sumX += quat.X;
                sumY += quat.Y;
                sumZ += quat.Z;
                sumW += quat.W;
                if (quat.X > maxX)
                {
                    maxX = quat.X;
                }
                if (quat.Y > maxY)
                {
                    maxY = quat.Y;
                }
                if (quat.Z > maxZ)
                {
                    maxZ = quat.Z;
                }
                if (quat.W > maxW)
                {
                    maxW = quat.W;
                }

                if (quat.X < minX)
                {
                    minX = quat.X;
                }
                if (quat.Y < minY)
                {
                    minY = quat.Y;
                }
                if (quat.Z < minZ)
                {
                    minZ = quat.Z;
                }
                if (quat.W < minW)
                {
                    minW = quat.W;
                }
            }
            Debug.WriteLine("--- STATS FOR JOINT: " + type);
            Debug.WriteLine("-> X : " + (sumX / 100) + " min:" + minX + "#" + ((((sumX / 100) - minX) * 100)) / (sumX / 100) + "%" + " max:" + maxX + "#" + (((maxX - (sumX / 100)) * 100)) / (sumX / 100) + "%");
            Debug.WriteLine("-> Y : " + (sumY / 100) + " min:" + minY + "#" + ((((sumY / 100) - minY) * 100)) / (sumY / 100) + "%" + " max:" + maxY + "#" + (((maxY - (sumY / 100)) * 100)) / (sumY / 100) + "%");
            Debug.WriteLine("-> Z : " + (sumZ / 100) + " min:" + minZ + "#" + ((((sumZ / 100) - minZ) * 100)) / (sumZ / 100) + "%" + " max:" + maxZ + "#" + (((maxZ - (sumZ / 100)) * 100)) / (sumZ / 100) + "%");
            Debug.WriteLine("-> W : " + (sumW / 100) + " min:" + minW + "#" + ((((sumW / 100) - minW) * 100)) / (sumW / 100) + "%" + " max:" + maxW + "#" + (((maxW - (sumW / 100)) * 100)) / (sumW / 100) + "%");
            Debug.WriteLine("--- --------------- ");
        }

        private static string getFileFormat(JointType type)
        {
            Vector4 tposePos = getPosition((int)type, tposeSkeleton);
            Vector4 actualPos = getPosition((int)type, actualSkeleton);
            
            return (int)type + "*" +
                tposePos.X + "|" + tposePos.Y + "|" + tposePos.Z + "|" + tposePos.W + "#" +
                actualPos.X + "|" + actualPos.Y + "|" + actualPos.Z + "|" + actualPos.W;
        }

        public static Vector4 getPosition(int type)
        {
            return actualSkeleton.BoneOrientations[(JointType)type].AbsoluteRotation.Quaternion;
        }

        public static Vector4 getPosition(int type, Skeleton skel)
        {
            return skel.BoneOrientations[(JointType)type].AbsoluteRotation.Quaternion;
        }

        public static DateTime getLastUpdate()
        {
            return lastUpdate;
        }

        private static void setBasicSkeleton()
        {
            actualSkeleton = new Skeleton();
            foreach (BoneOrientation bone in actualSkeleton.BoneOrientations)
            {
                Vector4 val = new Vector4();
                val.W = 0;
                val.X = 0;
                val.Y = 0;
                val.Z = 0;
                bone.AbsoluteRotation.Quaternion = val;
            }
        }
    }

}