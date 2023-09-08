using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private Dictionary<string, Socket>  sockets = new Dictionary<string, Socket>();
        Socket server = null;
        private void button1_Click(object sender, EventArgs e)
        {
            Button btn = (Button)sender;
            btn.Enabled = false;
            //创建服务器
             server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            //绑定端口
            string ip = textBox1.Text;
            string port = textBox2.Text;
            IPAddress address = IPAddress.Parse(ip);
            address = IPAddress.Any;
            EndPoint endPoint = new IPEndPoint(address, int.Parse(port));
            server.Bind(endPoint);

            // 开始监听
            server.Listen(20);  // 监听客户端的个数
            Reder("连接成功！");

            // 接收消息
            Task task =Task.Run(() => {

                Accepte(server);
            });


        }


        // 会话消息
        public  void Reder(string str)
        {
            textBox3.Invoke(new Action(() =>
            {
                textBox3.AppendText($"{DateTime.Now}:{str}\r\n");
            }));
        }

        // 接收 客户端消息

        public void Accepte(Socket server)
        {
            while (true)
            {
                Socket kehu = server.Accept();
                string ip = kehu.RemoteEndPoint.ToString();
                sockets[ip] = kehu;
                Reder($"客户端{ip}已连接");
                comboBox1.Invoke(new Action(() =>
                {
                    comboBox1.Items.Add(ip);
                }));

                // 创建线程来接收 客户端消息
              Task task2 = Task.Run(() =>
                {
                    AcceptMessge(kehu,ip);
                });
            }

        }

        //  接收消息
        public void AcceptMessge(Socket socket,string ip)
        {
            try {

                while (true)
                {
                    // 创建数组 接收消息
                    byte[] buff = new byte[1024 * 1024 * 2];
                    int len = socket.Receive(buff);

                    if (len <= 0)
                    {
                        Down(socket, ip);
                        break;
                    }

                    //把buff数组转换成字符串
                    string content = Encoding.UTF8.GetString(buff, 0, len);
                    //接收到消息，把接收到消息显示到文本框中
                    Reder($"接收到来自客户端{ip}的消息:{content}");
                }
            }
            catch (Exception e)
            {
                Down(socket, ip);
            }
        }

        // 发送消息
        private void button4_Click(object sender, EventArgs e)
        {
            //获取用户选择的客户端的socket对象

            if (comboBox1.SelectedItem != null)
            {
                Socket sendSocket = sockets[(string)comboBox1.SelectedItem];
                if (sendSocket == null) return;
                //获取文本内容
                string content = textBox5.Text.Trim();
               if (!string.IsNullOrEmpty(content)) {

                    Reder($"服务器发送的消息:{textBox5.Text.Trim()}");
                    byte[] buff = Encoding.UTF8.GetBytes(content);
                    //创建一个List数组用来标记当前发送的消息类型
                    List<byte> lists = new List<byte>();
                    lists.Add(1);  //第0个元素是1就表示文本
                    lists.AddRange(buff);  //除了第0个元素之外，其他都是内容
                                           //使用socket发送的消息
                    sendSocket.Send(lists.ToArray());
                    textBox5.Text = "";
                }
                
            }
            else
            {
                MessageBox.Show("请选择一个客户端");
            }

        }
        public void Down(Socket socket,string ip)
        {
            sockets.Remove(ip);
            comboBox1.Invoke((Action)(() =>
            {
                comboBox1.Items.Remove(ip);
            }));
            comboBox1.Items.Remove(ip);
            Reder($"客户端{ip}已下线");
            socket.Close();
            
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (comboBox1.SelectedItem != null)
            {
                Socket sendSocket = sockets[(string)comboBox1.SelectedItem];
                if (sendSocket == null) return;

                //约定文本消息的  datas[0] = 1
                List<byte> datas = new List<byte>();
                datas.Add(2);

                sendSocket.Send(datas.ToArray());
            }
            else
            {
                MessageBox.Show("请选择一个客户端");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();   
            openFileDialog.Title = "请选择要发送的文件";
            openFileDialog.InitialDirectory = @"D:\C#\C#进阶\新建文件夹";
            openFileDialog.Filter = "所有文件|*.*";
            openFileDialog.ShowDialog();

            string fileName = openFileDialog.FileName;
            if (string.IsNullOrEmpty(fileName))
            {
                //MessageBox.Show("请选择一个文件");
            }
            else
            {
                textBox4.Text = openFileDialog.FileName;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (comboBox1.SelectedItem != null)
            {
                Socket sendSocket = sockets[(string)comboBox1.SelectedItem];
                if (sendSocket == null) return;

                string path = textBox4.Text;
                byte[] buff = File.ReadAllBytes(path);
                List<byte> datas = new List<byte>();
                datas.Add(3);
                datas.AddRange(buff);
                sendSocket.Send(datas.ToArray());

            }
            else
            {
                MessageBox.Show("请选择一个客户端");
            }






        }

     
    }
}
