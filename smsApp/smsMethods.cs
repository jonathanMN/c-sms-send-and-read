using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Threading;
using System.Text.RegularExpressions;

namespace smsApp
{
    class smsMethods
    {
        public SerialPort OpenPort(string strPortName, string strBaudRate)
        {
            receiveNow = new AutoResetEvent(false);
            SerialPort port = new SerialPort();
            port.PortName = strPortName;
            port.BaudRate = Convert.ToInt32(strBaudRate);
            port.DataBits = 8;
            port.StopBits = StopBits.One;
            port.Parity = Parity.None;
            port.ReadTimeout = 300;
            port.WriteTimeout = 300;
            port.Encoding = Encoding.GetEncoding("iso-8859-1");
            port.DataReceived += new SerialDataReceivedEventHandler(port_DataReceived);
            port.Open();
            port.DtrEnable = true;
            port.RtsEnable = true;
            return port;
        }

        public void ClosePort(SerialPort port)
        {
            port.Close();
            port.DataReceived -= new SerialDataReceivedEventHandler(port_DataReceived);
            port = null;
        }

        public string ExecCommand(SerialPort port, string command, int responseTimeout, string errorMessage)
        {
            try
            {
                port.DiscardOutBuffer();
                port.DiscardInBuffer();
                receiveNow.Reset();
                port.Write(command + "\r");

                string input = ReadResponse(port, responseTimeout);
                if ((input.Length == 0) || ((!input.EndsWith("\r\n> ")) && (!input.EndsWith("\r\nOK\r\n"))))
                    throw new ApplicationException("No success message was received.");
                return input;
            }
            catch (Exception ex)
            {
                throw new ApplicationException(errorMessage, ex);
            }
        }

        public void port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (e.EventType == SerialData.Chars)
                receiveNow.Set();
        }
        public string ReadResponse(SerialPort port, int timeout)
        {
            string buffer = string.Empty;
            do
            {
                if (receiveNow.WaitOne(timeout, false))
                {
                    string t = port.ReadExisting();
                    buffer += t;
                }
                else
                {
                    if (buffer.Length > 0)
                        throw new ApplicationException("Response received is incomplete.");
                    else
                        throw new ApplicationException("No data received from phone.");
                }
            }
            while (!buffer.EndsWith("\r\nOK\r\n") && !buffer.EndsWith("\r\n> ") && !buffer.EndsWith("\r\nERROR\r\n"));
            return buffer;
        }

        public AutoResetEvent receiveNow;

        public ShortMessageCollection ReadSMS(SerialPort port, string strPortName, string strBaudRate)
        {
            ShortMessageCollection messages = null;
            try
            {
                // Check connection
                ExecCommand(port, "AT", 300, "No phone connected at " + strPortName + ".");
                // Use message format "Text mode"
                ExecCommand(port, "AT+CMGF=1", 300, "Failed to set message format.");
                // Use character set "ISO 8859-1"
                ExecCommand(port, "AT+CSCS=\"iso-8859-1\"", 300, "Failed to set character set.");
                // Select SIM storage
                ExecCommand(port, "AT+CPMS=\"SM\"", 300, "Failed to select message storage.");
                // Read the messages
                string input = ExecCommand(port, "AT+CMGL=\"ALL\"", 5000, "Failed to read the messages.");
                messages = ParseMessages(input);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            finally
            {
                if (port != null)
                {
                    ClosePort(port);
                    port.Close();
                    port.DataReceived -= new SerialDataReceivedEventHandler(port_DataReceived);
                    port = null;
                }
            }

            if (messages != null)
                return messages;
            else
                return null;
        }
        public ShortMessageCollection ParseMessages(string input)
        {
            ShortMessageCollection messages = new ShortMessageCollection();
            Regex r = new Regex(@"\+CMGL: (\d+),""(.+)"",""(.+)"",(.*),""(.+)""\r\n(.+)\r\n");
            Match m = r.Match(input);
            while (m.Success)
            {
                ShortMessage msg = new ShortMessage();
                msg.Index = int.Parse(m.Groups[1].Value);
                msg.Status = m.Groups[2].Value;
                msg.Sender = m.Groups[3].Value;
                msg.Alphabet = m.Groups[4].Value;
                msg.Sent = m.Groups[5].Value;
                msg.Message = m.Groups[6].Value;
                messages.Add(msg);

                m = m.NextMatch();
            }

            return messages;
        }

        static AutoResetEvent readNow = new AutoResetEvent(false);

        public bool sendMsg(SerialPort port, string strPortName, string strBaudRate, string PhoneNo, string Message)
        {
            bool isSend = false;
            try
            {
                string recievedData = ExecCommand(port, "AT", 300, "No phone connected at " + strPortName + ".");
                recievedData = ExecCommand(port, "AT+CMGF=1", 300, "Failed to set message format.");
                String command = "AT+CMGS=\"" + PhoneNo + "\"";
                recievedData = ExecCommand(port, command, 300, "Failed to accept phoneNo");
                command = Message + char.ConvertFromUtf32(26) + "\r";
                recievedData = ExecCommand(port, command, 3000, "Failed to send message"); //3 seconds
                if (recievedData.EndsWith("\r\nOK\r\n"))
                {
                    recievedData = "Message sent successfully";
                    isSend = true;
                }
                else if (recievedData.Contains("ERROR"))
                {
                    string recievedError = recievedData;
                    recievedError = recievedError.Trim();
                    recievedData = "Following error occured while sending the message" + recievedError;
                    isSend = false;
                }
                return isSend;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            finally
            {
                if (port != null)
                {
                    port.Close();
                    port.DataReceived -= new SerialDataReceivedEventHandler(port_DataReceived);
                    port = null;
                }
            }
        }
        static void DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (e.EventType == SerialData.Chars)
                readNow.Set();
        }

        public void DeleteMsg(SerialPort port, string strPortName, string strBaudRate)
        {
            try
            {
                string recievedData = ExecCommand(port, "AT", 300, "No phone connected at " + strPortName + ".");
                recievedData = ExecCommand(port, "AT+CMGF=1", 300, "Failed to set message format.");
                String command = "AT+CMGD=1,3";
                recievedData = ExecCommand(port, command, 300, "Failed to delete message");

                if (recievedData.EndsWith("\r\nOK\r\n"))
                    recievedData = "Message delete successfully";
                if (recievedData.Contains("ERROR"))
                {
                    string recievedError = recievedData;
                    recievedError = recievedError.Trim();
                    recievedData = "Following error occured while sending the message" + recievedError;
                }

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            finally
            {
                if (port != null)
                {
                    port.Close();
                    port.DataReceived -= new SerialDataReceivedEventHandler(port_DataReceived);
                    port = null;
                }
            }
        }

    }

}