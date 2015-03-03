using System;
using System.Collections.Generic;
using System.Threading;
namespace WpfInterface
{
    class VoiceListener : ClientListener
    {

        private MainWindow container;

        public VoiceListener(MainWindow container) 
        {
            this.container = container;
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
            container.startRecognized();
            foreach(VlcController controller in container.getControllers())
            {
                if (controller != null)
                {
                    new Thread(controller.togglePlay).Start();
                }
            }
            return;
        }

        private void toggleStopAction()
        {
            container.stopRecognized();
            foreach (VlcController controller in container.getControllers())
            {
                if (controller != null)
                {
                    new Thread(controller.togglePlay).Start();
                }
            }
            return;
        }
    }
}
