using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Net;

namespace AsyncClient
{
    public partial class Form1 : Form
    {
        Socket c;
        private byte[] file;
        private string fileName;
        private string filePath;
        private byte[] receive = new byte[2048];
        private UInt16 From;
        Dictionary<string,string> files = new Dictionary<string,string>();
        public Form1()
        {
            InitializeComponent();
            button2.Visible = false;
            button2.Visible = false;
            this.bunifuImageButton1.Enabled = false;
            this.bunifuImageButton3.Enabled = false;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            
        }

        private void ConnectCallBack(IAsyncResult AR)
        {
            c.EndConnect(AR);
            IPEndPoint e = (IPEndPoint)c.LocalEndPoint;
            From = Convert.ToUInt16(e.Port);
            Directory.CreateDirectory(From.ToString());
            c.BeginReceive(receive, 0, receive.Length, SocketFlags.None, new AsyncCallback(ReceiveCallBack), c);
        }

        private void ReceiveCallBack(IAsyncResult AR)
        {
            Socket c = (Socket)AR.AsyncState;
            int rec = c.EndReceive(AR);
            if (rec == 0)
            {
                c.Close();
                this.Close();
            }
            //receive = new byte[rec];
            ChatMessage message = new ChatMessage(receive);
            message.ToMessage(rec);
            string temp = DateTime.Now.ToShortTimeString() + "\r\n" +
                          message.From + ": " + message.Text + "\r\n";

            richTextBox1.Invoke(new Action(() => AppendText(Color.Black,temp)));
            if (message.FileAttached)
            {
                LinkLabel link = new LinkLabel();
                link.Text = message.FileName;
                string file = Directory.GetCurrentDirectory() + "\\" + From + "\\" + message.FileName;
                link.LinkClicked += new LinkLabelLinkClickedEventHandler((sender,e) => this.link_LinkClicked(sender,e,file,"recv"));
                richTextBox1.Invoke(new Action(() => AppendText(Color.Black, "Attachment: ")));
                link.AutoSize = true;
                richTextBox1.Invoke(new Action(() => link.Location = richTextBox1.GetPositionFromCharIndex(this.richTextBox1.TextLength)));
                richTextBox1.Invoke(new Action(() => richTextBox1.Controls.Add(link)));
                richTextBox1.Invoke(new Action(() => richTextBox1.AppendText(link.Text + "   ")));
                richTextBox1.Invoke(new Action(() => richTextBox1.SelectionStart = this.richTextBox1.TextLength));
                richTextBox1.Invoke(new Action(() => richTextBox1.Text += "\r\n"));
                File.WriteAllBytes(file,message.FileBytes);
            }
            richTextBox1.Invoke(new Action(() => richTextBox1.Text += "\r\n"));
            c.BeginReceive(receive, 0, receive.Length, SocketFlags.None, new AsyncCallback(ReceiveCallBack), c);
        }

        private void link_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e,string file,string state)
        {
            if(state=="send")
            { 
                if(files.ContainsKey(file))
                {
                    string path;
                    files.TryGetValue(file, out path);
                    Process.Start(path);
                }
            }
            else
            {
                Process.Start(file);
            }
        }

        void AppendText(Color color, string text)
        {
            int start = richTextBox1.TextLength;
            richTextBox1.AppendText(text);
            int end = richTextBox1.TextLength;

            // Textbox may transform chars, so (end-start) != text.Length

            richTextBox1.Select(start, end);
            richTextBox1.SelectionColor = color;
            richTextBox1.SelectionLength = 0; // clear
        }

        private void button2_Click(object sender, EventArgs e)
        {
            
        }
        private void SendCallBack(IAsyncResult AR)
        {
            c.EndSend(AR);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            c = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            toolTip1.SetToolTip(bunifuImageButton2,"Connect");
            toolTip2.SetToolTip(bunifuImageButton1,"Send");
            toolTip3.SetToolTip(bunifuImageButton3, "Open file");
        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {
            
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            c.Close();
        }

        private void bunifuImageButton1_Click(object sender, EventArgs e)
        {
            if (!String.IsNullOrWhiteSpace(txtText.Text))
            {
                ChatMessage message = null;
                if (!String.IsNullOrWhiteSpace(fileName))
                {
                    message = new ChatMessage(From, txtText.Text, fileName, file, true);
                    //fileName = "";
                }
                else
                {
                    message = new ChatMessage(From, txtText.Text, fileName, file, false);
                }
                string temp = DateTime.Now.ToShortTimeString() + "\r\nMe: " + txtText.Text + "\r\n";
                AppendText(Color.Black, temp);

                if (message.FileAttached)
                {
                    AppendText(Color.Black, "Attachment: ");
                    LinkLabel link = new LinkLabel();
                    link.Text = fileName;
                    link.LinkClicked += new LinkLabelLinkClickedEventHandler((sender1, e1) => this.link_LinkClicked(sender1, e1, link.Text, "send"));
                    link.AutoSize = true;
                    link.Location = richTextBox1.GetPositionFromCharIndex(richTextBox1.TextLength);
                    richTextBox1.Controls.Add(link);
                    richTextBox1.AppendText(link.Text + "   ");
                    richTextBox1.SelectionStart = richTextBox1.TextLength;
                    files.Add(fileName, filePath);
                    richTextBox1.Text += "\r\n";
                }
                richTextBox1.Text += "\r\n";
                byte[] buffer = message.ToBytes();
                fileName = "";
                c.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(SendCallBack), c);
                txtText.Text = "";
            }
            //if (fileName != null)
            //{
            //    type = Encoding.ASCII.GetBytes("file");
            //    c.BeginSend(type, 0, type.Length, SocketFlags.None, new AsyncCallback(SendCallBack), null);
            //    List<byte> byteList = new List<byte>();
            //    byteList.AddRange(BitConverter.GetBytes(fileName.Length));
            //    byteList.AddRange(Encoding.ASCII.GetBytes(fileName));
            //    byteList.AddRange(file);
            //    byte[] buffer = byteList.ToArray();
            //    c.BeginSendFile(fileName, new AsyncCallback(FileCallBack), c);
            //}
            //else
            //{
            //    type = Encoding.ASCII.GetBytes("text");
            //    c.BeginSend(type, 0, type.Length, SocketFlags.None, new AsyncCallback(SendCallBack), null);
            //    byte[] data = ASCIIEncoding.ASCII.GetBytes(txtText.Text);
            //    c.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(SendCallBack), null);
            //}
        }

        private void bunifuImageButton2_Click(object sender, EventArgs e)
        {
            //this.BeginInvoke((MethodInvoker)delegate (){richTextBox1.Text = "1"});
            c.BeginConnect(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9001), new AsyncCallback(ConnectCallBack), null);
            this.bunifuImageButton2.Enabled = false;
            this.bunifuImageButton1.Enabled = true;
            bunifuImageButton3.Enabled = true;
        }

        private void bunifuImageButton3_Click(object sender, EventArgs e)
        {
            openFileDialog1.ShowDialog();
            filePath = openFileDialog1.FileName;
            file = File.ReadAllBytes(filePath);
            fileName = Path.GetFileName(filePath);
        }
    }
}
