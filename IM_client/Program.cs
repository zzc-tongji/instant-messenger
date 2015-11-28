using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Threading;
using System.IO;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.Drawing;
using System.Collections;

namespace IM
{
    static class Program
    {
        /*      类：字段      */

        //主窗体
        static FormMain _formMain;
        //应用程序状态
        public static Int32 _status;
        //输入参数
        public static String[] _parameter;
        //套接字
        public static IPEndPoint _ipEndPoint;
        public static Socket _socket;
        //接收包缓冲区
        public static im_package _receivePackage;
        //发送包缓冲区
        public static im_package _sendPackage;
        //文件包处理队列
        public static im_package _receivePackageFile;
        public static Queue<im_package> _receivePackageFileQueue;
        //斗地主包处理队列
        public static im_package _receivePackageDoudizhu;
        public static Queue<im_package> _receivePackageDoudizhuQueue;
        //当前用户
        public static User _user;
        //用户列表
        public static List<ClientItem> _clientTable;
        //文件传输装置
        public static FileTransport _fileTransport;
        //日志文件写入器
        static FileStream _logFile;
        static StreamWriter _logFileWriter;
        public static Boolean _logEnable = false;
        public static Boolean _logConsole = false;
        //编解码器
        public static Encoding _encoder;
        //计时器
        static Stopwatch _stopwatch;
        //斗地主相关
        public static String doudizhuExe = "doudizhu.exe";
        //严格调试模式
        public static Boolean _strictDebug = false;
        //信号量
        static Semaphore _semReadLogin;
        static Semaphore _semReadTable;
        static Semaphore _semReadText;
        static Semaphore _semReadFile;
        static Semaphore _semReadDoudizhu;
        public static Semaphore _semReceive;
        public static Semaphore _semWrite;
        public static Semaphore _semSend;
        public static Semaphore _semLog;

        /*      类：方法      */

        //图形化用户交互界面线程（GUI线程，主线程）
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            //初始化
            _status = 0;
            _parameter = args;
            _ipEndPoint = new IPEndPoint(0, 0);
            _socket = null;
            _receivePackage = new im_package(true);
            _sendPackage = new im_package(true);
            _receivePackageFile = null;
            _receivePackageFileQueue = new Queue<im_package>();
            _receivePackageDoudizhu = null;
            _receivePackageDoudizhuQueue = new Queue<im_package>();
            _user = new User();
            _clientTable = new List<ClientItem>();
            _fileTransport = new FileTransport();
            if (_logEnable && _logConsole == false)
            {
                _logFile = new FileStream("IM_client.xml", FileMode.Append, FileAccess.Write);
                _logFileWriter = new StreamWriter(_logFile, Encoding.UTF8);
            }
            else
            {
                _logFile = null;
                _logFileWriter = null;
            }
            //_logEnable
            //_logConsole
            _encoder = Encoding.GetEncoding("utf-8");
            //_strictDebug
            _stopwatch = new Stopwatch();
            //doudizhuExe
            InitializeSemaphore();
            //日志
            if (_logEnable)
            {
                //GUI线程睡眠，直到当前时刻没有其他线程正在写日志
                _semLog.WaitOne();
                //GUI线程被唤醒，写日志
                //写日志文件
                WriteLog("Application run.");
                //写日志完毕，唤醒其他等待写日志的线程
                _semLog.Release(1);
            }
            //启动
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            _formMain = new FormMain();
            Application.Run(_formMain);
            //日志
            if (_logEnable)
            {
                //GUI线程睡眠，直到当前时刻没有其他线程正在写日志
                _semLog.WaitOne();
                //GUI线程被唤醒，写日志
                //写日志文件
                WriteLog("Application terminates.");
                //写日志完毕，唤醒其他等待写日志的线程
                _semLog.Release(1);
            }
        }

        //网络接收线程（Receive线程）
        public static void ThreadReceive()
        {
            Int32 return_value;
            Int32 content_lenth;
            Byte[] heartBeat = new Byte[4];
            heartBeat[0] = Byte.MaxValue;
            heartBeat[1] = Byte.MaxValue;
            heartBeat[2] = Byte.MaxValue;
            heartBeat[3] = Byte.MaxValue;
            im_package receivePackage;
            while (true)
            {
                //Receive线程睡眠，直到接收到的包被处理
                _semReceive.WaitOne();
                //Receive线程被唤醒，接收下一个包
                //尝试接收 4 Bit
                try
                {
                    return_value = _socket.Receive(_receivePackage.buffer, 4, SocketFlags.Peek);
                    while (return_value < 4)
                    {
                        if (return_value == 0)
                        {
                            //已断开
                            _formMain.Invoke(new MethodInvoker(delegate
                            {
                                _formMain.Disconnected();
                            }));
                            Thread.Sleep(60);
                        }
                    }
                }
                catch (SocketException ex)
                {
                    //日志
                    if (_logEnable)
                    {
                        //Receive线程睡眠，直到当前时刻没有其他线程正在写日志
                        _semLog.WaitOne();
                        //Receive线程被唤醒，写日志
                        //写日志文件
                        WriteLog(ex.ToString());
                        //写日志完毕，唤醒其他等待写日志的线程
                        _semLog.Release(1);
                    }
                    //已断开（异常）
                    _formMain.Invoke(new MethodInvoker(delegate
                    {
                        _formMain.DisconnectedAbnormally();
                    }));
                    Thread.Sleep(60);
                }
                if (BitConverter.ToInt32(_receivePackage.buffer, 0) == -1)  //0xFFFFFFFF
                {
                    //心跳包
                    //接收 4 Bit
                    _socket.Receive(_receivePackage.buffer, 4, SocketFlags.None);
                    //发送
                    //Receive线程睡眠，直到可以占用发送包缓冲区
                    _semWrite.WaitOne();
                    //Receive线程被唤醒，发送心跳包
                    try
                    {
                        send_confirm(heartBeat, 4, SocketFlags.None);
                    }
                    catch (SocketException ex)
                    {
                        //日志
                        if (_logEnable)
                        {
                            //Receive线程睡眠，直到当前时刻没有其他线程正在写日志
                            _semLog.WaitOne();
                            //Receive线程被唤醒，写日志
                            //写日志文件
                            WriteLog(ex.ToString());
                            //写日志完毕，唤醒其他等待写日志的线程
                            _semLog.Release(1);
                        }
                        //已断开（异常）
                        _formMain.Invoke(new MethodInvoker(delegate
                        {
                            _formMain.DisconnectedAbnormally();
                        }));
                        Thread.Sleep(60);
                    }
                    //解除对发送包缓冲区的占用
                    _semWrite.Release(1);
                    /*
                     * 实际上，Receive线程并未使用发送包缓冲区。
                     * 占用发送包缓冲区的目的是，确保在发送心跳包的的时候，Send线程不发送任何数据。
                     * 这就避免了心跳包被混杂在数据包中一起发送。
                     */
                    //日志
                    if (_logEnable)
                    {
                        //Receive线程睡眠，直到当前时刻没有其他线程正在写日志
                        _semLog.WaitOne();
                        //Receive线程被唤醒，写日志
                        //写日志文件
                        WriteLog(true, true, null);
                        //写日志完毕，唤醒其他等待写日志的线程
                        _semLog.Release(1);
                    }
                    //心跳包处理完毕，唤醒自己
                    _semReceive.Release(1);
                }
                else
                {
                    //数据包
                    //尝试接收 100 Bit
                    try
                    {
                        return_value = _socket.Receive(_receivePackage.buffer, im_package.size_head, SocketFlags.Peek);
                        while (return_value < im_package.size_head)
                        {
                            if (return_value == 0)
                            {
                                //已断开
                                _formMain.Invoke(new MethodInvoker(delegate
                                {
                                    _formMain.Disconnected();
                                }));
                                Thread.Sleep(60);
                            }
                        }
                    }
                    catch (SocketException ex)
                    {
                        //日志
                        if (_logEnable)
                        {
                            //Receive线程睡眠，直到当前时刻没有其他线程正在写日志
                            _semLog.WaitOne();
                            //Receive线程被唤醒，写日志
                            //写日志文件
                            WriteLog(ex.ToString());
                            //写日志完毕，唤醒其他等待写日志的线程
                            _semLog.Release(1);
                        }
                        //已断开（异常）
                        _formMain.Invoke(new MethodInvoker(delegate
                        {
                            _formMain.DisconnectedAbnormally();
                        }));
                        Thread.Sleep(60);
                    }
                    //预读变长部分的长度
                    content_lenth = _receivePackage.preread();
                    //尝试接收 100 Bit ~ 65636 Bit
                    try
                    {
                        return_value = _socket.Receive(_receivePackage.buffer, im_package.size_head + content_lenth, SocketFlags.Peek);
                        while (return_value < im_package.size_head + content_lenth)
                        {
                            if (return_value == 0)
                            {
                                //已断开
                                _formMain.Invoke(new MethodInvoker(delegate
                                {
                                    _formMain.Disconnected();
                                }));
                                Thread.Sleep(60);
                            }
                        }
                    }
                    catch (SocketException ex)
                    {
                        //日志
                        if (_logEnable)
                        {
                            //Receive线程睡眠，直到当前时刻没有其他线程正在写日志
                            _semLog.WaitOne();
                            //Receive线程被唤醒，写日志
                            //写日志文件
                            WriteLog(ex.ToString());
                            //写日志完毕，唤醒其他等待写日志的线程
                            _semLog.Release(1);
                        }
                        //已断开（异常）
                        _formMain.Invoke(new MethodInvoker(delegate
                        {
                            _formMain.DisconnectedAbnormally();
                        }));
                        Thread.Sleep(60);
                    }
                    //接收 100 Bit ~ 65636 Bit
                    _socket.Receive(_receivePackage.buffer, im_package.size_head + content_lenth, SocketFlags.None);
                    //拆包
                    _receivePackage.split();
                    //根据类型，然后唤醒相关线程来处理
                    switch (_receivePackage.type)
                    {
                        case im_package.tp_login:
                            //唤醒Login线程
                            _semReadLogin.Release(1);
                            break;
                        case im_package.tp_table:
                            //唤醒Table线程
                            _semReadTable.Release(1);
                            break;
                        case im_package.tp_text:
                            //唤醒Text线程
                            _semReadText.Release(1);
                            break;
                        case im_package.tp_file:
                            //构建_receivePackage的深度复制（buffer除外）
                            receivePackage = new im_package(false);
                            receivePackage.sender = _receivePackage.sender;
                            receivePackage.information = _receivePackage.information;
                            receivePackage.receiver_number = _receivePackage.receiver_number;
                            for (Int32 i = 0; i < 20; ++i)
                            {
                                receivePackage.receiver[i] = _receivePackage.receiver[i];
                            }
                            receivePackage.content_lenth = _receivePackage.content_lenth;
                            for (Int32 i = 0; i < receivePackage.content_lenth; ++i)
                            {
                                receivePackage.content[i] = _receivePackage.content[i];
                            }
                            //加入文件包处理队列
                            _receivePackageFileQueue.Enqueue(receivePackage);
                            //考虑到File线程和Receive线程的并行性，唤醒自己
                            _semReceive.Release(1);
                            //唤醒File线程
                            _semReadFile.Release(1);
                            break;
                        case im_package.tp_doudizhu:
                            //构建_receivePackage的深度复制（buffer除外）
                            receivePackage = new im_package(false);
                            receivePackage.sender = _receivePackage.sender;
                            receivePackage.information = _receivePackage.information;
                            receivePackage.receiver_number = _receivePackage.receiver_number;
                            for (Int32 i = 0; i < 20; ++i)
                            {
                                receivePackage.receiver[i] = _receivePackage.receiver[i];
                            }
                            receivePackage.content_lenth = _receivePackage.content_lenth;
                            for (Int32 i = 0; i < receivePackage.content_lenth; ++i)
                            {
                                receivePackage.content[i] = _receivePackage.content[i];
                            }
                            //加入斗地主包处理队列
                            _receivePackageDoudizhuQueue.Enqueue(receivePackage);
                            //考虑到Doudizhu线程和Receive线程的并行性，唤醒自己
                            _semReceive.Release(1);
                            //唤醒Doudizhu线程
                            _semReadDoudizhu.Release(1);
                            break;
                        default:
                            //服务端异常 / 客户端异常：数据包格式错误
                            if (_strictDebug)
                            {
                                throw new Exception();
                            }
                            break;
                    }
                    //日志
                    if (_logEnable)
                    {
                        //Receive线程睡眠，直到当前时刻没有其他线程正在写日志
                        _semLog.WaitOne();
                        //Receive线程被唤醒，写日志
                        //写日志文件
                        WriteLog(true, false, _receivePackage);
                        //写日志完毕，唤醒其他等待写日志的线程
                        _semLog.Release(1);
                    }
                }
            }
        }

        //网络发送线程（Send线程）
        public static void ThreadSend()
        {
            while (true)
            {
                //Send线程睡眠，直到有包需要发送
                _semSend.WaitOne();
                //Send线程被唤醒，发送下一个包
                //打包
                _sendPackage.build();
                //发送
                try
                {
                    send_confirm(_sendPackage.buffer, im_package.size_head + _sendPackage.content_lenth, SocketFlags.None);
                }
                catch (SocketException ex)
                {
                    //日志
                    if (_logEnable)
                    {
                        //Send线程睡眠，直到当前时刻没有其他线程正在写日志
                        _semLog.WaitOne();
                        //Send线程被唤醒，写日志
                        //写日志文件
                        WriteLog(ex.ToString());
                        //写日志完毕，唤醒其他等待写日志的线程
                        _semLog.Release(1);
                    }
                    //已断开（异常）
                    _formMain.Invoke(new MethodInvoker(delegate
                    {
                        _formMain.DisconnectedAbnormally();
                    }));
                    Thread.Sleep(60);
                }
                //发送完毕，解除对发送包缓冲区的占用
                _semWrite.Release(1);
                //日志
                if (_logEnable)
                {
                    //Send线程睡眠，直到当前时刻没有其他线程正在写日志
                    _semLog.WaitOne();
                    //Send线程被唤醒，写日志
                    //写日志文件
                    WriteLog(false, false, _sendPackage);
                    //写日志完毕，唤醒其他等待写日志的线程
                    _semLog.Release(1);
                }
            }
        }

        //数据处理线程：登录包（Login线程）
        public static void ThreadLogin()
        {
            while (true)
            {
                //Login线程睡眠，直到成功接收类型为tp_login的数据包
                _semReadLogin.WaitOne();
                //Login线程被唤醒，处理数据包
                if (_receivePackage.sender == 0)
                {
                    //当且仅当包的发送者为服务端，处理包
                    switch (_receivePackage.information)
                    {
                        case im_package.lg_ask:
                            //登录中
                            _formMain.Invoke(new MethodInvoker(delegate
                            {
                                _formMain.LogingIn();
                            }));
                            break;
                        case im_package.lg_accept:
                            //已登录
                            _formMain.Invoke(new MethodInvoker(delegate
                            {
                                _formMain.LogedIn();
                            }));
                            break;
                        case im_package.lg_reject_password:
                            //主窗体控件：锁定主窗体
                            _formMain.Invoke(new MethodInvoker(delegate
                            {
                                _formMain.Enabled = false;
                            }));
                            //输出信息
                            MessageBox.Show("密码错误！", "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            //服务端会主动断开连接，由Receive线程探知并处理
                            break;
                        case im_package.lg_reject_username:
                            //主窗体控件：锁定主窗体
                            _formMain.Invoke(new MethodInvoker(delegate
                            {
                                _formMain.Enabled = false;
                            }));
                            //输出信息
                            MessageBox.Show("用户名不存在！", "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            //服务端会主动断开连接，由Receive线程探知并处理
                            break;
                        case im_package.lg_logout:
                            //主窗体控件：锁定主窗体
                            _formMain.Invoke(new MethodInvoker(delegate
                            {
                                _formMain.Enabled = false;
                            }));
                            //服务端会主动断开连接，由Receive线程探知并处理
                            break;
                        case im_package.lg_logout_force:
                            //主窗体控件：锁定主窗体
                            _formMain.Invoke(new MethodInvoker(delegate
                            {
                                _formMain.Enabled = false;
                            }));
                            //输出信息
                            MessageBox.Show("您的用户名已在 " + _encoder.GetString(_receivePackage.content, 0, _receivePackage.content_lenth) + " 异地登陆，您被迫下线！", "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            //服务端会主动断开连接，由Receive线程探知并处理
                            break;
                        case im_package.lg_answer:
                        case im_package.lg_logout_request:
                            //服务端异常：不应发送这个包
                            if (_strictDebug)
                            {
                                throw new Exception();
                            }
                            break;
                        case im_package.lg_reject_format:
                            //客户端异常：不应发送格式错误的登陆包
                            if (_strictDebug)
                            {
                                throw new Exception();
                            }
                            break;
                        default:
                            //服务端异常 / 客户端异常：数据包格式错误
                            if (_strictDebug)
                            {
                                throw new Exception();
                            }
                            break;
                    }
                }
                //处理完毕，唤醒Receive线程
                _semReceive.Release(1);
            }
        }

        //数据处理线程：客户端列表包（Table线程）
        public static void ThreadTable()
        {
            Int32 tempInt32 = 0;
            while (true)
            {
                //Table线程睡眠，直到成功接收类型为tp_table的数据包
                _semReadTable.WaitOne();
                //Table线程被唤醒，处理数据包
                if (_receivePackage.sender == 0)
                {
                    //当且仅当包的发送者为服务端，处理包
                    switch (_receivePackage.information)
                    {
                        case im_package.tb_all:
                            _clientTable.Clear();
                            if (_strictDebug)
                            {
                                if (_receivePackage.content_lenth == 0)
                                {
                                    //服务端异常：该包的content不应为空
                                    throw new Exception();
                                }
                                Int32 test1 = 0;
                                HashSet<Int32> test2 = new HashSet<Int32>();
                                for (Int32 i = 0; i < _receivePackage.content_lenth; i += 4)
                                {
                                    test1 = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(_receivePackage.content, i));
                                    if (test2.Add(test1) == false)
                                    {
                                        //服务端异常：服务端的客户端列表中存在着相同用户名的项
                                        throw new Exception();
                                    }
                                    _clientTable.Add(new ClientItem(test1, false, false));
                                }
                            }
                            else
                            {
                                for (Int32 i = 0; i < _receivePackage.content_lenth; i += 4)
                                {
                                    _clientTable.Add(new ClientItem(IPAddress.NetworkToHostOrder(BitConverter.ToInt32(_receivePackage.content, i)), false, false));
                                }
                            }
                            //更新客户端显示器
                            _formMain.Invoke(new MethodInvoker(delegate
                            {
                                _formMain.UpdateClientTable();
                            }));
                            break;
                        case im_package.tb_online:
                            if (_clientTable.Count > 0)
                            {
                                //当且仅当处理过tb_all包，处理tb_online包
                                tempInt32 = 0;
                                foreach (ClientItem ci in _clientTable)
                                {
                                    ci.Online = false;
                                    for (Int32 i = tempInt32; i < _receivePackage.content_lenth; i += 4)
                                    {
                                        if (ci.UsernameInt32 == IPAddress.NetworkToHostOrder(BitConverter.ToInt32(_receivePackage.content, i)))
                                        {
                                            ci.Online = true;
                                            tempInt32 += 4;
                                            break;
                                        }
                                    }
                                }
                            }
                            //更新客户端显示器
                            _formMain.Invoke(new MethodInvoker(delegate
                            {
                                _formMain.UpdateClientTable();
                            }));
                            break;
                        default:
                            //服务端异常 / 客户端异常：数据包格式错误
                            if (_strictDebug)
                            {
                                throw new Exception();
                            }
                            break;
                    }
                }
                //处理完毕，唤醒Receive线程
                _semReceive.Release(1);
            }
        }

        //数据处理线程：文本包（Text线程）
        public static void ThreadText()
        {
            while (true)
            {
                //Text线程睡眠，直到成功接收类型为tp_text的数据包
                _semReadText.WaitOne();
                //Text线程被唤醒，处理数据包
                switch (_receivePackage.information)
                {
                    case im_package.tx_content:
                    case im_package.tx_reply:
                        //更新聊天记录框
                        _formMain.Invoke(new MethodInvoker(delegate
                        {
                            _formMain.UpdateReceiveContent();
                        }));
                        break;
                    default:
                        //异常
                        break;
                }
                //处理完毕，唤醒Receive线程
                _semReceive.Release(1);
            }
        }

        //数据处理线程：文件包（File线程）
        public static void ThreadFile()
        {
            Boolean finish = false;
            Boolean firstPackage = true;
            String extensionName = null;
            while (true)
            {
                //File线程睡眠，直到成功接收类型为tp_file的数据包
                _semReadFile.WaitOne();
                //File线程被唤醒
                //从文件包处理队列中取出一个文件包
                _receivePackageFile = _receivePackageFileQueue.Dequeue();
                //处理
                if (_fileTransport.PeerUsername == FileTransport._PeerNone)
                {
                    //握手阶段（文件接收方）
                    switch (_receivePackageFile.information)
                    {
                        case im_package.fl_request:
                            //处理文件传输请求
                            _formMain.Invoke(new MethodInvoker(delegate
                            {
                                _formMain.DealWithFileTransmissionRequest();
                            }));
                            firstPackage = true;
                            break;
                        case im_package.fl_content:
                        case im_package.fl_accept:
                        case im_package.fl_reject:
                        case im_package.fl_reply:
                        case im_package.fl_interrupt:
                        case im_package.fl_finish:
                        case im_package.fl_offline:
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    //传输阶段
                    if (_receivePackageFile.sender == 0)
                    {
                        //文件包来自服务端
                        switch (_receivePackageFile.information)
                        {
                            case im_package.fl_offline:
                                //状态：已登录
                                _status = 2;
                                //主窗体控件
                                _formMain.Invoke(new MethodInvoker(delegate
                                {
                                    _formMain.LabelStatus.Text = "已登录：" + _user.Username.ToString();
                                    _formMain.LabelStatus.ForeColor = Color.Blue;
                                }));
                                if (Program._fileTransport.ReceiveFilePath != null)
                                {
                                    //接收重置
                                    Program._fileTransport.ReceiveDisable(true);
                                }
                                else if (Program._fileTransport.SendFilePath != null)
                                {
                                    //发送重置
                                    Program._fileTransport.SendDisable();
                                }
                                //主窗体控件
                                _formMain.Invoke(new MethodInvoker(delegate
                                {
                                    _formMain.Enabled = false;
                                }));
                                //提示
                                MessageBox.Show("对方已下线。", "", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                //主窗体控件
                                _formMain.Invoke(new MethodInvoker(delegate
                                {
                                    _formMain.UpdateButtonEnable();
                                    _formMain.Enabled = true;
                                }));
                                //对端注销
                                _fileTransport.PeerUsername = FileTransport._PeerNone;
                                break;
                            case im_package.fl_content:
                            case im_package.fl_reply:
                            case im_package.fl_request:
                            case im_package.fl_accept:
                            case im_package.fl_reject:
                            case im_package.fl_interrupt:
                            case im_package.fl_finish:
                                break;
                            default:
                                break;
                        }
                    }
                    else if (_receivePackageFile.sender == _fileTransport.PeerUsername)
                    {
                        //文件包来自完成握手的客户端
                        if (_fileTransport.ReceiveFilePath != null)
                        {
                            //文件接收方
                            switch (_receivePackageFile.information)
                            {
                                case im_package.fl_content:
                                    //写文件
                                    _fileTransport.ReceiveFileStream.Write(_receivePackageFile.content, 0, _receivePackageFile.content_lenth);
                                    _fileTransport.FileSizeNow += _receivePackageFile.content_lenth;
                                    //File线程睡眠，直到能够占用发送包缓冲区
                                    _semWrite.WaitOne();
                                    //File线程被唤醒，填充发送包缓冲区
                                    _sendPackage.sender = _user.Username;
                                    _sendPackage.type = im_package.tp_file;
                                    _sendPackage.information = im_package.fl_reply;
                                    _sendPackage.receiver_number = 1;
                                    _sendPackage.receiver[0] = _fileTransport.PeerUsername;
                                    _sendPackage.content_lenth = 0;
                                    //填充完毕，唤醒Send线程
                                    _semSend.Release(1);
                                    if (firstPackage == false)
                                    {
                                        //计时器
                                        _stopwatch.Stop();
                                        //主窗体控件
                                        _formMain.Invoke(new MethodInvoker(delegate
                                        {
                                            _formMain.LabelStatus.Text = "文件接收中：" + _fileTransport.FileSizeNow.ToString() + " Bit / " + _fileTransport.FileSizeAll.ToString() + " Bit, " + String.Format("{0,5:f0} KiB/s", (0.97656 * im_package.size_content_max / _stopwatch.ElapsedMilliseconds));
                                        }));
                                        //计时器
                                        _stopwatch.Reset();
                                        _stopwatch.Start();
                                    }
                                    else
                                    {
                                        //主窗体控件
                                        _formMain.Invoke(new MethodInvoker(delegate
                                        {
                                            _formMain.LabelStatus.Text = "文件接收中：" + _fileTransport.FileSizeNow.ToString() + " Bit / " + _fileTransport.FileSizeAll.ToString() + " Bit";
                                        }));
                                        firstPackage = false;
                                        //计时器
                                        _stopwatch.Start();
                                    }
                                    break;
                                case im_package.fl_interrupt:
                                    //状态：已登录
                                    _status = 2;
                                    //主窗体控件
                                    _formMain.Invoke(new MethodInvoker(delegate
                                    {
                                        _formMain.LabelStatus.Text = "已登录：" + _user.Username.ToString();;
                                        _formMain.LabelStatus.ForeColor = Color.Blue;
                                    }));
                                    //接收重置
                                    _fileTransport.ReceiveDisable(true);
                                    //主窗体控件
                                    _formMain.Invoke(new MethodInvoker(delegate
                                    {
                                        _formMain.Enabled = false;
                                    }));
                                    //提示
                                    MessageBox.Show(_fileTransport.PeerUsername.ToString() + "：中止文件传输。", "", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    //主窗体控件
                                    _formMain.Invoke(new MethodInvoker(delegate
                                    {
                                        //主窗体控件
                                        _formMain.UpdateButtonEnable();
                                        _formMain.Enabled = true;
                                    }));
                                    //对端注销
                                    _fileTransport.PeerUsername = FileTransport._PeerNone;
                                    break;
                                case im_package.fl_finish:
                                    //状态：已登录
                                    _status = 2;
                                    //主窗体控件
                                    _formMain.Invoke(new MethodInvoker(delegate
                                    {
                                        _formMain.LabelStatus.Text = "已登录：" + _user.Username.ToString();;
                                        _formMain.LabelStatus.ForeColor = Color.Blue;
                                    }));
                                    if (_strictDebug)
                                    {
                                        //文件未能完整传输
                                        if (_fileTransport.FileSizeAll != _fileTransport.FileSizeNow)
                                        {
                                            throw new Exception();
                                        }
                                    }
                                    //若收到图片文件，则直接打开
                                    extensionName = _fileTransport.ReceiveFilePath.Substring(_fileTransport.ReceiveFilePath.LastIndexOf('.')).ToLower();
                                    switch (extensionName)
                                    {
                                        case ".bmp":
                                        case ".jpg":
                                        case ".jpeg":
                                        case ".tif":
                                        case ".tiff":
                                        case ".gif":
                                            Process.Start("iexplore.exe", _fileTransport.ReceiveFilePath);
                                            break;
                                        default:
                                            break;
                                    }
                                    extensionName = null;
                                    //接收重置
                                    _fileTransport.ReceiveDisable(false);
                                    //主窗体控件
                                    _formMain.Invoke(new MethodInvoker(delegate
                                    {
                                        _formMain.Enabled = false;
                                    }));
                                    //提示
                                    MessageBox.Show(_fileTransport.PeerUsername.ToString() + "：文件接收完毕。", "", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    //主窗体控件
                                    _formMain.Invoke(new MethodInvoker(delegate
                                    {
                                        _formMain.UpdateButtonEnable();
                                        _formMain.Enabled = true;
                                    }));
                                    //对端注销
                                    _fileTransport.PeerUsername = FileTransport._PeerNone;
                                    break;
                                case im_package.fl_reply:
                                case im_package.fl_request:
                                case im_package.fl_accept:
                                case im_package.fl_reject:
                                case im_package.fl_offline:
                                    break;
                                default:
                                    break;
                            }
                        }
                        else if (_fileTransport.SendFilePath != null)
                        {
                            //文件发送方
                            switch (_receivePackageFile.information)
                            {
                                case im_package.fl_reply:
                                    //File线程睡眠，直到能够占用发送包缓冲区
                                    _semWrite.WaitOne();
                                    //File线程被唤醒，填充发送包缓冲区
                                    _sendPackage.content_lenth = _fileTransport.SendFileStream.Read(_sendPackage.content, 0, im_package.size_content_max);
                                    _fileTransport.FileSizeNow += _sendPackage.content_lenth;
                                    if (_sendPackage.content_lenth == 0)
                                    {
                                        //文件已读完
                                        _sendPackage.sender = _user.Username;
                                        _sendPackage.type = im_package.tp_file;
                                        _sendPackage.information = im_package.fl_finish;
                                        _sendPackage.receiver_number = 1;
                                        _sendPackage.receiver[0] = _fileTransport.PeerUsername;
                                        finish = true;
                                    }
                                    else
                                    {
                                        //文件未读完
                                        _sendPackage.sender = _user.Username;
                                        _sendPackage.type = im_package.tp_file;
                                        _sendPackage.information = im_package.fl_content;
                                        _sendPackage.receiver_number = 1;
                                        _sendPackage.receiver[0] = _fileTransport.PeerUsername;
                                    }
                                    //填充完毕，唤醒Send线程
                                    _semSend.Release(1);
                                    if (finish)
                                    {
                                        //状态：已登录
                                        _status = 2;
                                        //主窗体控件
                                        _formMain.Invoke(new MethodInvoker(delegate
                                        {
                                            _formMain.LabelStatus.Text = "已登录：" + _user.Username.ToString();;
                                            _formMain.LabelStatus.ForeColor = Color.Blue;
                                        }));
                                        //发送重置
                                        _fileTransport.SendDisable();
                                        //主窗体控件
                                        _formMain.Invoke(new MethodInvoker(delegate
                                        {
                                            _formMain.Enabled = false;
                                        }));
                                        //提示
                                        MessageBox.Show(_fileTransport.PeerUsername.ToString() + "：文件发送完毕。", "", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                        //主窗体控件
                                        _formMain.Invoke(new MethodInvoker(delegate
                                        {
                                            _formMain.UpdateButtonEnable();
                                            _formMain.Enabled = true;
                                        }));
                                        //对端注销
                                        _fileTransport.PeerUsername = FileTransport._PeerNone;
                                    }
                                    else
                                    {
                                        //计时器
                                        _stopwatch.Stop();
                                        //主窗体控件
                                        _formMain.Invoke(new MethodInvoker(delegate
                                        {
                                            _formMain.LabelStatus.Text = "文件发送中：" + _fileTransport.FileSizeNow.ToString() + " Bit / " + _fileTransport.FileSizeAll.ToString() + " Bit, " + String.Format("{0,5:f0} KiB/s", (0.97656 * im_package.size_content_max / _stopwatch.ElapsedMilliseconds));
                                        }));
                                        //计时器
                                        _stopwatch.Reset();
                                        _stopwatch.Start();
                                    }
                                    break;
                                case im_package.fl_accept:
                                    //状态：文件传输中
                                    _status = 3;
                                    //主窗体控件
                                    _formMain.Invoke(new MethodInvoker(delegate
                                    {
                                        _formMain.UpdateButtonEnable();
                                    }));
                                    //发送第1个数据包
                                    finish = false;
                                    //File线程睡眠，直到能够占用发送包缓冲区
                                    _semWrite.WaitOne();
                                    //File线程被唤醒，填充发送包缓冲区
                                    _sendPackage.content_lenth = _fileTransport.SendFileStream.Read(_sendPackage.content, 0, im_package.size_content_max);
                                    _fileTransport.FileSizeNow += _sendPackage.content_lenth;
                                    if (_sendPackage.content_lenth == 0)
                                    {
                                        //文件已读完
                                        _sendPackage.sender = _user.Username;
                                        _sendPackage.type = im_package.tp_file;
                                        _sendPackage.information = im_package.fl_finish;
                                        _sendPackage.receiver_number = 1;
                                        _sendPackage.receiver[0] = _fileTransport.PeerUsername;
                                        finish = true;
                                    }
                                    else
                                    {
                                        //文件未读完
                                        _sendPackage.sender = _user.Username;
                                        _sendPackage.type = im_package.tp_file;
                                        _sendPackage.information = im_package.fl_content;
                                        _sendPackage.receiver_number = 1;
                                        _sendPackage.receiver[0] = _fileTransport.PeerUsername;
                                    }
                                    //填充完毕，唤醒Send线程
                                    _semSend.Release(1);
                                    if (finish)
                                    {
                                        //状态：已登录
                                        _status = 2;
                                        //主窗体控件
                                        _formMain.Invoke(new MethodInvoker(delegate
                                        {
                                            _formMain.LabelStatus.Text = "已登录：" + _user.Username.ToString();;
                                            _formMain.LabelStatus.ForeColor = Color.Blue;
                                        }));
                                        //发送重置
                                        _fileTransport.SendDisable();
                                        //主窗体控件
                                        _formMain.Invoke(new MethodInvoker(delegate
                                        {
                                            _formMain.Enabled = false;
                                        }));
                                        //提示
                                        MessageBox.Show(_fileTransport.PeerUsername.ToString() + "：文件发送完毕。", "", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                        //主窗体控件
                                        _formMain.Invoke(new MethodInvoker(delegate
                                        {
                                            _formMain.UpdateButtonEnable();
                                            _formMain.Enabled = true;
                                        }));
                                        //对端注销
                                        _fileTransport.PeerUsername = FileTransport._PeerNone;
                                    }
                                    else
                                    {
                                        //主窗体控件
                                        _formMain.Invoke(new MethodInvoker(delegate
                                        {
                                            _formMain.LabelStatus.Text = "文件发送中：" + _fileTransport.FileSizeNow.ToString() + " Bit / " + _fileTransport.FileSizeAll.ToString() + " Bit";
                                        }));
                                        //计时器
                                        _stopwatch.Start();
                                    }
                                    break;
                                case im_package.fl_reject:
                                    //发送重置
                                    _fileTransport.SendDisable();
                                    //主窗体控件
                                    _formMain.Invoke(new MethodInvoker(delegate
                                    {
                                        _formMain.Enabled = false;
                                    }));
                                    //提示
                                    MessageBox.Show(_fileTransport.PeerUsername.ToString() + "：拒绝接收文件。", "", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    //主窗体控件
                                    _formMain.Invoke(new MethodInvoker(delegate
                                    {
                                        _formMain.UpdateButtonEnable();
                                        _formMain.Enabled = true;
                                    }));
                                    //对端注销
                                    _fileTransport.PeerUsername = FileTransport._PeerNone;
                                    break;
                                case im_package.fl_interrupt:
                                    //状态：已登录
                                    _status = 2;
                                    //主窗体控件
                                    _formMain.Invoke(new MethodInvoker(delegate
                                    {
                                        _formMain.LabelStatus.Text = "已登录：" + _user.Username.ToString();;
                                        _formMain.LabelStatus.ForeColor = Color.Blue;
                                    }));
                                    //发送重置
                                    _fileTransport.SendDisable();
                                    //主窗体控件
                                    _formMain.Invoke(new MethodInvoker(delegate
                                    {
                                        _formMain.Enabled = false;
                                    }));
                                    //提示
                                    MessageBox.Show(_fileTransport.PeerUsername.ToString() + "：中止文件传输。", "", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    //主窗体控件
                                    _formMain.Invoke(new MethodInvoker(delegate
                                    {
                                        _formMain.UpdateButtonEnable();
                                        _formMain.Enabled = true;
                                    }));
                                    //对端注销
                                    _fileTransport.PeerUsername = FileTransport._PeerNone;
                                    break;
                                case im_package.fl_content:
                                case im_package.fl_request:
                                case im_package.fl_finish:
                                case im_package.fl_offline:
                                    break;
                                default:
                                    break;
                            }
                        }
                        else
                        {
                            //日志
                            if (_logEnable)
                            {
                                //File线程睡眠，直到当前时刻没有其他线程正在写日志
                                _semLog.WaitOne();
                                //File线程被唤醒，写日志
                                //写日志文件
                                WriteLog("Exception: There is am error in file transmission.");
                                //写日志完毕，唤醒其他等待写日志的线程
                                _semLog.Release(1);
                            }
                            throw new Exception("文件传输错误！");
                        }
                    }
                    else
                    {
                        //文件包来自其他客户端
                        switch (_receivePackageFile.information)
                        {
                            case im_package.fl_content:
                                //文件内容：中断
                                //File线程睡眠，直到能够占用发送包缓冲区
                                _semWrite.WaitOne();
                                //File线程被唤醒，填充发送包缓冲区
                                _sendPackage.sender = _user.Username;
                                _sendPackage.type = im_package.tp_file;
                                _sendPackage.information = im_package.fl_interrupt;
                                _sendPackage.receiver_number = 1;
                                _sendPackage.receiver[0] = _receivePackageFile.sender;
                                _sendPackage.content_lenth = 0;
                                //填充完毕，唤醒Send线程
                                _semSend.Release(1);
                                break;
                            case im_package.fl_request:
                                //文件传输请求：拒绝
                                //File线程睡眠，直到能够占用发送包缓冲区
                                _semWrite.WaitOne();
                                //File线程被唤醒，填充发送包缓冲区
                                _sendPackage.sender = _user.Username;
                                _sendPackage.type = im_package.tp_file;
                                _sendPackage.information = im_package.fl_reject;
                                _sendPackage.receiver_number = 1;
                                _sendPackage.receiver[0] = _receivePackageFile.sender;
                                _sendPackage.content_lenth = 0;
                                //填充完毕，唤醒Send线程
                                _semSend.Release(1);
                                break;
                            case im_package.fl_interrupt:
                                //两个客户端同时点击“中止文件传输”按钮
                                break;
                            case im_package.fl_reply:
                            case im_package.fl_accept:
                            case im_package.fl_reject:
                            case im_package.fl_finish:
                            case im_package.fl_offline:
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
        }

        //数据处理线程：斗地主包（Doudizhu线程）
        public static void ThreadDoudizhu()
        {
            Int32 tempInt32;
            while (true)
            {
                //Doudizhu线程睡眠，直到成功接收类型为tp_doudizhu的数据包
                _semReadDoudizhu.WaitOne();
                //Doudizhu线程被唤醒
                //从斗地主包处理队列中取出一个斗地主包
                _receivePackageDoudizhu = _receivePackageDoudizhuQueue.Dequeue();
                //处理
                if (_receivePackageDoudizhu.sender == 0)
                {
                    //当且仅当包的发送者为服务端，处理包
                    switch (_receivePackageDoudizhu.information)
                    {
                        case im_package.dz_online:
                            if (_clientTable.Count > 0)
                            {
                                //当且仅当处理过tb_all包，处理dz_online包
                                tempInt32 = 0;
                                foreach (ClientItem ci in _clientTable)
                                {
                                    ci.DoudizhuOnline = false;
                                    for (Int32 i = tempInt32; i < _receivePackageDoudizhu.content_lenth; i += 4)
                                    {
                                        if (ci.UsernameInt32 == IPAddress.NetworkToHostOrder(BitConverter.ToInt32(_receivePackageDoudizhu.content, i)))
                                        {
                                            ci.DoudizhuOnline = true;
                                            tempInt32 += 4;
                                            break;
                                        }
                                    }
                                }
                            }
                            //更新客户端列表
                            _formMain.Invoke(new MethodInvoker(delegate
                            {
                                _formMain.UpdateClientTable();
                            }));
                            break;
                        case im_package.dz_invite:
                            //处理斗地主游戏邀请
                            _formMain.Invoke(new MethodInvoker(delegate
                            {
                                _formMain.DoudizhuInvite();
                            }));
                            break;
                        case im_package.dz_accept:
                        case im_package.dz_reject:
                            //服务端异常：不应发送这个包
                            if (_strictDebug)
                            {
                                throw new Exception();
                            }
                            break;
                        default:
                            //服务端异常 / 客户端异常：数据包格式错误
                            if (_strictDebug)
                            {
                                throw new Exception();
                            }
                            break;
                    }
                 }
             }
        }

        //初始化信号量
        public static void InitializeSemaphore()
        {
            _semReadLogin = new Semaphore(0, 1);
            _semReadTable = new Semaphore(0, 1);
            _semReadText = new Semaphore(0, 1);
            _semReadFile = new Semaphore(0, Int32.MaxValue);
            _semReadDoudizhu = new Semaphore(0, Int32.MaxValue);
            _semReceive = new Semaphore(1, 1);
            _semWrite = new Semaphore(1, 1);
            _semSend = new Semaphore(0, 1);
            _semLog = new Semaphore(1, 1);
        }

        //确保完全发送
        static Int32 send_confirm(byte[] buffer, int size, SocketFlags socketFlags)
        {
            int lenth;
            int offset;
            int rtn_val;
            for (lenth = size, offset = 0; lenth > 0; lenth -= rtn_val, offset += rtn_val)
            {
                rtn_val = _socket.Send(buffer, offset, lenth, socketFlags);
                if (rtn_val <= 0)
                {
                    return rtn_val;
                }
            }
            return size;
        }

        //写日志文件：应用程序信息
        public static void WriteLog(String applicationMessage)
        {
            if (_logConsole)
            {
                Console.WriteLine("<Record>");
                Console.WriteLine("\t<Time>");
                Console.WriteLine("\t\t" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                Console.WriteLine("\t</Time>");
                Console.WriteLine("\t<Application>");
                Console.WriteLine("\t\t" + applicationMessage);
                Console.WriteLine("\t</Application>");
                Console.WriteLine("</Record>");
                Console.WriteLine();
            }
            else
            {
                _logFileWriter.WriteLine("<Record>");
                _logFileWriter.WriteLine("\t<Time>");
                _logFileWriter.WriteLine("\t\t" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                _logFileWriter.WriteLine("\t</Time>");
                _logFileWriter.WriteLine("\t<Application>");
                _logFileWriter.WriteLine("\t\t" + applicationMessage);
                _logFileWriter.WriteLine("\t</Application>");
                _logFileWriter.WriteLine("</Record>");
                _logFileWriter.WriteLine();
                _logFileWriter.Flush();
            }
        }

        //写日志文件：数据包
        public static void WriteLog(Boolean receive, Boolean heartbeat, im_package package)
        {
            if (_logConsole)
            {
                Console.WriteLine("<Record>");
                Console.WriteLine("\t<Time>");
                Console.WriteLine("\t\t" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                Console.WriteLine("\t</Time>");
                Console.WriteLine("\t<Communication>");
                Console.WriteLine((receive ? "\t\treceive from " : "\t\tsend to ") + _ipEndPoint.ToString());
                if (heartbeat)
                {
                    Console.WriteLine("\t<Heartbeat>");
                    Console.WriteLine("\t\t0xFFFFFFFF");
                    Console.WriteLine("\t</Heartbeat>");
                }
                else
                {
                    Console.WriteLine("\t<Package>");
                    Console.WriteLine("\t\t<sender> " + package.sender.ToString() + " </sender>");
                    Console.WriteLine("\t\t<type> " + package.type.ToString() + " </type>");
                    Console.WriteLine("\t\t<information> " + package.information.ToString() + " </information>");
                    Console.WriteLine("\t\t<receiver_number> " + package.receiver_number.ToString() + " </receiver_number>");
                    if (package.receiver_number >= 1 && package.receiver_number <= 20)
                    {
                        for (Int32 i = 0; i < package.receiver_number; ++i)
                        {
                            Console.WriteLine("\t\t<receiver[" + i.ToString() + "]> " + package.receiver[i].ToString() + " </receiver[" + i.ToString() + "]>");
                        }
                    }
                    Console.WriteLine("\t\t<content_lenth> " + package.content_lenth.ToString() + " </content_lenth>");
                    if (package.content_lenth != 0)
                    {
                        if (package.type == im_package.tp_table)
                        {
                            //nothing
                        }
                        else if (package.type == im_package.tp_doudizhu && package.information == im_package.dz_online)
                        {
                            //nothing
                        }
                        else if (package.type == im_package.tp_file && package.information == im_package.fl_content)
                        {
                            //nothing
                        }
                        else if (package.type == im_package.tp_file && package.information == im_package.fl_request)
                        {
                            Console.WriteLine("\t\t<content>");
                            Console.WriteLine("\t\t\t" + _encoder.GetString(package.content, 8, package.content_lenth - 8));
                            Console.WriteLine("\t\t</content>");
                        }
                        else
                        {
                            Console.WriteLine("\t\t<content>");
                            Console.WriteLine("\t\t\t" + _encoder.GetString(package.content, 0, package.content_lenth));
                            Console.WriteLine("\t\t</content>");
                        }
                    }
                    Console.WriteLine("\t</Package>");
                }
                Console.WriteLine("</Record>");
                Console.WriteLine();
            }
            else
            {
                _logFileWriter.WriteLine("<Record>");
                _logFileWriter.WriteLine("\t<Time>");
                _logFileWriter.WriteLine("\t\t" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                _logFileWriter.WriteLine("\t</Time>");
                _logFileWriter.WriteLine("\t<Communication>");
                _logFileWriter.WriteLine(receive ? "\t\treceive from " : "\t\tsend to " + _ipEndPoint.ToString());
                _logFileWriter.WriteLine("\t</Communication>");
                if (heartbeat)
                {
                    _logFileWriter.WriteLine("\t<Heartbeat>");
                    _logFileWriter.WriteLine("\t\t0xFFFFFFFF");
                    _logFileWriter.WriteLine("\t</Heartbeat>");
                }
                else
                {
                    _logFileWriter.WriteLine("\t<Package>");
                    _logFileWriter.WriteLine("\t\t<sender> " + package.sender.ToString() + " </sender>");
                    _logFileWriter.WriteLine("\t\t<type> " + package.type.ToString() + " </type>");
                    _logFileWriter.WriteLine("\t\t<information> " + package.information.ToString() + " </information>");
                    _logFileWriter.WriteLine("\t\t<receiver_number> " + package.receiver_number.ToString() + " </receiver_number>");
                    if (package.receiver_number >= 1 && package.receiver_number <= 20)
                    {
                        for (Int32 i = 0; i < package.receiver_number; ++i)
                        {
                            _logFileWriter.WriteLine("\t\t<receiver[" + i.ToString() + "]> " + package.receiver[i].ToString() + " </receiver[" + i.ToString() + "]>");
                        }
                    }
                    _logFileWriter.WriteLine("\t\t<content_lenth> " + package.content_lenth.ToString() + " </content_lenth>");
                    if (package.content_lenth != 0)
                    {
                        if (package.type == im_package.tp_table)
                        {
                            //nothing
                        }
                        else if (package.type == im_package.tp_doudizhu && package.information == im_package.dz_online)
                        {
                            //nothing
                        }
                        else if (package.type == im_package.tp_file && package.information == im_package.fl_content)
                        {
                            //nothing
                        }
                        else if (package.type == im_package.tp_file && package.information == im_package.fl_request)
                        {
                            _logFileWriter.WriteLine("\t\t<content>");
                            _logFileWriter.WriteLine("\t\t\t" + _encoder.GetString(package.content, 8, package.content_lenth - 8));
                            _logFileWriter.WriteLine("\t\t</content>");
                        }
                        else
                        {
                            _logFileWriter.WriteLine("\t\t<content>");
                            _logFileWriter.WriteLine("\t\t\t" + _encoder.GetString(package.content, 0, package.content_lenth));
                            _logFileWriter.WriteLine("\t\t</content>");
                        }
                    }
                    _logFileWriter.WriteLine("\t</Package>");
                }
                _logFileWriter.WriteLine("</Record>");
                _logFileWriter.WriteLine();
                _logFileWriter.Flush();
            }
        }
    }
}
