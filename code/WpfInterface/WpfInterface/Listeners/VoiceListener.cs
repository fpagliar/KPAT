﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
namespace WpfInterface
{
    class VoiceListener : ClientListener
    {
        private MainWindow container;
        private double confidenceThreshold = 0.5;

        public VoiceListener(MainWindow container) 
        {
            this.container = container;
        }

        public void dataArrived(object data)
        {
            String[] dataVoice = ((String)data).Split(new Char[] {'#'});
            String confidence = dataVoice[0];
            String action = dataVoice[1].ToUpper();
            if (Double.Parse(confidence).CompareTo(confidenceThreshold) > 0)
            {
                switch (action)
                {
                    case "PLAY":
                    case "PAUSE":
                        toggleStartAction(); break;
                    case "SET UP":
                        toggleSetupAction(); break;
                    case "BUCKETS": toggleBucketsAction(); break;
                    default:
                        break;
                }
            }
        }

        private void toggleStartAction()
        {
            foreach(VlcController controller in container.getControllers())
            {   
                if (controller != null)
                {
                    new Thread(controller.togglePlayPause).Start();
                }
            }
            container.startRecognized();
            return;
        }

        private void toggleStopAction()
        {
            container.stopRecognized();
            foreach (VlcController controller in container.getControllers())
            {
                if (controller != null)
                {
                    new Thread(controller.stop).Start();
                }
            }
            return;
        }

        private void toggleBucketsAction()
        {
            container.toggleBuckets();
        }

        private void toggleSetupAction()
        {
            foreach (VlcController controller in container.getControllers())
                if (controller != null)
                    controller.setup();

            container.setUpRecognized();
        }
    }
}
