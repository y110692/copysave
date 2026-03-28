using Microsoft.Win32;
using System;
using System.Windows.Forms;

namespace CopySave.Windows
{
    internal static class StartupManager
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ValueName = "CopySave";

        public static void EnsureCurrentUserStartup()
        {
            using (var runKey = Registry.CurrentUser.OpenSubKey(RunKeyPath, true) ?? Registry.CurrentUser.CreateSubKey(RunKeyPath))
            {
                if (runKey == null)
                {
                    return;
                }

                var expectedValue = Quote(Application.ExecutablePath);
                var currentValue = Convert.ToString(runKey.GetValue(ValueName));
                if (!string.Equals(currentValue, expectedValue, StringComparison.OrdinalIgnoreCase))
                {
                    runKey.SetValue(ValueName, expectedValue, RegistryValueKind.String);
                }
            }
        }

        private static string Quote(string value)
        {
            return "\"" + value + "\"";
        }
    }
}
