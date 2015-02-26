using System.Diagnostics;
using System.Net;

namespace WpfInterface
{
    class VlcController
    {
        private string ip;

        public VlcController(string ip)
        {
            this.ip = ip;
        }

        public void run()
        {
            //Debug.WriteLine("TOGGLE");
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://" + ip + "/requests/status.xml?" + "command=pl_pause");
            request.Headers["Authorization"] = "Basic " + password("1234");
            WebResponse resp = request.GetResponse();
            //Debug.WriteLine("TOGGLE: " + resp.Headers);
        }

        public void volume()
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://" + ip + "/requests/status.xml?" + "command=volume&val=512");
            request.Headers["Authorization"] = "Basic " + password("1234");
            WebResponse resp = request.GetResponse();
        }

        public void stop()
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://" + ip + "/requests/status.xml?" + "command=pl_stop");
            request.Headers["Authorization"] = "Basic " + password("1234");
            WebResponse resp = request.GetResponse();
        }

        private string password(string pass)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(":" + pass); //No username
            return System.Convert.ToBase64String(plainTextBytes);
        }
    }
}
