using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfInterface
{
    class SlowerAction : Action
    {
        private List<VlcController> controllers;

        public SlowerAction(List<VlcController> controllers)
        {
            this.controllers = controllers;
        }

        public void perform()
        {
            foreach (VlcController controller in controllers)
                controller.slower();
        }
    }
}
