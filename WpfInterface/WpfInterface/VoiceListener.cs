using System;
using System.Collections.Generic;
using System.Threading;
namespace WpfInterface
{
    class VoiceListener : ClientListener
    {
        private Dictionary<int, System.Windows.Controls.TextBox> UIControls;
        private List<VlcController> vlcControllers;


        public VoiceListener(Dictionary<int, System.Windows.Controls.TextBox> UIControls , List<VlcController> vlcControllers1, List<VlcController> vlcControllers2) 
        {
            this.UIControls = UIControls;
            this.vlcControllers = vlcControllers1;
            foreach (VlcController item in vlcControllers2)
            {
                this.vlcControllers.Add(item);   
            }

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
            for (int i = 0; i < vlcControllers.Count; i++)
            {
                Thread thread = new Thread(vlcControllers[i].togglePlay);
                thread.Start();
            }
            return;
        }

        private void toggleStopAction()
        {
            for (int i = 0; i < vlcControllers.Count; i++)
            {
                Thread thread = new Thread(vlcControllers[i].togglePlay);
                thread.Start();
            }
            return;
        }
    }
}
