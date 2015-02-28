using System;
using System.Collections.Generic;
using System.Threading;
namespace WpfInterface
{
    class VoiceListener : ClientListener
    {
        private Dictionary<int, System.Windows.Controls.TextBox> UIControls;
        private string[] leftAndRightArmsIps;


        public VoiceListener(Dictionary<int, System.Windows.Controls.TextBox> UIControls)
        {
            this.UIControls = UIControls;
            this.leftAndRightArmsIps = new string[] { "127.0.0.1", "127.0.0.1", "127.0.0.1", "127.0.0.1", "127.0.0.1", "127.0.0.1" };

        }

        public void dataArrived(object data)
        {
            String[] dataVoice = ((String)data).Split(new Char[] {'#'});
            String confidence = dataVoice[0];
            String action = dataVoice[1].ToUpper();

            switch (action)
            {
                case "STOP": toggleStopAction(); break;
                case "START": toggleStartAction(); break;
                default:
                    break;
            }


        }

        private void toggleStartAction()
        {
            for (int i = 0; i < leftAndRightArmsIps.Length; i++)
            {
                Thread thread = new Thread(new VlcController(leftAndRightArmsIps[i]).togglePlay);
                thread.Start();
            }
            return;
        }

        private void toggleStopAction()
        {
            for (int i = 0; i < leftAndRightArmsIps.Length; i++)
            {
                Thread thread = new Thread(new VlcController(leftAndRightArmsIps[i]).togglePlay);
                thread.Start();
            }
            return;
        }
    }
}
