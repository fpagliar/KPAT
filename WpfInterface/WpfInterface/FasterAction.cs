using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfInterface
{
    class FasterAction : Action
    {
        private List<VlcController> controllers;

        public FasterAction(List<VlcController> controllers)
        {
            this.controllers = controllers;
        }

        public void perform()
        {
            foreach (VlcController controller in controllers)
                controller.faster();
        }

    }
}
