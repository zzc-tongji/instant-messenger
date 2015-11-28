namespace IM
{
    partial class FormLogin
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.textBoxPort = new System.Windows.Forms.TextBox();
            this.labelPort = new System.Windows.Forms.Label();
            this.labelIPv4 = new System.Windows.Forms.Label();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.buttonOK = new System.Windows.Forms.Button();
            this.textBoxIPv4 = new System.Windows.Forms.TextBox();
            this.labelUsername = new System.Windows.Forms.Label();
            this.labelPassword = new System.Windows.Forms.Label();
            this.textBoxPassword = new System.Windows.Forms.TextBox();
            this.textBoxUsername = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            //
            // textBoxPort
            //
            this.textBoxPort.Location = new System.Drawing.Point(12, 74);
            this.textBoxPort.Name = "textBoxPort";
            this.textBoxPort.Size = new System.Drawing.Size(220, 23);
            this.textBoxPort.TabIndex = 3;
            this.textBoxPort.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.textBoxPort.TextChanged += new System.EventHandler(this.textBoxPort_TextChanged);
            //
            // labelPort
            //
            this.labelPort.AutoSize = true;
            this.labelPort.Location = new System.Drawing.Point(12, 58);
            this.labelPort.Name = "labelPort";
            this.labelPort.Size = new System.Drawing.Size(49, 14);
            this.labelPort.TabIndex = 0;
            this.labelPort.Text = "端口号";
            //
            // labelIPv4
            //
            this.labelIPv4.AutoSize = true;
            this.labelIPv4.Location = new System.Drawing.Point(12, 9);
            this.labelIPv4.Name = "labelIPv4";
            this.labelIPv4.Size = new System.Drawing.Size(63, 14);
            this.labelIPv4.TabIndex = 0;
            this.labelIPv4.Text = "IPv4地址";
            //
            // buttonCancel
            //
            this.buttonCancel.Location = new System.Drawing.Point(132, 215);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(100, 30);
            this.buttonCancel.TabIndex = 0;
            this.buttonCancel.TabStop = false;
            this.buttonCancel.Text = "取消";
            this.buttonCancel.UseVisualStyleBackColor = true;
            this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
            //
            // buttonOK
            //
            this.buttonOK.Location = new System.Drawing.Point(12, 215);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(100, 30);
            this.buttonOK.TabIndex = 1;
            this.buttonOK.Text = "确定";
            this.buttonOK.UseVisualStyleBackColor = true;
            this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
            //
            // textBoxIPv4
            //
            this.textBoxIPv4.Location = new System.Drawing.Point(12, 25);
            this.textBoxIPv4.Name = "textBoxIPv4";
            this.textBoxIPv4.Size = new System.Drawing.Size(220, 23);
            this.textBoxIPv4.TabIndex = 2;
            this.textBoxIPv4.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.textBoxIPv4.TextChanged += new System.EventHandler(this.textBoxIPv4_TextChanged);
            //
            // labelUsername
            //
            this.labelUsername.AutoSize = true;
            this.labelUsername.Location = new System.Drawing.Point(12, 107);
            this.labelUsername.Name = "labelUsername";
            this.labelUsername.Size = new System.Drawing.Size(49, 14);
            this.labelUsername.TabIndex = 0;
            this.labelUsername.Text = "用户名";
            //
            // labelPassword
            //
            this.labelPassword.AutoSize = true;
            this.labelPassword.Location = new System.Drawing.Point(12, 157);
            this.labelPassword.Name = "labelPassword";
            this.labelPassword.Size = new System.Drawing.Size(35, 14);
            this.labelPassword.TabIndex = 0;
            this.labelPassword.Text = "密码";
            //
            // textBoxPassword
            //
            this.textBoxPassword.Location = new System.Drawing.Point(12, 173);
            this.textBoxPassword.Name = "textBoxPassword";
            this.textBoxPassword.PasswordChar = '*';
            this.textBoxPassword.Size = new System.Drawing.Size(220, 23);
            this.textBoxPassword.TabIndex = 5;
            this.textBoxPassword.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            //
            // textBoxUsername
            //
            this.textBoxUsername.Location = new System.Drawing.Point(12, 124);
            this.textBoxUsername.Name = "textBoxUsername";
            this.textBoxUsername.Size = new System.Drawing.Size(220, 23);
            this.textBoxUsername.TabIndex = 4;
            this.textBoxUsername.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.textBoxUsername.TextChanged += new System.EventHandler(this.textBoxUsername_TextChanged);
            //
            // FormLogin
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(244, 257);
            this.ControlBox = false;
            this.Controls.Add(this.textBoxUsername);
            this.Controls.Add(this.textBoxIPv4);
            this.Controls.Add(this.textBoxPassword);
            this.Controls.Add(this.textBoxPort);
            this.Controls.Add(this.labelPassword);
            this.Controls.Add(this.labelPort);
            this.Controls.Add(this.labelUsername);
            this.Controls.Add(this.labelIPv4);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonOK);
            this.Font = new System.Drawing.Font("宋体", 10.5F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MinimumSize = new System.Drawing.Size(250, 200);
            this.Name = "FormLogin";
            this.Text = "登陆";
            this.TopMost = true;
            this.Load += new System.EventHandler(this.FormLogin_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBoxPort;
        private System.Windows.Forms.Label labelPort;
        private System.Windows.Forms.Label labelIPv4;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.TextBox textBoxIPv4;
        private System.Windows.Forms.Label labelUsername;
        private System.Windows.Forms.Label labelPassword;
        private System.Windows.Forms.TextBox textBoxPassword;
        private System.Windows.Forms.TextBox textBoxUsername;
    }
}
