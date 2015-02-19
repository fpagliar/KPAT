using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;

namespace WpfInterface
{
    class SkeletonRecorder
    {
        private List<Skeleton> skeletons = new List<Skeleton>();
        private IEnumerator<Skeleton> index;
        private string tag;
        private bool end;
        private Skeleton last;
        private int fixedLength = -1;
        private bool immutable = false;

        public SkeletonRecorder(SkeletonRecorder original)
        {
            foreach (Skeleton skel in original.skeletons)
            {
                skeletons.Add(skel);
            }
            tag = original.tag;
            end = false;
            immutable = true;
        }

        public SkeletonRecorder(string _tag)
        {
            tag = _tag;
            end = false;
        }
        public SkeletonRecorder(string _tag, int length)
        {
            tag = _tag;
            end = false;
            fixedLength = length;
        }

        public void add(Skeleton skel)
        {
            if (immutable)
            {
                return;
            }
            index = null;
            end = false;
            if (fixedLength != -1)
            {
                if (skeletons.Count >= fixedLength)
                {
                    skeletons.RemoveAt(0);
                }
            }
            skeletons.Add(skel);
        }

        public Skeleton next()
        {
            if (index == null)
            {
                index = skeletons.GetEnumerator();
            }
            end = !index.MoveNext();
            if (!end)
            {
                last = index.Current;
                return index.Current;
            }
            else
            {
                return last;
            }
        }

        public void restart()
        {
            if (index == null)
            {
                index = skeletons.GetEnumerator();
            }
            index.Reset();
            end = false;
        }

        public bool finished()
        {
            return end;
        }

        public void saveToFile(string filePath)
        {
            SkeletonUtils.serialize(skeletons, filePath);
        }

        public void loadFromFile(string filePath)
        {
            if (immutable)
            {
                return;
            }
            skeletons = SkeletonUtils.deserialize(filePath);
            end = false;
        }

        public int size()
        {
            return skeletons.Count;
        }

    }
}
