namespace PrintScreenApp
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            flowLayoutPanel1 = new FlowLayoutPanel();
            labelButton1 = new Label();
            labelButton2 = new Label();
            labelButton3 = new Label();
            labelTitle = new Label();
            labelHotkey = new Label();
            SuspendLayout();
            
            // 
            // labelTitle
            // 
            labelTitle.AutoSize = true;
            labelTitle.Font = new Font("Segoe UI", 22F, FontStyle.Bold);
            labelTitle.ForeColor = Color.FromArgb(32, 32, 35);
            labelTitle.Location = new Point(28, 30);
            labelTitle.Name = "labelTitle";
            labelTitle.Size = new Size(260, 40);
            labelTitle.TabIndex = 10;
            labelTitle.Text = "Screenshot Tool";
            
            // 
            // labelHotkey
            // 
            labelHotkey.AutoSize = true;
            labelHotkey.Font = new Font("Segoe UI", 10F, FontStyle.Underline);
            labelHotkey.ForeColor = Color.FromArgb(0, 120, 212);
            labelHotkey.Cursor = Cursors.Hand;
            labelHotkey.Location = new Point(30, 80);
            labelHotkey.Name = "labelHotkey";
            labelHotkey.Size = new Size(220, 19);
            labelHotkey.TabIndex = 11;
            labelHotkey.Text = "快捷键：点此设置";
            labelHotkey.Click += LabelHotkey_Click;

            // Tooltip：鼠标悬停显示「点击可以修改」
            ToolTip hotkeyTip = new ToolTip
            {
                AutoPopDelay = 5000,
                InitialDelay = 300,
                ReshowDelay = 200,
                ShowAlways = true
            };
            hotkeyTip.SetToolTip(labelHotkey, "点击可以修改快捷键");
            
            // 
            // flowLayoutPanel1
            // 
            flowLayoutPanel1.FlowDirection = FlowDirection.TopDown;
            flowLayoutPanel1.WrapContents = false;
            flowLayoutPanel1.Location = new Point(30, 120);
            flowLayoutPanel1.Name = "flowLayoutPanel1";
            flowLayoutPanel1.Size = new Size(260, 210);
            flowLayoutPanel1.TabIndex = 12;
            flowLayoutPanel1.Margin = new Padding(0);
            flowLayoutPanel1.Padding = new Padding(0);
            flowLayoutPanel1.Controls.Add(labelButton1);
            flowLayoutPanel1.Controls.Add(labelButton2);
            flowLayoutPanel1.Controls.Add(labelButton3);
            
            // 
            // labelButton1 - Region Screenshot
            // 
            labelButton1.BackColor = Color.FromArgb(0, 120, 212);
            labelButton1.Cursor = Cursors.Hand;
            labelButton1.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            labelButton1.ForeColor = Color.White;
            labelButton1.Location = new Point(0, 0);
            labelButton1.Margin = new Padding(0, 0, 0, 14);
            labelButton1.Name = "labelButton1";
            labelButton1.Padding = new Padding(16, 0, 0, 0);
            labelButton1.Size = new Size(260, 56);
            labelButton1.TabIndex = 0;
            labelButton1.Text = "📷  选择区域截图";
            labelButton1.TextAlign = ContentAlignment.MiddleLeft;
            labelButton1.Click += button1_Click;
            labelButton1.MouseEnter += Button_MouseEnter;
            labelButton1.MouseLeave += Button_MouseLeave;
            
            // 
            // labelButton2 - Save Screenshot
            // 
            labelButton2.BackColor = Color.FromArgb(107, 105, 214);
            labelButton2.Cursor = Cursors.Hand;
            labelButton2.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            labelButton2.ForeColor = Color.White;
            labelButton2.Location = new Point(0, 70);
            labelButton2.Margin = new Padding(0, 0, 0, 14);
            labelButton2.Name = "labelButton2";
            labelButton2.Padding = new Padding(16, 0, 0, 0);
            labelButton2.Size = new Size(260, 56);
            labelButton2.TabIndex = 1;
            labelButton2.Text = "💾  保存截图";
            labelButton2.TextAlign = ContentAlignment.MiddleLeft;
            labelButton2.Click += button2_Click;
            labelButton2.MouseEnter += Button_MouseEnter;
            labelButton2.MouseLeave += Button_MouseLeave;
            
            // 
            // labelButton3 - Copy to Clipboard
            // 
            labelButton3.BackColor = Color.FromArgb(59, 185, 72);
            labelButton3.Cursor = Cursors.Hand;
            labelButton3.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            labelButton3.ForeColor = Color.White;
            labelButton3.Location = new Point(0, 140);
            labelButton3.Margin = new Padding(0);
            labelButton3.Name = "labelButton3";
            labelButton3.Padding = new Padding(16, 0, 0, 0);
            labelButton3.Size = new Size(260, 56);
            labelButton3.TabIndex = 2;
            labelButton3.Text = "📋  复制到剪贴板";
            labelButton3.TextAlign = ContentAlignment.MiddleLeft;
            labelButton3.Click += button3_Click;
            labelButton3.MouseEnter += Button_MouseEnter;
            labelButton3.MouseLeave += Button_MouseLeave;
            
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(250, 251, 253);
            ClientSize = new Size(360, 380);
            Controls.Add(flowLayoutPanel1);
            Controls.Add(labelHotkey);
            Controls.Add(labelTitle);
            Font = new Font("Segoe UI", 9F);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Name = "Form1";
            ShowInTaskbar = true;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Screenshot Tool";
            WindowState = FormWindowState.Normal;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private FlowLayoutPanel flowLayoutPanel1;
        private Label labelButton1;
        private Label labelButton2;
        private Label labelButton3;
        private Label labelTitle;
        private Label labelHotkey;
    }
}

