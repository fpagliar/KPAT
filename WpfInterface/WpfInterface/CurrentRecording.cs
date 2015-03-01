using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;

namespace WpfInterface
{
    class CurrentRecording : ClientListener
    {
        private bool recording = false;
        private static string recordingTag = "recording";
        private SkeletonRecording recorder;

        public CurrentRecording()
        {
            
        }

        public void startRecording()
        {
            recording = true;
            recorder = new SkeletonRecording(recordingTag);
        }

        public void startFixedRecording(int length)
        {
            recording = true;
            recorder = new SkeletonRecording(recordingTag, length);
        }

        public void stopRecording()
        {
            recording = false;
        }

        public void saveRecording(string filepath)
        {
            recorder.saveToFile(filepath);
        }

        public SkeletonRecording getCurrentRecording()
        {
            return recorder;
        }

        public void dataArrived(object data)
        {
            if (!recording)
            {
                return;
            }
            recorder.add(SkeletonUtils.defaultSkeleton(data));
        }
    }
}
