using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace WpfInterface
{
    class CameraListener : ClientListener
    {
        private Image mainImage;

        public CameraListener(Image mainImage)
        {
            this.mainImage = mainImage;
        }

        public void dataArrived(object data)
        {
            List<Object> list = (List<Object>) data;
            Application.Current.Dispatcher.BeginInvoke(new ThreadStart(() => mainImage.Source = WindowUtils.ToBitmap((int)list[0], (int)list[1], (byte[])list[2])));
        }
    }
}
