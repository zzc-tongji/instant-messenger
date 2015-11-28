namespace IM
{
    partial class FormMain
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.richTextBoxReceiveContent = new System.Windows.Forms.RichTextBox();
            this.menuStrip = new System.Windows.Forms.MenuStrip();
            this.LoginToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.QuickLoginToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.DoudizhuToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.LogoutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ExitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.groupBox = new System.Windows.Forms.GroupBox();
            this.textBoxSendtolabel = new System.Windows.Forms.TextBox();
            this.textBoxSendTo = new System.Windows.Forms.TextBox();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.buttonToAll = new System.Windows.Forms.Button();
            this.buttonSelect = new System.Windows.Forms.Button();
            this.buttonSend = new System.Windows.Forms.Button();
            this.buttonStopFileTransmission = new System.Windows.Forms.Button();
            this.buttonSendFile = new System.Windows.Forms.Button();
            this.listViewClientTable = new System.Windows.Forms.ListView();
            this.textBoxSendContent = new System.Windows.Forms.TextBox();
            this.labelStatus = new System.Windows.Forms.Label();
            this.timer = new System.Windows.Forms.Timer(this.components);
            this.menuStrip.SuspendLayout();
            this.groupBox.SuspendLayout();
            this.SuspendLayout();
            //
            // richTextBoxReceiveContent
            //
            this.richTextBoxReceiveContent.Font = new System.Drawing.Font("新宋体", 10.5F);
            this.richTextBoxReceiveContent.Location = new System.Drawing.Point(7, 9);
            this.richTextBoxReceiveContent.Margin = new System.Windows.Forms.Padding(4);
            this.richTextBoxReceiveContent.Name = "richTextBoxReceiveContent";
            this.richTextBoxReceiveContent.ReadOnly = true;
            this.richTextBoxReceiveContent.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
            this.richTextBoxReceiveContent.Size = new System.Drawing.Size(610, 440);
            this.richTextBoxReceiveContent.TabIndex = 0;
            this.richTextBoxReceiveContent.TabStop = false;
            this.richTextBoxReceiveContent.Text = "";
            //
            // menuStrip
            //
            this.menuStrip.BackColor = System.Drawing.Color.Transparent;
            this.menuStrip.Font = new System.Drawing.Font("新宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.LoginToolStripMenuItem,
            this.QuickLoginToolStripMenuItem,
            this.DoudizhuToolStripMenuItem,
            this.LogoutToolStripMenuItem,
            this.ExitToolStripMenuItem});
            this.menuStrip.Location = new System.Drawing.Point(0, 0);
            this.menuStrip.Name = "menuStrip";
            this.menuStrip.Padding = new System.Windows.Forms.Padding(7, 3, 0, 3);
            this.menuStrip.Size = new System.Drawing.Size(880, 25);
            this.menuStrip.TabIndex = 0;
            //
            // LoginToolStripMenuItem
            //
            this.LoginToolStripMenuItem.ForeColor = System.Drawing.Color.Blue;
            this.LoginToolStripMenuItem.Name = "LoginToolStripMenuItem";
            this.LoginToolStripMenuItem.Size = new System.Drawing.Size(47, 19);
            this.LoginToolStripMenuItem.Text = "登陆";
            this.LoginToolStripMenuItem.Click += new System.EventHandler(this.LoginToolStripMenuItem_Click);
            //
            // QuickLoginToolStripMenuItem
            //
            this.QuickLoginToolStripMenuItem.ForeColor = System.Drawing.Color.Blue;
            this.QuickLoginToolStripMenuItem.Name = "QuickLoginToolStripMenuItem";
            this.QuickLoginToolStripMenuItem.Size = new System.Drawing.Size(75, 19);
            this.QuickLoginToolStripMenuItem.Text = "快速登陆";
            this.QuickLoginToolStripMenuItem.Click += new System.EventHandler(this.QuickLoginToolStripMenuItem_Click);
            //
            // DoudizhuToolStripMenuItem
            //
            this.DoudizhuToolStripMenuItem.Name = "DoudizhuToolStripMenuItem";
            this.DoudizhuToolStripMenuItem.Size = new System.Drawing.Size(145, 19);
            this.DoudizhuToolStripMenuItem.Text = "启动“斗地主”游戏";
            this.DoudizhuToolStripMenuItem.Click += new System.EventHandler(this.DoudizhuToolStripMenuItem_Click);
            //
            // LogoutToolStripMenuItem
            //
            this.LogoutToolStripMenuItem.ForeColor = System.Drawing.Color.Red;
            this.LogoutToolStripMenuItem.Name = "LogoutToolStripMenuItem";
            this.LogoutToolStripMenuItem.Size = new System.Drawing.Size(47, 19);
            this.LogoutToolStripMenuItem.Text = "注销";
            this.LogoutToolStripMenuItem.Click += new System.EventHandler(this.LogoutToolStripMenuItem_Click);
            //
            // ExitToolStripMenuItem
            //
            this.ExitToolStripMenuItem.Name = "ExitToolStripMenuItem";
            this.ExitToolStripMenuItem.Size = new System.Drawing.Size(47, 19);
            this.ExitToolStripMenuItem.Text = "退出";
            this.ExitToolStripMenuItem.Click += new System.EventHandler(this.ExitToolStripMenuItem_Click);
            //
            // groupBox
            //
            this.groupBox.Controls.Add(this.textBoxSendtolabel);
            this.groupBox.Controls.Add(this.textBoxSendTo);
            this.groupBox.Controls.Add(this.buttonCancel);
            this.groupBox.Controls.Add(this.buttonToAll);
            this.groupBox.Controls.Add(this.buttonSelect);
            this.groupBox.Controls.Add(this.buttonSend);
            this.groupBox.Controls.Add(this.buttonStopFileTransmission);
            this.groupBox.Controls.Add(this.buttonSendFile);
            this.groupBox.Controls.Add(this.listViewClientTable);
            this.groupBox.Controls.Add(this.textBoxSendContent);
            this.groupBox.Controls.Add(this.richTextBoxReceiveContent);
            this.groupBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox.Font = new System.Drawing.Font("新宋体", 1F);
            this.groupBox.Location = new System.Drawing.Point(0, 25);
            this.groupBox.Name = "groupBox";
            this.groupBox.Size = new System.Drawing.Size(880, 632);
            this.groupBox.TabIndex = 2;
            this.groupBox.TabStop = false;
            //
            // textBoxSendtolabel
            //
            this.textBoxSendtolabel.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.textBoxSendtolabel.Font = new System.Drawing.Font("新宋体", 9.5F);
            this.textBoxSendtolabel.Location = new System.Drawing.Point(7, 454);
            this.textBoxSendtolabel.Multiline = true;
            this.textBoxSendtolabel.Name = "textBoxSendtolabel";
            this.textBoxSendtolabel.ReadOnly = true;
            this.textBoxSendtolabel.Size = new System.Drawing.Size(49, 28);
            this.textBoxSendtolabel.TabIndex = 5;
            this.textBoxSendtolabel.Text = "发送到\r\n";
            this.textBoxSendtolabel.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            //
            // textBoxSendTo
            //
            this.textBoxSendTo.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.textBoxSendTo.Font = new System.Drawing.Font("新宋体", 9.5F);
            this.textBoxSendTo.Location = new System.Drawing.Point(62, 454);
            this.textBoxSendTo.Multiline = true;
            this.textBoxSendTo.Name = "textBoxSendTo";
            this.textBoxSendTo.ReadOnly = true;
            this.textBoxSendTo.Size = new System.Drawing.Size(555, 28);
            this.textBoxSendTo.TabIndex = 1;
            //
            // buttonCancel
            //
            this.buttonCancel.Font = new System.Drawing.Font("新宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.buttonCancel.Location = new System.Drawing.Point(794, 597);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(80, 30);
            this.buttonCancel.TabIndex = 0;
            this.buttonCancel.TabStop = false;
            this.buttonCancel.Text = "取消";
            this.buttonCancel.UseVisualStyleBackColor = true;
            this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
            //
            // buttonToAll
            //
            this.buttonToAll.Font = new System.Drawing.Font("新宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.buttonToAll.Location = new System.Drawing.Point(709, 597);
            this.buttonToAll.Name = "buttonToAll";
            this.buttonToAll.Size = new System.Drawing.Size(80, 30);
            this.buttonToAll.TabIndex = 0;
            this.buttonToAll.TabStop = false;
            this.buttonToAll.Text = "全体";
            this.buttonToAll.UseVisualStyleBackColor = true;
            this.buttonToAll.Click += new System.EventHandler(this.buttonToAll_Click);
            //
            // buttonSelect
            //
            this.buttonSelect.Font = new System.Drawing.Font("新宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.buttonSelect.Location = new System.Drawing.Point(624, 597);
            this.buttonSelect.Name = "buttonSelect";
            this.buttonSelect.Size = new System.Drawing.Size(80, 30);
            this.buttonSelect.TabIndex = 0;
            this.buttonSelect.TabStop = false;
            this.buttonSelect.Text = "选择";
            this.buttonSelect.UseVisualStyleBackColor = true;
            this.buttonSelect.Click += new System.EventHandler(this.buttonSelect_Click);
            //
            // buttonSend
            //
            this.buttonSend.Font = new System.Drawing.Font("新宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.buttonSend.Location = new System.Drawing.Point(497, 597);
            this.buttonSend.Name = "buttonSend";
            this.buttonSend.Size = new System.Drawing.Size(120, 30);
            this.buttonSend.TabIndex = 0;
            this.buttonSend.TabStop = false;
            this.buttonSend.Text = "发送文本";
            this.buttonSend.UseVisualStyleBackColor = true;
            this.buttonSend.Click += new System.EventHandler(this.buttonSend_Click);
            //
            // buttonStopFileTransmission
            //
            this.buttonStopFileTransmission.Font = new System.Drawing.Font("新宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.buttonStopFileTransmission.Location = new System.Drawing.Point(133, 597);
            this.buttonStopFileTransmission.Name = "buttonStopFileTransmission";
            this.buttonStopFileTransmission.Size = new System.Drawing.Size(120, 30);
            this.buttonStopFileTransmission.TabIndex = 0;
            this.buttonStopFileTransmission.TabStop = false;
            this.buttonStopFileTransmission.Text = "中止文件传输";
            this.buttonStopFileTransmission.UseVisualStyleBackColor = true;
            this.buttonStopFileTransmission.Click += new System.EventHandler(this.buttonStopFileTransmission_Click);
            //
            // buttonSendFile
            //
            this.buttonSendFile.Font = new System.Drawing.Font("新宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.buttonSendFile.Location = new System.Drawing.Point(7, 597);
            this.buttonSendFile.Name = "buttonSendFile";
            this.buttonSendFile.Size = new System.Drawing.Size(120, 30);
            this.buttonSendFile.TabIndex = 0;
            this.buttonSendFile.TabStop = false;
            this.buttonSendFile.Text = "发送文件";
            this.buttonSendFile.UseVisualStyleBackColor = true;
            this.buttonSendFile.Click += new System.EventHandler(this.buttonSendFile_Click);
            //
            // listViewClientTable
            //
            this.listViewClientTable.Font = new System.Drawing.Font("新宋体", 10.5F);
            this.listViewClientTable.Location = new System.Drawing.Point(624, 9);
            this.listViewClientTable.Name = "listViewClientTable";
            this.listViewClientTable.Size = new System.Drawing.Size(250, 582);
            this.listViewClientTable.TabIndex = 0;
            this.listViewClientTable.TabStop = false;
            this.listViewClientTable.UseCompatibleStateImageBehavior = false;
            //
            // textBoxSendContent
            //
            this.textBoxSendContent.Font = new System.Drawing.Font("新宋体", 10.5F);
            this.textBoxSendContent.Location = new System.Drawing.Point(7, 486);
            this.textBoxSendContent.Multiline = true;
            this.textBoxSendContent.Name = "textBoxSendContent";
            this.textBoxSendContent.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBoxSendContent.Size = new System.Drawing.Size(610, 105);
            this.textBoxSendContent.TabIndex = 0;
            this.textBoxSendContent.TabStop = false;
            this.textBoxSendContent.TextChanged += new System.EventHandler(this.textBoxSendContent_TextChanged);
            this.textBoxSendContent.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBoxSendContent_KeyDown);
            //
            // labelStatus
            //
            this.labelStatus.BackColor = System.Drawing.Color.Transparent;
            this.labelStatus.Font = new System.Drawing.Font("新宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.labelStatus.ForeColor = System.Drawing.Color.Red;
            this.labelStatus.Location = new System.Drawing.Point(374, 3);
            this.labelStatus.Name = "labelStatus";
            this.labelStatus.Size = new System.Drawing.Size(500, 21);
            this.labelStatus.TabIndex = 6;
            this.labelStatus.Text = "未登录";
            this.labelStatus.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // timer
            //
            this.timer.Enabled = true;
            this.timer.Interval = 5000;
            this.timer.Tick += new System.EventHandler(this.timer_Tick);
            //
            // FormMain
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(880, 657);
            this.Controls.Add(this.labelStatus);
            this.Controls.Add(this.groupBox);
            this.Controls.Add(this.menuStrip);
            this.Font = new System.Drawing.Font("新宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.KeyPreview = true;
            this.MainMenuStrip = this.menuStrip;
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(900, 700);
            this.MinimumSize = new System.Drawing.Size(900, 700);
            this.Name = "FormMain";
            this.Text = "IM客户端";
            this.Load += new System.EventHandler(this.FormMain_Load);
            this.menuStrip.ResumeLayout(false);
            this.menuStrip.PerformLayout();
            this.groupBox.ResumeLayout(false);
            this.groupBox.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.RichTextBox richTextBoxReceiveContent;
        private System.Windows.Forms.MenuStrip menuStrip;
        private System.Windows.Forms.ToolStripMenuItem QuickLoginToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem DoudizhuToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem LogoutToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem ExitToolStripMenuItem;
        private System.Windows.Forms.GroupBox groupBox;
        private System.Windows.Forms.Button buttonSendFile;
        private System.Windows.Forms.ListView listViewClientTable;
        private System.Windows.Forms.TextBox textBoxSendContent;
        private System.Windows.Forms.Button buttonSend;
        private System.Windows.Forms.Button buttonSelect;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.Button buttonToAll;
        private System.Windows.Forms.ToolStripMenuItem LoginToolStripMenuItem;
        private System.Windows.Forms.TextBox textBoxSendTo;
        private System.Windows.Forms.TextBox textBoxSendtolabel;
        private System.Windows.Forms.Button buttonStopFileTransmission;
        private System.Windows.Forms.Label labelStatus;
        private System.Windows.Forms.Timer timer;

        public System.Windows.Forms.Label LabelStatus
        {
            get { return labelStatus; }
            set { labelStatus = value; }
        }
    }
}

