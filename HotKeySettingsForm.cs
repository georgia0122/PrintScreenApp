using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PrintScreenApp
{
    /// <summary>
    /// 快捷键设置对话框：用户可添加 / 删除 / 重置截图触发热键
    /// </summary>
    public sealed class HotKeySettingsForm : Form
    {
        private readonly ListBox _listBox;
        private readonly Button _addButton;
        private readonly Button _removeButton;
        private readonly Button _resetButton;
        private readonly Button _okButton;
        private readonly Button _cancelButton;
        private readonly Label _hint;

        public HotKeyConfig Config { get; private set; }

        public HotKeySettingsForm(HotKeyConfig current)
        {
            Config = new HotKeyConfig { Entries = [.. current.Entries.Select(Clone)] };

            Text = "快捷键设置";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(420, 320);
            ShowInTaskbar = false;

            _hint = new Label
            {
                Text = "下面列出当前已注册的截图快捷键。可添加多个候选，按任意一组都能触发截图。",
                Location = new Point(12, 10),
                Size = new Size(396, 38),
                ForeColor = Color.FromArgb(80, 80, 80)
            };

            _listBox = new ListBox
            {
                Location = new Point(12, 52),
                Size = new Size(280, 220),
                IntegralHeight = false
            };

            _addButton = new Button { Text = "添加…", Location = new Point(304, 52), Size = new Size(104, 28) };
            _removeButton = new Button { Text = "删除", Location = new Point(304, 86), Size = new Size(104, 28) };
            _resetButton = new Button { Text = "恢复默认", Location = new Point(304, 120), Size = new Size(104, 28) };

            _okButton = new Button
            {
                Text = "保存",
                Location = new Point(232, 282),
                Size = new Size(80, 28),
                DialogResult = DialogResult.OK
            };
            _cancelButton = new Button
            {
                Text = "取消",
                Location = new Point(320, 282),
                Size = new Size(80, 28),
                DialogResult = DialogResult.Cancel
            };

            AcceptButton = _okButton;
            CancelButton = _cancelButton;

            Controls.AddRange([_hint, _listBox, _addButton, _removeButton, _resetButton, _okButton, _cancelButton]);

            _addButton.Click += (_, _) => AddHotKey();
            _removeButton.Click += (_, _) => RemoveSelected();
            _resetButton.Click += (_, _) => ResetToDefault();
            _okButton.Click += (_, _) =>
            {
                if (Config.Entries.Count == 0)
                {
                    MessageBox.Show(this, "至少要保留一个快捷键。", "无法保存",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    DialogResult = DialogResult.None;
                }
            };

            RefreshList();
        }

        private static HotKeyEntry Clone(HotKeyEntry e) => new()
        {
            Ctrl = e.Ctrl,
            Alt = e.Alt,
            Shift = e.Shift,
            Win = e.Win,
            Key = e.Key
        };

        private void RefreshList()
        {
            _listBox.BeginUpdate();
            _listBox.Items.Clear();
            foreach (HotKeyEntry e in Config.Entries)
            {
                _listBox.Items.Add(e.DisplayName);
            }
            _listBox.EndUpdate();
        }

        private void AddHotKey()
        {
            using var capture = new HotKeyCaptureForm();
            if (capture.ShowDialog(this) == DialogResult.OK && capture.Result != null)
            {
                if (!capture.Result.IsValid)
                {
                    MessageBox.Show(this, "需要至少一个修饰键 (Ctrl/Alt/Shift/Win) 加一个普通按键。",
                        "无效快捷键", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (Config.Entries.Any(e => SameCombo(e, capture.Result)))
                {
                    MessageBox.Show(this, "这个组合键已经在列表中。",
                        "重复", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                Config.Entries.Add(capture.Result);
                RefreshList();
                _listBox.SelectedIndex = _listBox.Items.Count - 1;
            }
        }

        private static bool SameCombo(HotKeyEntry a, HotKeyEntry b) =>
            a.Ctrl == b.Ctrl && a.Alt == b.Alt && a.Shift == b.Shift && a.Win == b.Win && a.Key == b.Key;

        private void RemoveSelected()
        {
            int idx = _listBox.SelectedIndex;
            if (idx >= 0 && idx < Config.Entries.Count)
            {
                Config.Entries.RemoveAt(idx);
                RefreshList();
            }
        }

        private void ResetToDefault()
        {
            Config = HotKeyConfig.CreateDefault();
            RefreshList();
        }
    }

    /// <summary>
    /// 用来捕获用户敲下的组合键
    /// </summary>
    public sealed class HotKeyCaptureForm : Form
    {
        private readonly Label _label;
        private readonly Button _ok;
        private readonly Button _cancel;
        public HotKeyEntry? Result { get; private set; }

        public HotKeyCaptureForm()
        {
            Text = "录制快捷键";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(420, 200);
            KeyPreview = true;
            ShowInTaskbar = false;
            BackColor = Color.White;

            var hint = new Label
            {
                Text = "请直接在键盘上按下你想用的组合键\n（Ctrl/Alt/Shift/Win + 任意按键）",
                Location = new Point(12, 12),
                Size = new Size(396, 44),
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(80, 80, 80)
            };

            _label = new Label
            {
                Text = "等待按键…",
                Location = new Point(12, 60),
                Size = new Size(396, 70),
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(0, 120, 212),
                BackColor = Color.FromArgb(245, 248, 252),
                BorderStyle = BorderStyle.FixedSingle
            };

            _ok = new Button
            {
                Text = "确定",
                Location = new Point(228, 152),
                Size = new Size(88, 34),
                Enabled = false,
                DialogResult = DialogResult.OK
            };
            _cancel = new Button
            {
                Text = "取消",
                Location = new Point(322, 152),
                Size = new Size(88, 34),
                DialogResult = DialogResult.Cancel
            };

            Controls.AddRange([hint, _label, _ok, _cancel]);
            AcceptButton = _ok;
            CancelButton = _cancel;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            // 忽略只有修饰键的情况
            if (e.KeyCode is Keys.ControlKey or Keys.Menu or Keys.ShiftKey
                or Keys.LWin or Keys.RWin or Keys.None)
            {
                return;
            }

            HotKeyEntry entry = new()
            {
                Ctrl = e.Control,
                Alt = e.Alt,
                Shift = e.Shift,
                Win = (Control.ModifierKeys & Keys.LWin) != 0
                      || ((GetAsyncKeyState(0x5B) & 0x8000) != 0)
                      || ((GetAsyncKeyState(0x5C) & 0x8000) != 0),
                Key = e.KeyCode
            };

            Result = entry;
            _label.Text = entry.DisplayName;
            _ok.Enabled = entry.IsValid;
            e.SuppressKeyPress = true;
            e.Handled = true;
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);
    }
}
