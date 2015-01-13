using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Diagnostics;

namespace VlcController
{
    public partial class Form1 : Form
    {

        private string password;
        private string basicURL = "http://localhost:8080/requests/status.xml?";

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "( *.mp4) | *.mp4";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                Process myProcess = Process.Start(dialog.FileName);
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(":" + passwordTextBox.Text); //No username
            password = System.Convert.ToBase64String(plainTextBytes);
        }

        private void PlayPauseButton_Click(object sender, EventArgs e)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(basicURL + "command=pl_pause");
            request.Headers["Authorization"] = "Basic " + password;
            request.GetResponse();
        }

        private void StopButton_Click(object sender, EventArgs e)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(basicURL + "command=pl_stop");
            request.Headers["Authorization"] = "Basic " + password;
            request.GetResponse();
        }

        private void Volume_Scroll(object sender, EventArgs e)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(basicURL + "command=volume&val=" + VolumeScrollBar.Value);
            request.Headers["Authorization"] = "Basic " + password;
            request.GetResponse();
        }
    }
}
