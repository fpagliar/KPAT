using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfInterface
{
    class FasterAction : Action
    {
        private MainWindow container;

        public FasterAction(MainWindow container)
        {
            this.container = container;
        }

        public void perform()
        {
            foreach (VlcController controller in container.getControllers())
                if(controller != null)
                    controller.faster();
        }

    }
}
