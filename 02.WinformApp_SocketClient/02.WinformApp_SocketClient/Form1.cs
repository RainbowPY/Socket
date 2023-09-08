using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace _02.WinformApp_SocketClient
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        //客户端的socket对象
        private Socket clientSocket = null;

        private void button1_Click(object sender, EventArgs e)
        {
            Button btn = (Button)sender;
            btn.Enabled = false;

            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPAddress ip = IPAddress.Parse(textBoxIP.Text);
            EndPoint endPoint = new IPEndPoint(ip, int.Parse(textBoxPort.Text));

            //连接服务器
            clientSocket.Connect(endPoint);

            ShowLog("连接服务器成功");

            Task.Run(() =>
            {
                ReceiveMessage(clientSocket);
            });
        }

        //接收来自服务器的消息
        private void ReceiveMessage(Socket socket)
        {
            try
            {
                while (true)
                {
                    byte[] buffer = new byte[1024 * 1024 * 2];
                    int len = socket.Receive(buffer);

                    //没有接收到消息：客户端关闭连接、请求超时、接收过程中出现异常
                    if (len <= 0)
                    {
                        socket.Close();
                    }

                    //文本
                    if (buffer[0] == 1)
                    {
                        string content = Encoding.UTF8.GetString(buffer, 1, len - 1);
                        ShowLog($"接收到来自服务器的消息：{content}");
                    }
                    //震动
                    else if (buffer[0] == 2)
                    {
                        Zhendong();
                    }
                    //文件
                    else if (buffer[0] == 3)
                    {
                        this.Invoke(new Action(() => {
                            SaveFileDialog saveFileDialog = new SaveFileDialog();
                            saveFileDialog.Title = "请选择要发送的文件";
                            saveFileDialog.InitialDirectory = @"C:\Users\mazg\Desktop";
                            saveFileDialog.Filter = "所有文件|*.*";
                            saveFileDialog.ShowDialog();

                            string fileName = saveFileDialog.FileName;
                            if (string.IsNullOrEmpty(fileName)) return;

                            using (FileStream fileStream = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.Write))
                            {
                                fileStream.Write(buffer, 1, len - 1);
                            }

                        }));
                    }
                }
            }
            catch (Exception ex)
            {
                socket.Close();

                //让连接按钮重新启用
                button1.Invoke(new Action(() =>
                {
                    button1.Enabled = true;
                }));
            }
        }

        private void ShowLog(string message)
        {
            textBoxLog.Invoke(new Action(() =>
            {
                
                textBoxLog.AppendText(DateTime.Now+":"+ message + "\r\n");
            }));
        }

        //让当前窗口震动
        private void Zhendong()
        {
            for (int i = 0; i < 100; i++)
            {
                this.Invoke(new Action(() =>
                {
                    this.Location = new Point(100, 100);
                    this.Location = new Point(150, 150);
                }));
            }
        }

        //向服务器的socket发送数据
        private void button2_Click(object sender, EventArgs e)
        {
            if (clientSocket != null)
            {
                string content = textBoxMsg.Text;

                if (string.IsNullOrEmpty(content)) return;
                ShowLog($"本机信息:{textBoxMsg.Text}");
                byte[] buff = Encoding.UTF8.GetBytes(content);
                clientSocket.Send(buff);

                textBoxMsg.Text = "";
            }
        }
    }
}