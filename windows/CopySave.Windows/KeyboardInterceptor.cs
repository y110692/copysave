using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CopySave.Windows
{
    internal sealed class KeyboardInterceptor : IDisposable
    {
        private readonly NativeMethods.LowLevelKeyboardProc callback;
        private IntPtr hookHandle;
        private bool swallowUntilVKeyUp;

        public KeyboardInterceptor()
        {
            callback = HookCallback;
        }

        public event EventHandler PasteIntercepted;

        public void Start()
        {
            if (hookHandle != IntPtr.Zero)
            {
                return;
            }

            using (var process = Process.GetCurrentProcess())
            using (var module = process.MainModule)
            {
                hookHandle = NativeMethods.SetWindowsHookEx(
                    NativeMethods.WH_KEYBOARD_LL,
                    callback,
                    NativeMethods.GetModuleHandle(module.ModuleName),
                    0);
            }

            if (hookHandle == IntPtr.Zero)
            {
                throw new InvalidOperationException("Failed to install keyboard hook.");
            }
        }

        public void Dispose()
        {
            if (hookHandle != IntPtr.Zero)
            {
                NativeMethods.UnhookWindowsHookEx(hookHandle);
                hookHandle = IntPtr.Zero;
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode < 0)
            {
                return NativeMethods.CallNextHookEx(hookHandle, nCode, wParam, lParam);
            }

            var message = wParam.ToInt32();
            var keyInfo = (NativeMethods.KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(NativeMethods.KBDLLHOOKSTRUCT));
            var isInjected = (keyInfo.flags & NativeMethods.LLKHF_INJECTED) != 0;
            var isDown = message == NativeMethods.WM_KEYDOWN || message == NativeMethods.WM_SYSKEYDOWN;
            var isUp = message == NativeMethods.WM_KEYUP || message == NativeMethods.WM_SYSKEYUP;

            if (keyInfo.vkCode == NativeMethods.VK_V)
            {
                if (swallowUntilVKeyUp && isUp)
                {
                    swallowUntilVKeyUp = false;
                    return (IntPtr)1;
                }

                if (!swallowUntilVKeyUp
                    && !isInjected
                    && isDown
                    && IsOnlyCtrlPressed()
                    && ExplorerContextResolver.IsEligibleExplorerForeground()
                    && ClipboardPayloadReader.HasSavableClipboard())
                {
                    swallowUntilVKeyUp = true;
                    OnPasteIntercepted();
                    return (IntPtr)1;
                }
            }

            if (isUp && (keyInfo.vkCode == NativeMethods.VK_CONTROL || keyInfo.vkCode == NativeMethods.VK_LCONTROL || keyInfo.vkCode == NativeMethods.VK_RCONTROL))
            {
                swallowUntilVKeyUp = false;
            }

            return NativeMethods.CallNextHookEx(hookHandle, nCode, wParam, lParam);
        }

        private bool IsOnlyCtrlPressed()
        {
            return IsPressed(NativeMethods.VK_CONTROL)
                && !IsPressed(NativeMethods.VK_SHIFT)
                && !IsPressed(NativeMethods.VK_MENU)
                && !IsPressed(NativeMethods.VK_LWIN)
                && !IsPressed(NativeMethods.VK_RWIN);
        }

        private bool IsPressed(int virtualKey)
        {
            return (NativeMethods.GetAsyncKeyState(virtualKey) & 0x8000) != 0;
        }

        private void OnPasteIntercepted()
        {
            var handler = PasteIntercepted;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }
    }
}
