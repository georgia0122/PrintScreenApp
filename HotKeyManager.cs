using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PrintScreenApp
{
    /// <summary>
    /// Global hotkey manager class using Windows API
    /// </summary>
    public class HotKeyManager : IDisposable
    {
        /// <summary>
        /// Common hotkey modifier flags
        /// </summary>
        [Flags]
        public enum KeyModifiers
        {
            None = 0,
            Alt = 0x0001,
            Ctrl = 0x0002,
            Shift = 0x0004,
            Win = 0x0008,
            NoRepeat = 0x4000
        }

        private const int WmHotkey = 0x0312;
        private int _hotKeyId;
        private uint _registeredMod;
        private uint _registeredVk;
        private IntPtr _handle;
        private bool _isRegistered = false;
        private bool _disposed = false;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        /// <summary>
        /// Initialize HotKeyManager
        /// </summary>
        /// <param name="windowHandle">Window handle to register hotkey</param>
        public HotKeyManager(IntPtr windowHandle)
        {
            if (windowHandle == IntPtr.Zero)
            {
                throw new ArgumentException("Window handle cannot be null", nameof(windowHandle));
            }
            _handle = windowHandle;
        }

        /// <summary>
        /// Register global hotkey
        /// </summary>
        /// <param name="modifiers">Modifiers (combination of Ctrl, Alt, Shift, Win)</param>
        /// <param name="keyCode">Virtual key code</param>
        /// <returns>Hotkey ID</returns>
        public int Register(KeyModifiers modifiers, Keys keyCode)
        {
            ThrowIfDisposed();

            if (_isRegistered)
            {
                throw new InvalidOperationException("Hotkey is already registered. Please unregister first before registering a new one.");
            }

            try
            {
                uint mod = (uint)modifiers;
                uint vk = (uint)keyCode & 0xFF;
                _hotKeyId = Environment.TickCount & 0xBFFF;

                bool result = RegisterHotKey(_handle, _hotKeyId, mod, vk);

                if (!result)
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    throw new InvalidOperationException(
                        $"Failed to register hotkey. Error code: {errorCode}. " +
                        "Possible reasons: Hotkey is already used by another application or window handle is invalid.");
                }

                _registeredMod = mod;
                _registeredVk = vk;
                _isRegistered = true;
                return _hotKeyId;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Exception occurred while registering hotkey: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Unregister hotkey
        /// </summary>
        public void Unregister()
        {
            if (!_isRegistered || _disposed)
            {
                return;
            }

            UnregisterHotKey(_handle, _hotKeyId);
            _isRegistered = false;
        }

        /// <summary>
        /// Check if hotkey ID is the one registered by this manager
        /// </summary>
        public bool IsHotKeyMessage(Message message)
        {
            if (message.Msg != WmHotkey || !_isRegistered)
            {
                return false;
            }

            if ((int)message.WParam != _hotKeyId)
            {
                return false;
            }

            int lParam = message.LParam.ToInt32();
            uint mod = (uint)(lParam & 0xFFFF);
            uint vk = (uint)((lParam >> 16) & 0xFFFF);
            return mod == _registeredMod && vk == _registeredVk;
        }

        /// <summary>
        /// Check if hotkey is registered
        /// </summary>
        public bool IsRegistered => _isRegistered;

        /// <summary>
        /// Clean up resources
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
