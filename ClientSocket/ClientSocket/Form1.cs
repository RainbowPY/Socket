using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using System.Threading;
using System.Net;
using System.Net.Sockets;

namespace ClientSocket
{
    public partial class Form1 : Form
    {
        Socket socketClient;
        Thread mythread;//创建线程
        Socket socketServer;
        int port = 8080;//定义侦听端口号
        public static ManualResetEvent allDone = new ManualResetEvent(false);
        public delegate void MyDelegate(string ip,string message);

        public Form1()
        {
            InitializeComponent();
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            mythread = new Thread(new ThreadStart(BeginListen));
            mythread.Start();

            txtRemoteIP.Text = GetServerIP().ToString();
        }

        #region server
        //获取本机IP地址
        public static IPAddress GetServerIP()
        {
            IPHostEntry ieh = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress item in ieh.AddressList)
            {
                if (item.ToString().IndexOf("192.168.24.")>=0)
                {
                    return item;
                }
            }
            return null;
        }

        //异步传递的状态对象
        public class StateObject
        {
            public Socket workSocket = null;
            public const int BufferSize = 1024;
            public byte[] buffer = new byte[BufferSize];
        }

        //监听
        private void BeginListen()
        {
            socketServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            IPAddress ServerIp = GetServerIP();
            IPEndPoint iep = new IPEndPoint(ServerIp, port);
            socketServer.Bind(iep);

            while (true)
            {
                try
                {
                    socketServer.Listen(5);
                    allDone.Reset();
                    socketServer.BeginAccept(new AsyncCallback(AcceptCallback), socketServer);
                    allDone.WaitOne();
                }
                catch (SocketException ex)
                {
                    toolStripStatusLabel1.Text += ex.ToString();
                }
            }
        }

        //异步连接回调函数
        public void AcceptCallback(IAsyncResult ar)
        {
            Socket listener = (Socket)ar.AsyncState;
            Socket client = listener.EndAccept(ar);
            allDone.Set();
            StateObject state = new StateObject();
            state.workSocket = client;
            client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(readCallback), state);
        }

        //异步接收回调函数
        public void readCallback(IAsyncResult ar)
        {
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.workSocket;
            int bytesRead = handler.EndReceive(ar);
            if (bytesRead > 0)
            {
                string strmsg = Encoding.Default.GetString(state.buffer, 0, bytesRead);

                //远端信息
                EndPoint tempRemoteEP = handler.RemoteEndPoint;
                IPEndPoint tempRemoteIP = (IPEndPoint)tempRemoteEP;
                IPHostEntry host = Dns.GetHostByAddress(tempRemoteIP.Address);
                string HostName = host.HostName;

                string ip = tempRemoteIP.Address.ToString() + "(" + HostName + ") " + DateTime.Now.ToString();
                if (listBox1.InvokeRequired)
                {
                    MyDelegate md;
                    md = new MyDelegate(ChangeText);
                    listBox1.Invoke(md, ip, strmsg);
                }
            }
        }

        public void ChangeText(string ip,string message)
        {
            listBox1.Items.Add(ip);
            listBox1.Items.Add(message);
        }

        #endregion

        #region client
        //发送信息
        private void btn_Send_Click(object sender, EventArgs e)
        {
            try
            {
                string message = txtMsg.Text.Trim();
                socketClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                string remoteIp = this.txtRemoteIP.Text;
                string remotePort = this.txtRemotePort.Text;
                int serverPort = Convert.ToInt32(remotePort);
                IPAddress serverIp = IPAddress.Parse(remoteIp);
                IPEndPoint remoteIep = new IPEndPoint(serverIp, serverPort);
                socketClient.Connect(remoteIep);
                toolStripStatusLabel1.Text = "与远程计算机" + remoteIp + ":" + remotePort + "建立连接！";

                byte[] byteMessage = Encoding.Default.GetBytes(message);
                socketClient.Send(byteMessage);

                IPHostEntry host = Dns.GetHostEntry(GetServerIP());
                string HostName = host.HostName;

                //发送信息 
                string time1 = DateTime.Now.ToString();
                listBox1.Items.Add(GetServerIP().ToString() + "(" + HostName + ") " + time1);
                listBox1.Items.Add(message);

                socketClient.Shutdown(SocketShutdown.Both);
                socketClient.Close();
            }
            catch
            {
                toolStripStatusLabel1.Text = "无法连接到目标计算机！";
                return;
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
        #endregion
    }
}
