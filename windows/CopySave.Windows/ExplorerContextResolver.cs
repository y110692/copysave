using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace CopySave.Windows
{
    internal static class ExplorerContextResolver
    {
        private static readonly string[] AllowedFilePaneClasses =
        {
            "DirectUIHWND",
            "UIItemsView",
            "SHELLDLL_DefView",
            "SysListView32"
        };

        private static readonly string[] BlockedFocusClasses =
        {
            "Edit",
            "RichEditD2DPT",
            "RichEdit20WPT",
            "ComboBox",
            "ComboBoxEx32",
            "SearchEditBoxWndClass",
            "ToolbarWindow32",
            "Breadcrumb Parent",
            "Address Band Root",
            "Auto-Suggest Dropdown",
            "NamespaceTreeControl",
            "SysTreeView32",
            "TreeView"
        };

        public static bool IsEligibleExplorerForeground()
        {
            var foreground = NativeMethods.GetForegroundWindow();
            if (foreground == IntPtr.Zero)
            {
                return false;
            }

            var rootClassName = GetWindowClassName(foreground);
            if (rootClassName != "CabinetWClass"
                && rootClassName != "ExploreWClass"
                && rootClassName != "Progman"
                && rootClassName != "WorkerW")
            {
                return false;
            }

            return IsFilePaneFocused(foreground);
        }

        public static bool TryGetFrontFolderPath(out string folderPath)
        {
            folderPath = null;

            var foreground = NativeMethods.GetForegroundWindow();
            if (foreground == IntPtr.Zero)
            {
                return false;
            }

            var rootClassName = GetWindowClassName(foreground);
            if (rootClassName == "Progman" || rootClassName == "WorkerW")
            {
                folderPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                return Directory.Exists(folderPath);
            }

            if (rootClassName != "CabinetWClass" && rootClassName != "ExploreWClass")
            {
                return false;
            }

            object shellApplication = null;
            object shellWindows = null;

            try
            {
                var shellType = Type.GetTypeFromProgID("Shell.Application");
                if (shellType == null)
                {
                    return false;
                }

                shellApplication = Activator.CreateInstance(shellType);
                dynamic dynamicShell = shellApplication;
                shellWindows = dynamicShell.Windows();
                dynamic dynamicWindows = shellWindows;
                var count = Convert.ToInt32(dynamicWindows.Count, CultureInfo.InvariantCulture);

                for (var index = 0; index < count; index += 1)
                {
                    object window = null;
                    object document = null;
                    object folder = null;
                    object self = null;

                    try
                    {
                        window = dynamicWindows.Item(index);
                        if (window == null)
                        {
                            continue;
                        }

                        dynamic dynamicWindow = window;
                        var hwnd = (IntPtr)Convert.ToInt64(dynamicWindow.HWND, CultureInfo.InvariantCulture);
                        if (hwnd != foreground)
                        {
                            continue;
                        }

                        document = dynamicWindow.Document;
                        if (document == null)
                        {
                            continue;
                        }

                        dynamic dynamicDocument = document;
                        folder = dynamicDocument.Folder;
                        if (folder == null)
                        {
                            continue;
                        }

                        dynamic dynamicFolder = folder;
                        self = dynamicFolder.Self;
                        if (self == null)
                        {
                            continue;
                        }

                        dynamic dynamicSelf = self;
                        folderPath = Convert.ToString(dynamicSelf.Path, CultureInfo.InvariantCulture);
                        if (!string.IsNullOrWhiteSpace(folderPath) && Directory.Exists(folderPath))
                        {
                            return true;
                        }
                    }
                    catch (COMException)
                    {
                    }
                    catch (InvalidCastException)
                    {
                    }
                    finally
                    {
                        ReleaseComObject(self);
                        ReleaseComObject(folder);
                        ReleaseComObject(document);
                        ReleaseComObject(window);
                    }
                }
            }
            catch (COMException)
            {
            }
            finally
            {
                ReleaseComObject(shellWindows);
                ReleaseComObject(shellApplication);
            }

            folderPath = null;
            return false;
        }

        private static bool IsFilePaneFocused(IntPtr foreground)
        {
            IntPtr focusedWindow;
            if (!TryGetFocusedWindow(foreground, out focusedWindow))
            {
                return false;
            }

            if (WindowOrAncestorMatchesAnyClass(focusedWindow, BlockedFocusClasses))
            {
                return false;
            }

            return WindowOrAncestorMatchesAnyClass(focusedWindow, AllowedFilePaneClasses);
        }

        private static bool TryGetFocusedWindow(IntPtr foreground, out IntPtr focusedWindow)
        {
            focusedWindow = IntPtr.Zero;

            uint processId;
            var threadId = NativeMethods.GetWindowThreadProcessId(foreground, out processId);
            if (threadId == 0)
            {
                return false;
            }

            var guiThreadInfo = new NativeMethods.GUITHREADINFO();
            guiThreadInfo.cbSize = Marshal.SizeOf(typeof(NativeMethods.GUITHREADINFO));

            if (!NativeMethods.GetGUIThreadInfo(threadId, ref guiThreadInfo) || guiThreadInfo.hwndFocus == IntPtr.Zero)
            {
                return false;
            }

            focusedWindow = guiThreadInfo.hwndFocus;
            return true;
        }

        private static bool WindowOrAncestorMatchesAnyClass(IntPtr hwnd, string[] expectedClassNames)
        {
            for (var index = 0; index < expectedClassNames.Length; index += 1)
            {
                if (WindowOrAncestorMatchesClass(hwnd, expectedClassNames[index]))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool WindowOrAncestorMatchesClass(IntPtr hwnd, string expectedClassName)
        {
            var current = hwnd;
            while (current != IntPtr.Zero)
            {
                if (string.Equals(GetWindowClassName(current), expectedClassName, StringComparison.Ordinal))
                {
                    return true;
                }

                current = NativeMethods.GetParent(current);
            }

            return false;
        }

        private static string GetWindowClassName(IntPtr hwnd)
        {
            var builder = new StringBuilder(256);
            NativeMethods.GetClassName(hwnd, builder, builder.Capacity);
            return builder.ToString();
        }

        private static void ReleaseComObject(object value)
        {
            if (value != null && Marshal.IsComObject(value))
            {
                Marshal.FinalReleaseComObject(value);
            }
        }
    }
}
