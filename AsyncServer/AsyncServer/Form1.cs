using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Net;
using System.Security.Cryptography;
using AsyncClient;

namespace AsyncServer
{
    public partial class Form1 : Form
    {
        Socket server;
        List<Socket> clients = new List<Socket>();
        private string type = "";
        byte[] data;
        public Form1()
        {
            InitializeComponent();
            /*
             Errno 2] No such file or directory: 'C:\\Users\\Danial\\Desktop\\backup\\images\\5d4afae6569e0
             Screen+Shot+2017-02-16+at+3.34.45+PM.xml
             */
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            StartServer();
            Directory.CreateDirectory("uploads");
        }

        public void StartServer()
        {
            server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            server.Bind(new IPEndPoint(IPAddress.Parse("127.0.0.1"),9001));
            server.Listen(5);
            server.BeginAccept(new AsyncCallback(AcceptCallBack), null);

        }

        private void AcceptCallBack(IAsyncResult AR)
        {
            Socket c = server.EndAccept(AR);
            this.Invoke(new Action(() => this.richTextBox1.Text += "New connection from: " +c.RemoteEndPoint + "\r\n"));
            clients.Add(c);
            data = new byte[2048];
            byte[] buffer;
            string message = "Hey! " + c.RemoteEndPoint + " Welcome to NP Chat room !!";
            ChatMessage m = new ChatMessage(9001,message,null,null,false);
            buffer = m.ToBytes();
            c.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(SendCallBack), c);
            c.BeginReceive(data, 0, data.Length, SocketFlags.None, new AsyncCallback(ReceiveCallBack), c);
            server.BeginAccept(new AsyncCallback(AcceptCallBack), null);
        }

        private void ReceiveCallBack(IAsyncResult AR)
        {
            Socket c = (Socket)AR.AsyncState;
            int rec = c.EndReceive(AR);
            if (rec == 0)
            { 
                clients.Remove(c);
                c.Close();
            }
            byte[] received = new byte[rec];
            Buffer.BlockCopy(data,0,received,0,rec);
            ChatMessage message = new ChatMessage(received);
            message.ToMessage(rec);
            if (clients.Count > 1)
            {
                foreach (Socket client in clients)
                {
                    if (!c.Equals(client))
                    {
                        ChatMessage temp = new ChatMessage(message.From,message.Text,message.FileName,message.FileBytes,message.FileAttached);
                        if (temp.FileAttached)
                        {
                            string path = Directory.GetCurrentDirectory() + "\\uploads\\" + temp.FileName;
                            File.WriteAllBytes(path,temp.FileBytes);
                        }
                        byte[] toSend = temp.ToBytes();
                        client.BeginSend(toSend, 0, toSend.Length, SocketFlags.None, new AsyncCallback(SendCallBack), client);
                }
                }
                string temp2 = DateTime.Now.ToShortTimeString() + "\r\n" +
                              message.From + ": " + message.Text + "\r\n";
                if (message.FileAttached)
                {
                    temp2 += "Attachement: " + message.FileName + "\r\n";
                }
                temp2 += "\r\n";
                richTextBox1.Invoke(new Action(() => this.richTextBox1.Text += temp2));
            }
            else if (clients.Count == 0)
            {
                MessageBox.Show("No clients connected");
            }
            else
            {
                MessageBox.Show("Only 1 client is connected");
            }
            type = "";
            c.BeginReceive(data, 0, data.Length, SocketFlags.None, new AsyncCallback(ReceiveCallBack), c);
        }
        private void SendCallBack(IAsyncResult AR)
        {
            Socket c = (Socket)AR.AsyncState;
            c.EndSend(AR);
        }
    }
}
