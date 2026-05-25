using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PrintScreenApp
{
    /// <summary>
    /// 全局快捷键管理类，使用 Windows API 实现
    /// </summary>
    public class HotKeyManager : IDisposable
    {
        /// <summary>
        /// 快捷键常用修饰符
        /// </summary>
        [Flags]
        public enum KeyModifiers
        {
            None = 0,
            Alt = 1,
            Ctrl = 2,
            Shift = 4,
            Win = 8
        }

        private const int WmHotkey = 0x0312;
        private int _hotKeyId = 1;
        private IntPtr _handle;
        private bool _isRegistered = false;
        private bool _disposed = false;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        /// <summary>
        /// 初始化HotKeyManager
        /// </summary>
        /// <param name="windowHandle">要注册快捷键的窗口句柄</param>
        public HotKeyManager(IntPtr windowHandle)
        {
            if (windowHandle == IntPtr.Zero)
            {
                throw new ArgumentException("窗口句柄不能为空", nameof(windowHandle));
            }
            _handle = windowHandle;
        }

        /// <summary>
        /// 注册全局快捷键
        /// </summary>
        /// <param name="modifiers">修饰符（Ctrl, Alt, Shift, Win 的组合）</param>
        /// <param name="keyCode">虚拟键代码</param>
        /// <returns>快捷键ID</returns>
        public int Register(KeyModifiers modifiers, Keys keyCode)
        {
            ThrowIfDisposed();

            if (_isRegistered)
            {
                throw new InvalidOperationException("快捷键已注册，请先注销后再注册新的快捷键");
            }

            try
            {
                uint mod = (uint)modifiers;
                uint vk = (uint)keyCode;

                bool result = RegisterHotKey(_handle, _hotKeyId, mod, vk);

                if (!result)
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    throw new InvalidOperationException(
                        $"注册快捷键失败。错误代码: {errorCode}。" +
                        "可能原因：快捷键已被其他应用程序占用，或窗口句柄无效。");
                }

                _isRegistered = true;
                return _hotKeyId;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"注册快捷键时出现异常: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 注销快捷键
        /// </summary>
        public void Unregister()
        {
            if (!_isRegistered || _disposed)
            {
                return;
            }

            try
            {
                bool result = UnregisterHotKey(_handle, _hotKeyId);

                if (!result)
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    throw new InvalidOperationException(
                        $"注销快捷键失败。错误代码: {errorCode}");
                }

                _isRegistered = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"注销快捷键时出现异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 判断快捷键ID是否为当前管理器注册的快捷键
        /// </summary>
        public bool IsHotKeyMessage(Message message)
        {
            return message.Msg == WmHotkey && (int)message.WParam == _hotKeyId;
        }

        /// <summary>
        /// 获取快捷键是否已注册
        /// </summary>
        public bool IsRegistered => _isRegistered;

        /// <summary>
        /// 清理资源
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            Unregister();
            _disposed = true;
            GC.SuppressFinalize(this);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        ~HotKeyManager()
        {
            Dispose();
        }
    }
}
