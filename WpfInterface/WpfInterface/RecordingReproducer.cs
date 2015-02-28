using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Controls;
using System.Collections.Concurrent;
using System.Threading;

namespace WpfInterface
{

    class RecordingReproducer : ClientListener
    {
        private SkeletonRecording recorder;
        private TcpClient client;
        private Canvas skeletonCanvas;
        private string tag;
        private Color color;

        public RecordingReproducer(Canvas skeletonCanvas, SkeletonRecording recorder, string tag, Color color)
        {
            this.recorder = recorder;
            recorder.restart();
            this.tag = tag;
            this.color = color;
            this.skeletonCanvas = skeletonCanvas;
        }

        public RecordingReproducer(Canvas skeletonCanvas, SkeletonRecording recorder, string tag, Color color, TcpClient client)
        {
            this.recorder = recorder;
            recorder.restart();
            this.client = client;
            this.tag = tag;
            this.color = color;
            this.skeletonCanvas = skeletonCanvas;
        }

        public void dataArrived(object data)
        {
            if (recorder.finished())
            {
                if (client != null)
                {
                    client.unsubscribe(this);
                    Application.Current.Dispatcher.BeginInvoke(new ThreadStart(() =>
                        DrawingUtils.deleteElements(skeletonCanvas, tag)));
                    return;
                }
                else
                {
                    recorder.restart();
                }
            }
            SkeletonUtils.redraw(skeletonCanvas, recorder.next(), tag, color);
        }
    }
}
