﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using ChatServer.Util;
using ChatServer.Codec;
using ChatServer.Model;
using System.IO;
using ChatServer.DB;
using RabbitMQ.Client;
using System.Diagnostics;
using ZDZC_JT808Access;
using SuperSocket.SocketBase.Config;
using System.Configuration;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Framing.Impl;
using SuperSocket.SocketBase;

namespace ChatServer
{
    public partial class FServer : Form
    {
        // 队列名称  
        private readonly static string QUEUE_NAME = "task_queue";
        public FServer()
        {
            InitializeComponent();
            //关闭对文本框的非法线程操作检查
            TextBox.CheckForIllegalCrossThreadCalls = false;
            Label.CheckForIllegalCrossThreadCalls = false;
        }


        /// <summary>
        /// 启动服务
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void btnStartService_Click(object sender, EventArgs e)
        {

            var protocolServer = new JT808ProtocolServer();
            var serverConfig = new ServerConfig();

            string ipAddress = null;
            //获取服务端IPv4地址
            ipAddress = GetLocalIPv4Address().ToString();
            //获取多网卡外网IP
            //ipAddress = GetIP();

            //ip: 服务器监听的ip地址。你可以设置具体的地址，也可以设置为下面的值 Any
            if (ipBox.Text.Trim() != "" && portBox.Text.Trim() != "")
            {
                try {
                    serverConfig.Ip = ipBox.Text.Trim();
                    serverConfig.Port = int.Parse(portBox.Text.Trim());
                    serverConfig.MaxConnectionNumber = Int32.Parse(ConfigurationManager.AppSettings["maxConnectionNumber"]);
                } catch {
                    connMsg.AppendText("---->>>请输入正确的IP及端口!" + "\r\n");
                    return;
                }
                
            }
            else {
                connMsg.AppendText("---->>>请输入IP地址!" + "\r\n");
                return;
            }
           
            //lblPort.Text = serverConfig.Port.ToString();
            //lblIP.Text = serverConfig.Ip.ToString();
            //启动应用服务端口
            if (!protocolServer.Setup(serverConfig)) //启动时监听端口2017
            {
                connMsg.AppendText("---->>>服务启动失败，请检查IP地址!" + "\r\n");
                return;
            }


            //注册连接事件
            protocolServer.NewSessionConnected += protocolServer_NewSessionConnected;
            //注册请求事件
            protocolServer.NewRequestReceived += protocolServer_NewRequestReceived;
            //注册Session关闭事件
            protocolServer.SessionClosed += protocolServer_SessionClosed;
            //尝试启动应用服务
            if (!protocolServer.Start())
            {
                connMsg.AppendText("---->>>服务启动失败!" + "\r\n");
                return;
            }

            connMsg.AppendText("---->>>服务器已经启动,开始监听客户端传来的信息!" + "\r\n");


            btnStartService.Enabled = false;
        }


        int msgCount = 0;
        int i = 0;
        /// <summary>
        /// 接收消息
        /// </summary>
        /// <param name="session"></param>
        /// <param name="requestInfo"></param>
        /// notify:使用AppendText向控件写信息将极大影响消息写入rabbitmq的速度，为了保证rabbitmq的写入速率，建议关闭向控件写入信息，用日志记录即可
        void protocolServer_NewRequestReceived(HLProtocolSession session, HLProtocolRequestInfo requestInfo)
        {
            msgCount++;
            session.Logger.Info(msgCount + "条，\r\n" + GetCurrentTime() + "\n 收到客户端【" + session.RemoteEndPoint + "】\n信息：\r\n" + requestInfo.Body.all2 + "\r\n");
            //txtMsg.AppendText("\r\n" + GetCurrentTime() + "\n 收到客户端【" + session.RemoteEndPoint + "】\n信息：\r\n" + requestInfo.Body.all2 + "\r\n");

            if (requestInfo.Body.errorlog != null) {
                //LogHelper.WriteLog(typeof(FServer), "\r\n消息解析失败，格式错误！\r\n" + session.RemoteEndPoint + "发送：" + requestInfo.Body.all2);

                session.Logger.Error("\r\n消息解析失败，格式错误！IP:" + session.RemoteEndPoint + "发送：：\r\n" + requestInfo.Body.all2);
                //connMsg.AppendText("\r\n" + requestInfo.Body.errorlog+"消息内容：\r\n" + requestInfo.Body.all2);

            }

            //答应消息发送
            if (requestInfo.Body.getMsgRespBytes() != null)
            {
                session.Send(requestInfo.Body.getMsgRespBytes(), 0, requestInfo.Body.getMsgRespBytes().Length);
                //txtMsg.AppendText("\r\n" + GetCurrentTime() + "\n 向客户端【" + session.RemoteEndPoint + "】\n回执消息:\r\n" + ExplainUtils.convertStrMsg(requestInfo.Body.getMsgRespBytes()) + "\r\n");
            }
            //打印位置信息
            if (ExplainUtils.msg_id_terminal_location_info_upload == requestInfo.Body.msgHeader.msgId)
            {
                //string bodymsg = " 报警--->" + requestInfo.Body.locationInfo.alc
                //               + "\r\n 状态--->" + requestInfo.Body.locationInfo.bst
                //               + "\r\n 经度--->" + requestInfo.Body.locationInfo.lon.ToString()
                //               + "\r\n 纬度--->" + requestInfo.Body.locationInfo.lat.ToString()
                //               + "\r\n 高程--->" + requestInfo.Body.locationInfo.hgt.ToString()
                //               + "\r\n 速度--->" + requestInfo.Body.locationInfo.spd.ToString()
                //               + "\r\n 方向--->" + requestInfo.Body.locationInfo.agl.ToString()
                //               + "\r\n 时间--->" + requestInfo.Body.locationInfo.gtm.ToString()
                //               + "\r\n 里程--->" + requestInfo.Body.locationInfo.mlg.ToString()
                //               + "\r\n 油量--->" + requestInfo.Body.locationInfo.oil.ToString()
                //               + "\r\n 记录仪速度--->" + requestInfo.Body.locationInfo.spd2.ToString()
                //               + "\r\n 信号状态--->" + requestInfo.Body.locationInfo.est
                //               + "\r\n IO状态位--->" + requestInfo.Body.locationInfo.io
                //               + "\r\n 模拟量--->" + requestInfo.Body.locationInfo.ad1.ToString()
                //               + "\r\n 信号强度--->" + requestInfo.Body.locationInfo.yte.ToString()
                //               + "\r\n 定位卫星数--->" + requestInfo.Body.locationInfo.gnss.ToString();


                //txtMsg.AppendText("\r\n【解析消息内容:】\r\n" + bodymsg + "\r\n");

                rabbitMqTest(session,requestInfo);
            }
        }

        int count1 = 0;
        //rabbitmq消息测试
        public void rabbitMqTest(HLProtocolSession session, HLProtocolRequestInfo requestInfo)
        {
            string terminalPhone = requestInfo.Body.msgHeader.terminalPhone;
            var sendMessage = BitConverter.ToString(requestInfo.Body.getMsgBodyBytes()).Replace("-", " ");
            var sendbody = Encoding.UTF8.GetBytes(sendMessage);
            var factory = new ConnectionFactory();
            factory.HostName = "localhost";
            factory.Port = 5672;
            factory.UserName = "admin";
            factory.Password = "123456";
            factory.RequestedHeartbeat = 60;
            factory.AutomaticRecoveryEnabled = true;   //设置端口后自动恢复连接属性即可
            try
            {
                using (var connection = factory.CreateConnection()) //创建Socket连接
                {
                    using (var channel = connection.CreateModel())  //channel中包含几乎所有的API来供我们操作queue
                    {
                        //声明queue
                        channel.QueueDeclare(queue: QUEUE_NAME,//队列名
                                             durable: true,//是否持久化,在RabbitMQ服务重启的情况下，也不会丢失消息
                                             exclusive: false,//排他性
                                             autoDelete: false,//一旦客户端连接断开则自动删除queue
                                             arguments: null);//如果安装了队列优先级插件则可以设置优先级

                        //消息持久化
                        var properties = channel.CreateBasicProperties();
                        properties.Persistent = true;

                        channel.BasicPublish(exchange: "",//exchange名称
                                            routingKey: QUEUE_NAME,//如果存在exchange，则消息被发送到名为task_queue的客户端
                                            basicProperties: properties,
                                            body: sendbody);//消息体
                        count1++;
                        pubMsgCount.Text = "发送到消息队列消息条数：" + count1.ToString();
                        session.Logger.Info(GetCurrentTime() + "\n 发送到消息队列消息条数:"+ count1.ToString()+ "\r\n");
                        Console.WriteLine("[x] sent {0}", sendbody);
                    }
                    Console.WriteLine("Press [enter] to exit.");
                    Console.ReadLine();
                }
            }
            catch (RabbitMQ.Client.Exceptions.BrokerUnreachableException ex)
            {
                session.Logger.Error("\r\n"+ ex.Message.ToString() + "\r\n");
            }
            session.Logger.Info("\r\n写入rabbitmq消息:" + sendMessage + "\r\n");
        }

        /// <summary>
        /// Session关闭
        /// </summary>
        /// <param name="session"></param>
        /// <param name="value"></param>
        void protocolServer_SessionClosed(HLProtocolSession session, SuperSocket.SocketBase.CloseReason value)
        {
            //connMsg.AppendText("\r\n客户端【" + session.RemoteEndPoint + "】已经中断连接！断开原因：" + value + "\r\n");
            session.Logger.Info("\r\n客户端【" + session.RemoteEndPoint + "】已经中断连接！断开原因：" + value + "\r\n");
            session.Close();
        }

        /// <summary>
        /// 注册连接
        /// </summary>
        /// <param name="session"></param>
        void protocolServer_NewSessionConnected(HLProtocolSession session)
        {

            //connMsg.AppendText("\r\nIP:【" + session.RemoteEndPoint + "】 的客户端与您连接成功,现在你们可以开始通信了...\r\n");
            session.Logger.Info("\r\nIP:【" + session.RemoteEndPoint + "】 的客户端与您连接成功,现在你们可以开始通信了...\r\n");
        }



        /// <summary>  
        /// 获取外网ip地址  
        /// </summary>  
        private static string GetIP()
        {
            string tempip = "";
            try
            {
                WebRequest wr = WebRequest.Create("http://city.ip138.com/ip2city.asp");
                Stream s = wr.GetResponse().GetResponseStream();
                StreamReader sr = new StreamReader(s, Encoding.Default);
                string all = sr.ReadToEnd(); //读取网站的数据

                int start = all.IndexOf("您的IP地址是：[") + 9;
                int end = all.IndexOf("]", start);
                tempip = all.Substring(start, end - start);
                sr.Close();
                s.Close();
            }
            catch
            {
            }
            return tempip;
        }

        /// <summary>
        /// 获取本地IPv4地址
        /// </summary>
        /// <returns>本地IPv4地址</returns>
        public IPAddress GetLocalIPv4Address()
        {
            IPAddress localIPv4 = null;
            //获取本机所有的IP地址列表
            IPAddress[] ipAddressList = Dns.GetHostAddresses(Dns.GetHostName());
            foreach (IPAddress ipAddress in ipAddressList)
            {
                //判断是否是IPv4地址
                if (ipAddress.AddressFamily == AddressFamily.InterNetwork) //AddressFamily.InterNetwork表示IPv4 
                {
                    localIPv4 = ipAddress;
                }
                else
                    continue;
            }
            return localIPv4;
        }


        /// <summary>
        /// 获取当前系统时间
        /// </summary>
        public DateTime GetCurrentTime()
        {
            DateTime currentTime = new DateTime();
            currentTime = DateTime.Now;
            return currentTime;
        }

        //关闭服务端
        private void btnExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            txtMsg.Text = "";
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (txtMsg.Text == "") return;
            Clipboard.SetDataObject(txtMsg.Text);
            MessageBox.Show("文本内容已复制到剪切板！");
        }

        private void button3_Click(object sender, EventArgs e)
        {
            connMsg.Text = "";
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (connMsg.Text == "") return;
            Clipboard.SetDataObject(connMsg.Text);
            MessageBox.Show("文本内容已复制到剪切板！");
        }

    }
}