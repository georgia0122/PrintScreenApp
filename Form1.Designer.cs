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
            button1 = new Button();
            button2 = new Button();
            button3 = new Button();
            labelTitle = new Label();
            labelHotkey = new Label();
            SuspendLayout();
            
            // 
            // labelTitle
            // 
            labelTitle.AutoSize = true;
            labelTitle.Font = new Font("Segoe UI", 20F, FontStyle.Bold);
            labelTitle.ForeColor = Color.FromArgb(45, 45, 48);
            labelTitle.Location = new Point(40, 30);
            labelTitle.Name = "labelTitle";
            labelTitle.Size = new Size(200, 35);
            labelTitle.TabIndex = 10;
            labelTitle.Text = "Screenshot Tool";
            
            // 
            // labelHotkey
            // 
            labelHotkey.AutoSize = true;
            labelHotkey.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            labelHotkey.ForeColor = Color.FromArgb(120, 120, 120);
            labelHotkey.Location = new Point(40, 65);
            labelHotkey.Name = "labelHotkey";
            labelHotkey.Size = new Size(220, 19);
            labelHotkey.TabIndex = 11;
            labelHotkey.Text = "Hotkey: Ctrl + Alt + Q";
            
            // 
            // button1 - Region Screenshot
            // 
            button1.BackColor = Color.FromArgb(0, 120, 212);
            button1.Cursor = Cursors.Hand;
            button1.FlatAppearance.BorderSize = 0;
            button1.FlatStyle = FlatStyle.Flat;
            button1.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            button1.ForeColor = Color.White;
            button1.Location = new Point(40, 130);
            button1.Name = "button1";
            button1.Size = new Size(220, 50);
            button1.TabIndex = 0;
            button1.Text = "📷 Region";
            button1.UseVisualStyleBackColor = false;
            button1.Click += button1_Click;
            button1.MouseEnter += Button_MouseEnter;
            button1.MouseLeave += Button_MouseLeave;
            
            // 
            // button2 - Save Screenshot
            // 
            button2.BackColor = Color.FromArgb(107, 105, 214);
            button2.Cursor = Cursors.Hand;
            button2.FlatAppearance.BorderSize = 0;
            button2.FlatStyle = FlatStyle.Flat;
            button2.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            button2.ForeColor = Color.White;
            button2.Location = new Point(40, 200);
            button2.Name = "button2";
            button2.Size = new Size(220, 50);
            button2.TabIndex = 1;
            button2.Text = "💾 Save";
            button2.UseVisualStyleBackColor = false;
            button2.Click += button2_Click;
            button2.MouseEnter += Button_MouseEnter;
            button2.MouseLeave += Button_MouseLeave;
            
            // 
            // button3 - Copy to Clipboard
            // 
            button3.BackColor = Color.FromArgb(59, 185, 72);
            button3.Cursor = Cursors.Hand;
            button3.FlatAppearance.BorderSize = 0;
            button3.FlatStyle = FlatStyle.Flat;
            button3.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            button3.ForeColor = Color.White;
            button3.Location = new Point(40, 270);
            button3.Name = "button3";
            button3.Size = new Size(220, 50);
            button3.TabIndex = 2;
            button3.Text = "📋 Copy";
            button3.UseVisualStyleBackColor = false;
            button3.Click += button3_Click;
            button3.MouseEnter += Button_MouseEnter;
            button3.MouseLeave += Button_MouseLeave;
            
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(255, 255, 255);
            ClientSize = new Size(320, 380);
            Controls.Add(labelHotkey);
            Controls.Add(labelTitle);
            Controls.Add(button1);
            Controls.Add(button2);
            Controls.Add(button3);
            Font = new Font("Segoe UI", 9F);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Screenshot Tool";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button button1;
        private Button button2;
        private Button button3;
        private Label labelTitle;
        private Label labelHotkey;
    }
}

