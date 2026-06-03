using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PrintScreenApp
{
    public sealed class HotKeyMessageWindow : NativeWindow, IDisposable
    {
        private const int WmHotkey = 0x0312;
        private static int _nextHotKeyId = Environment.TickCount & 0x3FFF;
        private readonly int _hotKeyId;
        private bool _isRegistered;
        private bool _disposed;

        public event EventHandler? Pressed;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        public HotKeyMessageWindow()
        {
            _hotKeyId = GetNextHotKeyId();
            CreateHandle(new CreateParams());
        }

        public bool IsRegistered => _isRegistered;

        public void Register(HotKeyManager.KeyModifiers modifiers, Keys key)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(HotKeyMessageWindow));
            }

            if (_isRegistered)
            {
                throw new InvalidOperationException("Hotkey is already registered.");
            }

            uint modifierFlags = (uint)(modifiers | HotKeyManager.KeyModifiers.NoRepeat);
            uint virtualKey = (uint)key;
            bool ok = RegisterHotKey(Handle, _hotKeyId, modifierFlags, virtualKey);

            if (!ok)
            {
                int errorCode = Marshal.GetLastWin32Error();
                throw new InvalidOperationException($"Failed to register hotkey. Error code: {errorCode}.");
            }

            _isRegistered = true;
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WmHotkey && (int)m.WParam == _hotKeyId)
            {
                Pressed?.Invoke(this, EventArgs.Empty);
                return;
            }

            base.WndProc(ref m);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            if (_isRegistered)
            {
                UnregisterHotKey(Handle, _hotKeyId);
                _isRegistered = false;
            }

            DestroyHandle();
            _disposed = true;
        }

        private static int GetNextHotKeyId()
        {
            _nextHotKeyId++;
            if (_nextHotKeyId > 0xBFFF)
            {
                _nextHotKeyId = 1;
            }

            return _nextHotKeyId;
        }
    }
}
