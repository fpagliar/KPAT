using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace WpfInterface
{
    class CameraListener : ClientListener
    {
        private Image mainImage;

        public CameraListener(Image skeletonCanvas)
        {
            this.mainImage = skeletonCanvas;
        }

        public void dataArrived(object data)
        {
            //ImageData img = (ImageData)data;
            //mainImage.Source = WindowUtils.ToBitmap(img);
        }
    }
}
