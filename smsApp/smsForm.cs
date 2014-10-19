using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Text.RegularExpressions;

namespace smsApp
{
    public partial class smsForm : Form
    {
        SerialPort port = new SerialPort();
        smsMethods objsendSMS = new smsMethods();
        ShortMessageCollection objShortMessageCollection = new ShortMessageCollection();
        string baudRate = "9600";
        public smsForm()
        {
            InitializeComponent();
        }

        private void smsForm_Load(object sender, EventArgs e)
        {
            try
            {
                string[] ports = SerialPort.GetPortNames();
                foreach (string port in ports)
                {
                    this.cbPorts.Items.Add(port);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            try
            {
                this.port = objsendSMS.OpenPort(this.cbPorts.Text, baudRate);

                if (this.port != null)
                {
                    this.lblStatus.Text = "Connected at " + this.cbPorts.Text;
                    cbPorts.Enabled = false;
                    btnConnect.Enabled = false;
                    btnDisconnect.Enabled = true;
                    tabControls.Enabled = true;
                }
                else
                {
                    MessageBox.Show("Invalid port settings");
                    this.lblStatus.Text = "Not Connected";
                    cbPorts.Enabled = true;
                    btnConnect.Enabled = true;
                    btnDisconnect.Enabled = false;
                    tabControls.Enabled = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            try
            {
                objsendSMS.ClosePort(this.port);
                this.lblStatus.Text = "Not Connected";
                cbPorts.Enabled = true;
                btnConnect.Enabled = true;
                btnDisconnect.Enabled = false;
                tabControls.Enabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            try
            {
                if (this.txtPhonenum.Text != "" || this.txtMessage.Text != "")
                {
                    if (objsendSMS.sendMsg(this.port, this.cbPorts.Text, baudRate, this.txtPhonenum.Text, this.txtMessage.Text))
                    {
                        MessageBox.Show("Message has sent successfully");
                    }
                    else
                    {
                        MessageBox.Show("Failed to send message");
                    }
                }
                else
                {
                    MessageBox.Show("Please Enter a number and a message.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

    }
}
