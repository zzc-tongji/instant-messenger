using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Diagnostics;

namespace IM
{
    public partial class FormMain : Form
    {
        /*      类：字段      */

        static String stringAll = "全体";

        /*      对象：字段      */

        //线程
        Thread _threadReceive;
        Thread _threadSend;
        Thread _threadLogin;
        Thread _threadTable;
        Thread _threadText;
        Thread _threadFile;
        Thread _threadDoudizhu;
        //客户端显示器：正待向客户端显示器中插入的行
        ListViewItem _line;
        //客户端显示器：正待向客户端显示器中插入的格子
        ListViewItem.ListViewSubItem _cell;
        //客户端显示器：在线客户端的显示字体
        Font _fontRegular;
        Font _fontBold;
        //打开文件对话框
        OpenFileDialog _openFileDialog;
        //保存文件对话框
        SaveFileDialog _saveFileDialog;
        //缓存：文本
        String _cacheText;
        //缓存：文本接收者
        public Dictionary<Int32, String> _cacheReceiver;
        //临时变量
        Byte[] _tempByteArray;
        Int64 _tempInt64;       //结合定时器使用，用来判定文件发送端下线

        /*      对象：辅助方法      */

        //禁用窗体的关闭按钮
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams myCp = base.CreateParams;
                myCp.ClassStyle = myCp.ClassStyle | 0x200;
                return myCp;
            }
        }

        //设置StripMenuItems状态【GUI线程】
        public void SetToolStripMenuItemEnable(Boolean b1, Boolean b2, Boolean b3, Boolean b4, Boolean b5)
        {
            LoginToolStripMenuItem.Enabled = b1;
            QuickLoginToolStripMenuItem.Enabled = b2;
            DoudizhuToolStripMenuItem.Enabled = b3;
            LogoutToolStripMenuItem.Enabled = b4;
            ExitToolStripMenuItem.Enabled = b5;
        }

        //更新Button状态【GUI线程 / File线程】
        public void UpdateButtonEnable()
        {
            switch (Program._status)
            {
                case 0: //未连接
                case 1: //已连接
                    buttonSendFile.Enabled = false;
                    buttonStopFileTransmission.Enabled = false;
                    break;
                case 2: //已登陆
                    if (_cacheReceiver.Count == 1)
                    {
                        buttonSendFile.Enabled = true;
                    }
                    else
                    {
                        buttonSendFile.Enabled = false;
                    }
                    buttonStopFileTransmission.Enabled = false;
                    break;
                case 3: //文件传输中
                    buttonSendFile.Enabled = false;
                    buttonStopFileTransmission.Enabled = true;
                    break;
                default:
                    if (Program._strictDebug)
                    {
                        //应用程序状态异常
                        throw new Exception();
                    }
                    break;
            }
        }

        //设置Button状态【GUI线程】
        void setButtonEnable(Boolean b3, Boolean b4, Boolean b5, Boolean b6)
        {
            buttonSend.Enabled = b3;
            buttonSelect.Enabled = b4;
            buttonToAll.Enabled = b5;
            buttonCancel.Enabled = b6;
        }

        //已连接【GUI线程】
        void connected()
        {
            //创建线程
            _threadReceive = new Thread(Program.ThreadReceive);
            _threadSend = new Thread(Program.ThreadSend);
            _threadLogin = new Thread(Program.ThreadLogin);
            _threadTable = new Thread(Program.ThreadTable);
            _threadText = new Thread(Program.ThreadText);
            _threadFile = new Thread(Program.ThreadFile);
            _threadDoudizhu = new Thread(Program.ThreadDoudizhu);
            //初始化信号量
            Program.InitializeSemaphore();
            //启动线程
            _threadSend.Start();
            _threadLogin.Start();
            _threadTable.Start();
            _threadText.Start();
            _threadFile.Start();
            _threadDoudizhu.Start();
            _threadReceive.Start();     //该线程启动之后会立即收到一个登陆包
            //日志
            if (Program._logEnable)
            {
                //GUI线程睡眠，直到当前时刻没有其他线程正在写日志
                Program._semLog.WaitOne();
                //GUI线程被唤醒，写日志
                //写日志文件
                Program.WriteLog("Client connected.");
                //写日志完毕，唤醒其他等待写日志的线程
                Program._semLog.Release(1);
            }
            //日志
            if (Program._logEnable)
            {
                //GUI线程睡眠，直到当前时刻没有其他线程正在写日志
                Program._semLog.WaitOne();
                //GUI线程被唤醒，写日志
                //写日志文件
                Program.WriteLog("All background threads start.");
                //写日志完毕，唤醒其他等待写日志的线程
                Program._semLog.Release(1);
            }
            //状态：已连接
            Program._status = 1;
            labelStatus.Text = "已连接";
            labelStatus.ForeColor = Color.Yellow;
        }

        //登录中【Login线程】
        public void LogingIn()
        {
            //调用线程睡眠，直到能够占用发送包缓冲区
            Program._semWrite.WaitOne();
            //调用线程被唤醒，填充发送包缓冲区
            Program._sendPackage.sender = Program._user.Username;
            Program._sendPackage.type = im_package.tp_login;
            Program._sendPackage.information = im_package.lg_answer;
            Program._sendPackage.receiver_number = 0;
            //Program._sendPackage.receiver
            //字符串编码
            _tempByteArray = Program._encoder.GetBytes(Program._user.Password);
            Program._sendPackage.content_lenth = _tempByteArray.Length;
            if (Program._sendPackage.content_lenth > im_package.size_content_max)
            {
                //日志
                if (Program._logEnable)
                {
                    //Login线程睡眠，直到当前时刻没有其他线程正在写日志
                    Program._semLog.WaitOne();
                    //Login线程被唤醒，写日志
                    //写日志文件
                    Program.WriteLog("Exception: Password is too long.");
                    //写日志完毕，唤醒其他等待写日志的线程
                    Program._semLog.Release(1);
                }
                throw new Exception("密码过长！应用程序中止。");
            }
            _tempByteArray.CopyTo(Program._sendPackage.content, 0);
            //填充完毕，唤醒Send线程
            Program._semSend.Release(1);
            //日志
            if (Program._logEnable)
            {
                //Login线程睡眠，直到当前时刻没有其他线程正在写日志
                Program._semLog.WaitOne();
                //Login线程被唤醒，写日志
                //写日志文件
                Program.WriteLog("Client is Loging in.");
                //写日志完毕，唤醒其他等待写日志的线程
                Program._semLog.Release(1);
            }
        }

        //已登录【Login线程】
        public void LogedIn()
        {
            //主窗体控件
            SetToolStripMenuItemEnable(false, false, true, true, false);
            setButtonEnable(false, true, true, false);
            UpdateButtonEnable();
            groupBox.Enabled = true;
            this.Enabled = true;
            //日志
            if (Program._logEnable)
            {
                //Login线程睡眠，直到当前时刻没有其他线程正在写日志
                Program._semLog.WaitOne();
                //Login线程被唤醒，写日志
                //写日志文件
                Program.WriteLog("Client has Loged in.");
                //写日志完毕，唤醒其他等待写日志的线程
                Program._semLog.Release(1);
            }
            //状态：已登陆
            Program._status = 2;
            labelStatus.Text = "已登录：" + Program._user.Username.ToString();;
            labelStatus.ForeColor = Color.Blue;
        }

        //已断开【Receive线程】
        public void Disconnected()
        {
            //终止线程
            _threadReceive.Abort();
            _threadSend.Abort();
            _threadLogin.Abort();
            _threadTable.Abort();
            _threadText.Abort();
            _threadFile.Abort();
            _threadDoudizhu.Abort();
            //关闭套接字
            Program._socket.Close();
            //初始化文件传输装置
            Program._fileTransport.PeerUsername = FileTransport._PeerNone;
            Program._fileTransport.ReceiveDisable(false);
            Program._fileTransport.SendDisable();
            //主窗体控件
            SetToolStripMenuItemEnable(true, true, false, false, true);
            setButtonEnable(false, true, true, false);
            UpdateButtonEnable();
            richTextBoxReceiveContent.Text = "";
            textBoxSendTo.Text = "";
            _cacheReceiver.Clear();
            listViewClientTable.Items.Clear();
            listViewClientTable.Enabled = true;
            groupBox.Enabled = false;
            this.Enabled = true;
            //日志
            if (Program._logEnable)
            {
                //Receive线程睡眠，直到当前时刻没有其他线程正在写日志
                Program._semLog.WaitOne();
                //Receive线程被唤醒，写日志
                //写日志文件
                Program.WriteLog("All background threads abort.");
                //写日志完毕，唤醒其他等待写日志的线程
                Program._semLog.Release(1);
            }
            //日志
            if (Program._logEnable)
            {
                //Receive线程睡眠，直到当前时刻没有其他线程正在写日志
                Program._semLog.WaitOne();
                //Receive线程被唤醒，写日志
                //写日志文件
                Program.WriteLog("Client disconnected.");
                //写日志完毕，唤醒其他等待写日志的线程
                Program._semLog.Release(1);
            }
            //状态：已断开
            Program._status = 0;
            labelStatus.Text = "未连接";
            labelStatus.ForeColor = Color.Red;
        }

        //已断开（异常）【Receive线程 / Send线程】
        public void DisconnectedAbnormally()
        {
            //终止线程
            _threadReceive.Abort();
            _threadSend.Abort();
            _threadLogin.Abort();
            _threadTable.Abort();
            _threadText.Abort();
            _threadFile.Abort();
            //关闭套接字
            Program._socket.Close();
            //初始化文件传输装置
            Program._fileTransport.PeerUsername = FileTransport._PeerNone;
            Program._fileTransport.ReceiveDisable(false);
            Program._fileTransport.SendDisable();
            //主窗体控件
            SetToolStripMenuItemEnable(true, true, false, false, true);
            setButtonEnable(false, true, true, false);
            UpdateButtonEnable();
            richTextBoxReceiveContent.Text = "";
            textBoxSendTo.Text = "";
            _cacheReceiver.Clear();
            listViewClientTable.Items.Clear();
            listViewClientTable.Enabled = true;
            groupBox.Enabled = false;
            this.Enabled = true;
            //日志
            if (Program._logEnable)
            {
                //调用线程（Receive线程 / Send线程）睡眠，直到当前时刻没有其他线程正在写日志
                Program._semLog.WaitOne();
                //调用线程（Receive线程 / Send线程）被唤醒，写日志
                //写日志文件
                Program.WriteLog("All background threads abort.");
                //写日志完毕，唤醒其他等待写日志的线程
                Program._semLog.Release(1);
            }
            //日志
            if (Program._logEnable)
            {
                //调用线程（Receive线程 / Send线程）睡眠，直到当前时刻没有其他线程正在写日志
                Program._semLog.WaitOne();
                //调用线程（Receive线程 / Send线程）被唤醒，写日志
                //写日志文件
                Program.WriteLog("Client disconnected abnormally.");
                //写日志完毕，唤醒其他等待写日志的线程
                Program._semLog.Release(1);
            }
            //状态：已断开
            Program._status = 0;
            labelStatus.Text = "未连接";
            labelStatus.ForeColor = Color.Red;
        }

        //更新客户端显示器【Table线程 / Doudizhu线程】
        public void UpdateClientTable()
        {
            if (Program._receivePackage.type == im_package.tp_table && Program._receivePackage.information == im_package.tb_all)
            {
                //收到全体客户端列表
                listViewClientTable.Items.Clear();
                //客户端显示器：添加行
                foreach (ClientItem ci in Program._clientTable)
                {
                    //line
                    _line = new ListViewItem();
                    _line.UseItemStyleForSubItems = false;
                    //cell 1
                    _cell = new ListViewItem.ListViewSubItem();
                    _cell.Text = ci.UsernameString;
                    _line.SubItems.Add(_cell);
                    //cell 2
                    _cell = new ListViewItem.ListViewSubItem();
                    _cell.Text = "";
                    _line.SubItems.Add(_cell);
                    //cell 3
                    _cell = new ListViewItem.ListViewSubItem();
                    _cell.Text = "";
                    _line.SubItems.Add(_cell);
                    //line
                    listViewClientTable.Items.Add(_line);
                }
                //调用线程睡眠，直到能够占用发送包缓冲区
                Program._semWrite.WaitOne();
                //调用线程被唤醒，填充发送包缓冲区
                Program._sendPackage.sender = Program._user.Username;
                Program._sendPackage.type = im_package.tp_table;
                Program._sendPackage.information = im_package.tb_online;
                Program._sendPackage.receiver_number = 0;
                //Program._sendPackage.receiver
                Program._sendPackage.content_lenth = 0;
                //填充完毕，唤醒Send线程
                Program._semSend.Release(1);
                //日志
                if (Program._logEnable)
                {
                    //GUI线程睡眠，直到当前时刻没有其他线程正在写日志
                    Program._semLog.WaitOne();
                    //GUI线程被唤醒，写日志
                    //写日志文件
                    Program.WriteLog("Client created client table.");
                    //写日志完毕，唤醒其他等待写日志的线程
                    Program._semLog.Release(1);
                }
            }
            else
            {
                //收到IM在线客户端列表或斗地主在线客户端列表
                for (int i = 0; i < Program._clientTable.Count; i++)
                {
                    listViewClientTable.Items[i].SubItems[1].ForeColor = Color.Black;
                    listViewClientTable.Items[i].SubItems[1].Font = _fontRegular;
                    listViewClientTable.Items[i].SubItems[2].Text = "";
                    listViewClientTable.Items[i].SubItems[3].Text = "";
                    if (Program._clientTable[i].Online)
                    {
                        if (Program._clientTable[i].UsernameInt32 == Program._user.Username)
                        {
                            listViewClientTable.Items[i].SubItems[1].ForeColor = Color.Blue;
                        }
                        else
                        {
                            listViewClientTable.Items[i].SubItems[1].ForeColor = Color.Green;
                        }
                        listViewClientTable.Items[i].SubItems[1].Font = _fontBold;
                        listViewClientTable.Items[i].SubItems[2].Text = "在线";
                    }
                    if (Program._clientTable[i].DoudizhuOnline)
                    {
                        listViewClientTable.Items[i].SubItems[3].Text = "在线";
                    }
                }
                //日志
                if (Program._logEnable)
                {
                    //调用线程（Table线程 / Doudizhu线程）睡眠，直到当前时刻没有其他线程正在写日志
                    Program._semLog.WaitOne();
                    //调用线程（Table线程 / Doudizhu线程）被唤醒，写日志
                    //写日志文件
                    Program.WriteLog("Client refreshed client table.");
                    //写日志完毕，唤醒其他等待写日志的线程
                    Program._semLog.Release(1);
                }
            }
        }

        //更新接收框【Text线程】
        public void UpdateReceiveContent()
        {
            if (Program._receivePackage.information == im_package.tx_content)
            {
                //主窗体控件：文本框
                richTextBoxReceiveContent.Text += "<接收>\r\n";
                richTextBoxReceiveContent.Text += "\t<发送者>\r\n";
                if (Program._receivePackage.sender == Program._user.Username)
                {
                    richTextBoxReceiveContent.Text += "\t\t" + "我" + "\r\n";
                }
                else
                {
                    richTextBoxReceiveContent.Text += "\t\t" + Program._receivePackage.sender.ToString() + "\r\n";
                }
                richTextBoxReceiveContent.Text += "\t</发送者>\r\n";
                richTextBoxReceiveContent.Text += "\t<接收者>\r\n";
                if (Program._receivePackage.receiver_number == -1)
                {
                    richTextBoxReceiveContent.Text += "\t\t" + "全体" + "\r\n";
                }
                else
                {
                    for (int i = 0; i < Program._receivePackage.receiver_number; ++i)
                    {
                        if (Program._receivePackage.receiver[i] == Program._user.Username)
                        {
                            richTextBoxReceiveContent.Text += "\t\t" + "我" + "\r\n";
                        }
                        else
                        {
                            richTextBoxReceiveContent.Text += "\t\t" + Program._receivePackage.receiver[i].ToString() + "\r\n";
                        }
                    }
                }
                richTextBoxReceiveContent.Text += "\t</接收者>\r\n";
                richTextBoxReceiveContent.Text += "\t<内容>\r\n";
                //字符串解码
                richTextBoxReceiveContent.Text += "\t\t" + Program._encoder.GetString(Program._receivePackage.content, 0, Program._receivePackage.content_lenth) + "\r\n";
                richTextBoxReceiveContent.Text += "\t</内容>\r\n";
                richTextBoxReceiveContent.Text += "</接收>\r\n";
                richTextBoxReceiveContent.Text += "\r\n";
            }
            else //Program._receivePackage.information == IM_package.tx_reply
            {
                //主窗体控件：文本框
                richTextBoxReceiveContent.Text += "<发送>\r\n";
                richTextBoxReceiveContent.Text += "\t<发送者>\r\n";
                richTextBoxReceiveContent.Text += "\t\t" + "我" + "\r\n";
                richTextBoxReceiveContent.Text += "\t</发送者>\r\n";
                richTextBoxReceiveContent.Text += "\t<接收者>\r\n";
                if (textBoxSendTo.Text == "全体")
                {
                    richTextBoxReceiveContent.Text += "\t\t" + "全体" + "\r\n";
                }
                else
                {
                    foreach (Int32 i in _cacheReceiver.Keys)
                    {
                        if (i == Program._user.Username)
                        {
                            richTextBoxReceiveContent.Text += "\t\t" + "我" + "\r\n";
                        }
                        else
                        {
                            richTextBoxReceiveContent.Text += "\t\t" + _cacheReceiver[i] + "\r\n";
                        }
                    }
                }
                richTextBoxReceiveContent.Text += "\t</接收者>\r\n";
                richTextBoxReceiveContent.Text += "\t<内容>\r\n";
                richTextBoxReceiveContent.Text += "\t\t" + _cacheText + "\r\n";
                richTextBoxReceiveContent.Text += "\t</内容>\r\n";
                richTextBoxReceiveContent.Text += "</发送>\r\n";
                richTextBoxReceiveContent.Text += "\r\n";
                //主窗体控件：按钮
                buttonSend.Enabled = false;
                buttonCancel.Enabled = true;
            }
            //将滚动条移动到底部
            richTextBoxReceiveContent.SelectionStart = richTextBoxReceiveContent.Text.Length;
            richTextBoxReceiveContent.ScrollToCaret();
            //日志
            if (Program._logEnable)
            {
                //Text线程睡眠，直到当前时刻没有其他线程正在写日志
                Program._semLog.WaitOne();
                //Text线程被唤醒，写日志
                //写日志文件
                Program.WriteLog("Client received a text.");
                //写日志完毕，唤醒其他等待写日志的线程
                Program._semLog.Release(1);
            }
        }

        //处理文件传输请求【File线程】
        public void DealWithFileTransmissionRequest()
        {
            _saveFileDialog.FileName = Program._encoder.GetString(Program._receivePackageFile.content, 8, Program._receivePackageFile.content_lenth - 8);
            if (MessageBox.Show(Program._receivePackageFile.sender.ToString() + "：\r\n\r\n" + _saveFileDialog.FileName + "\r\n（" + BitConverter.ToInt64(Program._receivePackageFile.content, 0).ToString() + " Bit）\r\n\r\n是否接收？", "", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                if (_saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    //同意接收
                    //清零
                    _tempInt64 = 0;
                    //状态：文件传输中
                    Program._status = 3;
                    labelStatus.Text = "文件接收中";
                    labelStatus.ForeColor = Color.Blue;
                    //主窗体控件
                    UpdateButtonEnable();
                    //对端注册
                    Program._fileTransport.PeerUsername = Program._receivePackageFile.sender;
                    //接收准备
                    if (Program._fileTransport.ReceiveEnable(_saveFileDialog.FileName))
                    {
                        Program._fileTransport.FileSizeAll = BitConverter.ToInt64(Program._receivePackageFile.content, 0);
                        //File线程睡眠，直到能够占用发送包缓冲区
                        Program._semWrite.WaitOne();
                        //File线程被唤醒，填充发送包缓冲区
                        Program._sendPackage.sender = Program._user.Username;
                        Program._sendPackage.type = im_package.tp_file;
                        Program._sendPackage.information = im_package.fl_accept;
                        Program._sendPackage.receiver_number = 1;
                        Program._sendPackage.receiver[0] = Program._fileTransport.PeerUsername;
                        Program._sendPackage.content_lenth = 0;
                        //填充完毕，唤醒Send线程
                        Program._semSend.Release(1);
                    }
                    else
                    {
                        //拒绝接收
                        //File线程睡眠，直到能够占用发送包缓冲区
                        Program._semWrite.WaitOne();
                        //File线程被唤醒，填充发送包缓冲区
                        Program._sendPackage.sender = Program._user.Username;
                        Program._sendPackage.type = im_package.tp_file;
                        Program._sendPackage.information = im_package.fl_reject;
                        Program._sendPackage.receiver_number = 1;
                        Program._sendPackage.receiver[0] = Program._receivePackageFile.sender;
                        Program._sendPackage.content_lenth = 0;
                        //填充完毕，唤醒Send线程
                        Program._semSend.Release(1);
                        //主窗体控件
                        UpdateButtonEnable();
                        //提示
                        MessageBox.Show("无法写入目标文件！", "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                else
                {
                    //拒绝接收
                    //File线程睡眠，直到能够占用发送包缓冲区
                    Program._semWrite.WaitOne();
                    //File线程被唤醒，填充发送包缓冲区
                    Program._sendPackage.sender = Program._user.Username;
                    Program._sendPackage.type = im_package.tp_file;
                    Program._sendPackage.information = im_package.fl_reject;
                    Program._sendPackage.receiver_number = 1;
                    Program._sendPackage.receiver[0] = Program._receivePackageFile.sender;
                    Program._sendPackage.content_lenth = 0;
                    //填充完毕，唤醒Send线程
                    Program._semSend.Release(1);
                    //主窗体控件
                    UpdateButtonEnable();
                }
            }
            else
            {
                //拒绝接收
                //File线程睡眠，直到能够占用发送包缓冲区
                Program._semWrite.WaitOne();
                //File线程被唤醒，填充发送包缓冲区
                Program._sendPackage.sender = Program._user.Username;
                Program._sendPackage.type = im_package.tp_file;
                Program._sendPackage.information = im_package.fl_reject;
                Program._sendPackage.receiver_number = 1;
                Program._sendPackage.receiver[0] = Program._receivePackageFile.sender;
                Program._sendPackage.content_lenth = 0;
                //填充完毕，唤醒Send线程
                Program._semSend.Release(1);
                //主窗体控件
                UpdateButtonEnable();
            }
        }

        //处理斗地主游戏邀请【Doudizhu线程】
        public void DoudizhuInvite()
        {
            if (MessageBox.Show(Program._receivePackageDoudizhu.receiver[17].ToString() + "：邀请您参加“斗地主”游戏。\r\n\r\n是否参加？", "", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                //启动斗地主应用程序（自动登录）
                try
                {
                    Process.Start(System.Environment.CurrentDirectory + "\\" + Program.doudizhuExe, Program._user.Username.ToString());
                }
                catch (Exception ex)
                {
                    this.Enabled = false;
                    MessageBox.Show(ex.ToString(), "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    this.Enabled = true;
                }
                //日志
                if (Program._logEnable)
                {
                    //Doudizhu线程睡眠，直到当前时刻没有其他线程正在写日志
                    Program._semLog.WaitOne();
                    //Doudizhu线程被唤醒，写日志
                    //写日志文件
                    Program.WriteLog("The application of \"" + Program.doudizhuExe + "\" has been launched.");
                    //写日志完毕，唤醒其他等待写日志的线程
                    Program._semLog.Release(1);
                }
                //调用线程睡眠，直到能够占用发送包缓冲区
                Program._semWrite.WaitOne();
                //调用线程被唤醒，填充发送包缓冲区
                Program._sendPackage.sender = Program._user.Username;
                Program._sendPackage.type = im_package.tp_doudizhu;
                Program._sendPackage.information = im_package.dz_accept;
                Program._sendPackage.receiver_number = 0;
                Program._sendPackage.receiver[17] = Program._user.Username;
                Program._sendPackage.receiver[18] = Program._receivePackageDoudizhu.receiver[17];
                Program._sendPackage.content_lenth = 0;
                //填充完毕，唤醒Send线程
                Program._semSend.Release(1);
                //日志
                if (Program._logEnable)
                {
                    //Doudizhu线程睡眠，直到当前时刻没有其他线程正在写日志
                    Program._semLog.WaitOne();
                    //Doudizhu线程被唤醒，写日志
                    //写日志文件
                    Program.WriteLog("Client accepted the invitation of playing doudizhu.");
                    //写日志完毕，唤醒其他等待写日志的线程
                    Program._semLog.Release(1);
                }
            }
            else
            {
                //调用线程睡眠，直到能够占用发送包缓冲区
                Program._semWrite.WaitOne();
                //调用线程被唤醒，填充发送包缓冲区
                Program._sendPackage.sender = Program._user.Username;
                Program._sendPackage.type = im_package.tp_doudizhu;
                Program._sendPackage.information = im_package.dz_reject;
                Program._sendPackage.receiver_number = 0;
                Program._sendPackage.receiver[17] = Program._user.Username;
                Program._sendPackage.receiver[18] = Program._receivePackageDoudizhu.receiver[17];
                Program._sendPackage.content_lenth = 0;
                //填充完毕，唤醒Send线程
                Program._semSend.Release(1);
                //日志
                if (Program._logEnable)
                {
                    //Doudizhu线程睡眠，直到当前时刻没有其他线程正在写日志
                    Program._semLog.WaitOne();
                    //Doudizhu线程被唤醒，写日志
                    //写日志文件
                    Program.WriteLog("Client rejected the invitation of playing doudizhu.");
                    //写日志完毕，唤醒其他等待写日志的线程
                    Program._semLog.Release(1);
                }
            }
        }

        /*      对象：构造方法和事件      */
        public FormMain()
        {
            InitializeComponent();
            _threadReceive = null;
            _threadSend = null;
            _threadLogin = null;
            _threadTable = null;
            _threadText = null;
            _threadFile = null;
            _threadDoudizhu = null;
            _line = null;
            _cell = null;
            _fontRegular = new Font("新宋体", 10.5f, FontStyle.Regular);
            _fontBold = new Font("新宋体", 10.5f, FontStyle.Bold);
            _saveFileDialog = new SaveFileDialog();
            _saveFileDialog.Title = "选择文件";
            _saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            _saveFileDialog.Filter = "所有文件|*";
            _openFileDialog = new OpenFileDialog();
            _openFileDialog.Title = "选择文件";
            _openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            _openFileDialog.Filter = "所有文件|*";
            _openFileDialog.Multiselect = false;
            _cacheText = null;
            _cacheReceiver = new Dictionary<Int32, String>();
            _tempByteArray = null;
            _tempInt64 = 0;
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            IPAddress address;
            Int32 port;
            Int32 username;
            //主窗体控件
            SetToolStripMenuItemEnable(true, false, false, false, true);
            groupBox.Enabled = false;
            //接收框
            richTextBoxReceiveContent.Text = "";
            //发送目标框
            textBoxSendTo.Text = "";
            //发送框：获得焦点
            textBoxSendContent.Select();
            //客户端显示器：显示网格线
            listViewClientTable.GridLines = false;
            //客户端显示器：可以选中整行
            listViewClientTable.FullRowSelect = false;
            //客户端显示器：更改显示方式
            listViewClientTable.View = View.Details;
            //客户端显示器：自动显示滚动条
            listViewClientTable.Scrollable = true;
            //客户端显示器：可以选中整行
            listViewClientTable.FullRowSelect = true;
            //客户端显示器：可以按住“Ctrl”选中多行
            listViewClientTable.MultiSelect = true;
            //客户端显示器：清空内容
            listViewClientTable.Clear();
            //客户端显示器：添加列
            listViewClientTable.Columns.Add("", 1);
            listViewClientTable.Columns.Add("用户名", 75, HorizontalAlignment.Center);
            listViewClientTable.Columns.Add("聊天", 75, HorizontalAlignment.Center);
            listViewClientTable.Columns.Add("斗地主", 75, HorizontalAlignment.Center);
            //日志
            if (Program._logEnable)
            {
                //GUI线程睡眠，直到当前时刻没有其他线程正在写日志
                Program._semLog.WaitOne();
                //GUI线程被唤醒，写日志
                //写日志文件
                Program.WriteLog("Main thread (GUI) starts.");
                //写日志完毕，唤醒其他等待写日志的线程
                Program._semLog.Release(1);
            }
            if (Program._parameter.Length == 4)
            {
                if
                    (
                        IPAddress.TryParse(Program._parameter[0], out address) &&
                        Int32.TryParse(Program._parameter[1], out port) &&
                        Int32.TryParse(Program._parameter[2], out username)
                    )
                {

                    Program._ipEndPoint.Address = address;
                    Program._ipEndPoint.Port = port;
                    Program._user.Username = username;
                    Program._user.Password = Program._parameter[3];
                    SetToolStripMenuItemEnable(true, true, false, false, true);
                    QuickLoginToolStripMenuItem_Click(new object(), new EventArgs());
                }
            }
        }

        private void LoginToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Enabled = false;
            FormLogin formLogin = new FormLogin(this);
            formLogin.Show();
        }

        public void QuickLoginToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //主窗体控件：锁定主窗体
            this.Enabled = false;
            //套接字
            Program._socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                Program._socket.Connect(Program._ipEndPoint);
            }
            catch (SocketException ex)
            {
                //日志
                if (Program._logEnable)
                {
                    //GUI线程睡眠，直到当前时刻没有其他线程正在写日志
                    Program._semLog.WaitOne();
                    //GUI线程被唤醒，写日志
                    //写日志文件
                    Program.WriteLog(ex.ToString());
                    //写日志完毕，唤醒其他等待写日志的线程
                    Program._semLog.Release(1);
                }
                //提示
                MessageBox.Show(ex.ToString(), "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                //主窗体控件
                this.Enabled = true;
                return;
            }
            //已连接
            connected();
        }

        private void DoudizhuToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //启动斗地主应用程序（自动登录）
            try
            {
                Process.Start(System.Environment.CurrentDirectory + "\\" + Program.doudizhuExe, Program._user.Username.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void LogoutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //主窗体控件：锁定主窗体
            this.Enabled = false;
            //GUI线程睡眠，直到能够占用发送包缓冲区
            Program._semWrite.WaitOne();
            //GUI线程被唤醒，填充发送包缓冲区
            Program._sendPackage.sender = Program._user.Username;
            Program._sendPackage.type = im_package.tp_login;
            Program._sendPackage.information = im_package.lg_logout_request;
            Program._sendPackage.receiver_number = 0;
            //Program._sendPackage.receiver
            Program._sendPackage.content_lenth = 0;
            //填充完毕，唤醒Send线程
            Program._semSend.Release(1);
        }

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Program._logEnable)
            {
                //GUI线程睡眠，直到当前时刻没有其他线程正在写日志
                Program._semLog.WaitOne();
                //GUI线程被唤醒，写日志
                //写日志文件
                Program.WriteLog("Main thread (GUI) aborts.");
                //写日志完毕，唤醒其他等待写日志的线程
                Program._semLog.Release(1);
            }
            //关闭窗口并退出程序
            this.Close();
            Environment.Exit(0);
        }

        private void buttonSendFile_Click(object sender, EventArgs e)
        {
            //对端注册
            Program._fileTransport.PeerUsername = _cacheReceiver.Keys.First<Int32>();
            if (Program._user.Username == Program._fileTransport.PeerUsername)
            {
                MessageBox.Show("不能给自己发送文件！", "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                //对端注销
                Program._fileTransport.PeerUsername = -1;
            }
            else
            {
                _openFileDialog.FileName = "";
                if (_openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    //发送准备
                    if (Program._fileTransport.SendEnable(_openFileDialog.FileName))
                    {
                        //清零
                        _tempInt64 = 0;
                        //主窗体控件
                        buttonSendFile.Enabled = false;
                        buttonStopFileTransmission.Enabled = false;
                        if (buttonCancel.Enabled)
                        {
                            textBoxSendContent.Select();
                        }
                        else
                        {
                            listViewClientTable.Select();
                        }
                        //GUI线程睡眠，直到能够占用发送包缓冲区
                        Program._semWrite.WaitOne();
                        //GUI线程被唤醒，填充发送包缓冲区
                        Program._sendPackage.sender = Program._user.Username;
                        Program._sendPackage.type = im_package.tp_file;
                        Program._sendPackage.information = im_package.fl_request;
                        Program._sendPackage.receiver_number = 1;
                        Program._sendPackage.receiver[0] = Program._fileTransport.PeerUsername;
                        //文件大小和文件名
                        BitConverter.GetBytes(Program._fileTransport._FileInfo.Length).CopyTo(Program._sendPackage.content, 0);
                        Program._sendPackage.content_lenth = 8 + Program._encoder.GetBytes(Program._fileTransport._FileInfo.Name, 0, Program._fileTransport._FileInfo.Name.Length, Program._sendPackage.content, 8);
                        //填充完毕，唤醒Send线程
                        Program._semSend.Release(1);
                    }
                    else
                    {
                        //主窗体控件
                        UpdateButtonEnable();
                        if (buttonCancel.Enabled)
                        {
                            textBoxSendContent.Select();
                        }
                        else
                        {
                            listViewClientTable.Select();
                        }
                        //发送重置
                        Program._fileTransport.SendDisable();
                        //对端注销
                        Program._fileTransport.PeerUsername = FileTransport._PeerNone;
                        //提示
                        MessageBox.Show("无法打开文件！", "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                else
                {
                    //对端注销
                    Program._fileTransport.PeerUsername = -1;
                }
            }
        }

        private void buttonStopFileTransmission_Click(object sender, EventArgs e)
        {
            //GUI线程睡眠，直到能够占用发送包缓冲区
            Program._semWrite.WaitOne();
            //GUI线程被唤醒，填充发送包缓冲区
            Program._sendPackage.sender = Program._user.Username;
            Program._sendPackage.type = im_package.tp_file;
            Program._sendPackage.information = im_package.fl_interrupt;
            Program._sendPackage.receiver_number = 1;
            Program._sendPackage.receiver[0] = Program._fileTransport.PeerUsername;
            Program._sendPackage.content_lenth = 0;
            //填充完毕，唤醒Send线程
            Program._semSend.Release(1);
            //状态：已登录
            Program._status = 2;
            labelStatus.Text = "已登录：" + Program._user.Username.ToString();;
            labelStatus.ForeColor = Color.Blue;
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
            else
            {
                if (Program._strictDebug)
                {
                    //状态错误
                    throw new Exception();
                }
            }
            //对端注销
            Program._fileTransport.PeerUsername = FileTransport._PeerNone;
            //主窗体控件
            UpdateButtonEnable();
            if (buttonCancel.Enabled)
            {
                textBoxSendContent.Select();
            }
            else
            {
                listViewClientTable.Select();
            }
        }

        private void buttonSend_Click(object sender, EventArgs e)
        {
            _cacheText = textBoxSendContent.Text;
            //主窗体控件
            textBoxSendContent.Text = "";
            buttonSend.Enabled = false;
            buttonCancel.Enabled = false;
            textBoxSendContent.Select();
            //GUI线程睡眠，直到能够占用发送包缓冲区
            Program._semWrite.WaitOne();
            //GUI线程被唤醒，填充发送包缓冲区
            Program._sendPackage.sender = Program._user.Username;
            Program._sendPackage.type = im_package.tp_text;
            Program._sendPackage.information = im_package.tx_content;
            if (textBoxSendTo.Text == stringAll)
            {
                Program._sendPackage.receiver_number = -1;
            }
            else
            {
                Program._sendPackage.receiver_number = _cacheReceiver.Count;
                if (true)
                {
                    Int32 i = 0;
                    foreach (Int32 j in _cacheReceiver.Keys)
                    {
                        Program._sendPackage.receiver[i] = j;
                        ++i;
                    }
                }
            }
            //字符串编码
            _tempByteArray = Program._encoder.GetBytes(_cacheText);
            Program._sendPackage.content_lenth = _tempByteArray.Length;
            if (Program._sendPackage.content_lenth > im_package.size_content_max)
            {
                MessageBox.Show("文本过长！请重新输入。", "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                //解除对发送包缓冲区的占用
                Program._semWrite.Release(1);
                //主窗体控件
                textBoxSendContent.Text = "";
                buttonSend.Enabled = false;
                buttonCancel.Enabled = true;
            }
            else
            {
                _tempByteArray.CopyTo(Program._sendPackage.content, 0);
                //填充完毕，唤醒Send线程
                Program._semSend.Release(1);
                //日志
                if (Program._logEnable)
                {
                    //GUI线程睡眠，直到当前时刻没有其他线程正在写日志
                    Program._semLog.WaitOne();
                    //GUI线程被唤醒，写日志
                    //写日志文件
                    Program.WriteLog("Client sent a text.");
                    //写日志完毕，唤醒其他等待写日志的线程
                    Program._semLog.Release(1);
                }
            }
        }

        private void buttonSelect_Click(object sender, EventArgs e)
        {
            if (listViewClientTable.SelectedItems.Count < 1)
            {
                MessageBox.Show("未选取！", "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                listViewClientTable.Select();
            }
            else if (listViewClientTable.SelectedItems.Count > 20)
            {
                MessageBox.Show("选取超过20项！", "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                listViewClientTable.Select();
            }
            else
            {
                for (Int32 i = 0; i < listViewClientTable.SelectedItems.Count; ++i)
                {
                    _cacheReceiver.Add(Convert.ToInt32(listViewClientTable.SelectedItems[i].SubItems[1].Text), listViewClientTable.SelectedItems[i].SubItems[1].Text);
                }
                //主窗体控件
                if (textBoxSendContent.Text.Length == 0)
                {
                    setButtonEnable(false, false, false, true);
                }
                else
                {
                    setButtonEnable(true, false, false, true);
                }
                UpdateButtonEnable();
                listViewClientTable.Enabled = false;
                foreach (Int32 i in _cacheReceiver.Keys)
                {
                    textBoxSendTo.Text += _cacheReceiver[i] + " ";
                }
                textBoxSendContent.Select();
            }
        }

        private void buttonToAll_Click(object sender, EventArgs e)
        {
            //主窗体控件
            setButtonEnable(true, false, false, true);
            UpdateButtonEnable();
            listViewClientTable.Enabled = false;
            _cacheReceiver.Clear();
            textBoxSendTo.Text = stringAll;
            textBoxSendContent.Select();
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            //主窗体控件
            setButtonEnable(false, true, true, false);
            listViewClientTable.Enabled = true;
            _cacheReceiver.Clear();
            UpdateButtonEnable();
            textBoxSendTo.Text = "";
            listViewClientTable.Select();
        }

        private void textBoxSendContent_TextChanged(object sender, EventArgs e)
        {
            if (textBoxSendContent.TextLength == 0)
            {
                buttonSend.Enabled = false;
            }
            else
            {
                if (buttonCancel.Enabled)
                {
                    buttonSend.Enabled = true;
                }
            }
        }

        private void textBoxSendContent_KeyDown(object sender, KeyEventArgs e)
        {
            //使用“Ctrl+Enter”组合键发送消息
            if (e.Modifiers.CompareTo(Keys.Control) == 0 && e.KeyCode == Keys.Enter)
            {
                if (buttonSend.Enabled == true)
                {
                    buttonSend_Click(sender, e);
                }
            }
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            if (Program._status == 3 && Program._fileTransport.FileSizeNow > 0)
            {
                if (Program._fileTransport.FileSizeNow == _tempInt64)
                {
                    //文件大小在5秒内没有发生改变
                    buttonStopFileTransmission_Click(sender, e);
                    MessageBox.Show("对方已下线。", "", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    _tempInt64 = Program._fileTransport.FileSizeNow;
                }
            }
        }
    }
}
