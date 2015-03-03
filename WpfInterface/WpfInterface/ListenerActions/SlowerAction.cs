using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfInterface
{
    class SlowerAction : Action
    {
        private IReadOnlyList<VlcController> controllers;

        public SlowerAction(IReadOnlyList<VlcController> controllers)
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
