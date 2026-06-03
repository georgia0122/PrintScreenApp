using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PrintScreenApp
{
    public sealed class GlobalKeyboardHook : IDisposable
    {
        private const int WhKeyboardLl = 13;
        private const int WmKeydown = 0x0100;
        private const int WmSyskeydown = 0x0104;
        private const int VkMenu = 0x12;
        private const int VkControl = 0x11;
        private const int VkShift = 0x10;
        private const int LlkhfAltdown = 0x20;

        private readonly LowLevelKeyboardProc _proc;
        private IntPtr _hookId;
        private bool _disposed;

        public event EventHandler<Keys>? KeyPressed;

        public GlobalKeyboardHook()
        {
            _proc = HookCallback;
            _hookId = SetHook(_proc);
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string? lpModuleName);

        [DllImport("user32.dll")]
        private static extern short GetKeyState(int nVirtKey);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using Process currentProcess = Process.GetCurrentProcess();
            using ProcessModule currentModule = currentProcess.MainModule!;
            return SetWindowsHookEx(WhKeyboardLl, proc, GetModuleHandle(currentModule.ModuleName), 0);
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (wParam == WmKeydown || wParam == WmSyskeydown))
            {
                KeyboardHookStruct keyInfo = Marshal.PtrToStructure<KeyboardHookStruct>(lParam);
                int vkCode = keyInfo.VirtualKeyCode;
                bool altDown = (keyInfo.Flags & LlkhfAltdown) != 0 || IsKeyDown(VkMenu);
                bool ctrlDown = IsKeyDown(VkControl);
                bool shiftDown = IsKeyDown(VkShift);

                bool isConfiguredCombo = ctrlDown && altDown && (vkCode == (int)Keys.Z || vkCode == (int)Keys.B);

                if (isConfiguredCombo)
                {
                    KeyPressed?.Invoke(this, (Keys)vkCode);
                    return (IntPtr)1;
                }
            }

            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            if (_hookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookId);
                _hookId = IntPtr.Zero;
            }

            _disposed = true;
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static bool IsKeyDown(int virtualKey)
        {
            return (GetAsyncKeyState(virtualKey) & unchecked((short)0x8000)) != 0
                || (GetKeyState(virtualKey) & unchecked((short)0x8000)) != 0;
        }

        [StructLayout(LayoutKind.Sequential)]
        private readonly struct KeyboardHookStruct
        {
            public readonly int VirtualKeyCode;
            public readonly int ScanCode;
            public readonly int Flags;
            public readonly int Time;
            public readonly IntPtr ExtraInfo;
        }
    }
}
