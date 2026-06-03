using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;

namespace PrintScreenApp
{
    /// <summary>
    /// 单条热键配置（持久化用）
    /// </summary>
    public sealed class HotKeyEntry
    {
        public bool Ctrl { get; set; }
        public bool Alt { get; set; }
        public bool Shift { get; set; }
        public bool Win { get; set; }
        public Keys Key { get; set; } = Keys.None;

        public bool IsValid => Key != Keys.None && (Ctrl || Alt || Shift || Win);

        public HotKeyManager.KeyModifiers GetModifiers()
        {
            HotKeyManager.KeyModifiers m = HotKeyManager.KeyModifiers.None;
            if (Ctrl) m |= HotKeyManager.KeyModifiers.Ctrl;
            if (Alt) m |= HotKeyManager.KeyModifiers.Alt;
            if (Shift) m |= HotKeyManager.KeyModifiers.Shift;
            if (Win) m |= HotKeyManager.KeyModifiers.Win;
            return m;
        }

        public string DisplayName
        {
            get
            {
                List<string> parts = [];
                if (Ctrl) parts.Add("Ctrl");
                if (Alt) parts.Add("Alt");
                if (Shift) parts.Add("Shift");
                if (Win) parts.Add("Win");
                parts.Add(KeyDisplayName(Key));
                return string.Join(" + ", parts);
            }
        }

        public static string KeyDisplayName(Keys k)
        {
            // 数字、字母直接显示字符
            if (k >= Keys.A && k <= Keys.Z) return k.ToString();
            if (k >= Keys.D0 && k <= Keys.D9) return ((char)('0' + (k - Keys.D0))).ToString();
            return k.ToString();
        }
    }

    /// <summary>
    /// 热键集合配置（可序列化为 JSON）
    /// </summary>
    public sealed class HotKeyConfig
    {
        public List<HotKeyEntry> Entries { get; set; } = [];

        public static string ConfigPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PrintScreenApp",
            "hotkeys.json");

        public static HotKeyConfig CreateDefault() => new()
        {
            Entries =
            [
                new() { Ctrl = true, Alt = true, Key = Keys.Z },
                new() { Ctrl = true, Alt = true, Key = Keys.B },
                new() { Ctrl = true, Win = true, Key = Keys.Z },
                new() { Ctrl = true, Win = true, Key = Keys.B }
            ]
        };

        public static HotKeyConfig Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    string json = File.ReadAllText(ConfigPath);
                    HotKeyConfig? cfg = JsonSerializer.Deserialize<HotKeyConfig>(json);
                    if (cfg != null && cfg.Entries.Count > 0)
                    {
                        return cfg;
                    }
                }
            }
            catch
            {
                // 配置损坏：回落到默认
            }
            return CreateDefault();
        }

        public void Save()
        {
            string? dir = Path.GetDirectoryName(ConfigPath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }
            string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigPath, json);
        }
    }
}
