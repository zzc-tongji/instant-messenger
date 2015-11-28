using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;

namespace IM
{
    public partial class FormLogin : Form
    {
        /*      对象：字段      */

        FormMain _formMain;
        Boolean[] _vaild;
        IPAddress _ipAddress;
        Int32 _port;
        Int32 _username;

        /*      对象：构造方法和事件      */

        public FormLogin(FormMain formMain)
        {
            InitializeComponent();
            _formMain = formMain;
            _vaild = new Boolean[3] { false, false, false };
        }

        private void FormLogin_Load(object sender, EventArgs e)
        {

        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            if (_vaild[0] && _vaild[1] && _vaild[2])
            {
                Program._ipEndPoint.Address = _ipAddress;
                Program._ipEndPoint.Port = _port;
                Program._user.Username = _username;
                Program._user.Password = textBoxPassword.Text;
                _formMain.SetToolStripMenuItemEnable(true, true, false, false, true);
                _formMain.Enabled = true;
                _formMain.QuickLoginToolStripMenuItem_Click(sender, e);
                this.Close();
            }
            else
            {
                MessageBox.Show((_vaild[0] ? "" : "IPv4地址非法！\r\n") + (_vaild[1] ? "" : "端口号非法！\r\n") + (_vaild[2] ? "" : "用户名非法！\r\n"), "",MessageBoxButtons.OK,MessageBoxIcon.Warning);
            }
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            _formMain.Enabled = true;
            this.Close();
        }

        private void textBoxIPv4_TextChanged(object sender, EventArgs e)
        {
            if (IPAddress.TryParse(textBoxIPv4.Text, out _ipAddress))
            {
                _vaild[0] = true;
            }
            else
            {
                _vaild[0] = false;
            }
        }

        private void textBoxPort_TextChanged(object sender, EventArgs e)
        {
            if (Int32.TryParse(textBoxPort.Text, out _port))
            {
                _vaild[1] = true;
            }
            else
            {
                _vaild[1] = false;
            }
        }

        private void textBoxUsername_TextChanged(object sender, EventArgs e)
        {
            if (Int32.TryParse(textBoxUsername.Text, out _username))
            {
                _vaild[2] = true;
            }
            else
            {
                _vaild[2] = false;
            }
        }
    }
}
