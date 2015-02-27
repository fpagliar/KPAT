using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectUtils
{
    [Serializable]
    class ImageData
    {
        private int width;
        private int height;
        private byte[] data;

        public ImageData(int width, int height, byte[] data)
        {
            this.width = width;
            this.height = height;
            this.data = data;
        }

        public int getWidth()
        {
            return width;
        }

        public int getHeight()
        {
            return height;
        }

        public byte[] getData()
        {
            return data;
        }
    }
}
