using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace CopySave.Windows
{
    internal sealed class ApplicationContextHost : ApplicationContext
    {
        private readonly NotifyIcon trayIcon;
        private readonly KeyboardInterceptor keyboardInterceptor;
        private readonly Control invoker;
        private bool handlingPaste;

        public ApplicationContextHost()
        {
            StartupManager.EnsureCurrentUserStartup();

            invoker = new Control();
            invoker.CreateControl();

            trayIcon = new NotifyIcon
            {
                Text = "CopySave",
                Icon = LoadTrayIcon(),
                Visible = true,
                ContextMenuStrip = BuildTrayMenu()
            };

            keyboardInterceptor = new KeyboardInterceptor();
            keyboardInterceptor.PasteIntercepted += OnPasteIntercepted;
            keyboardInterceptor.Start();
        }

        private ContextMenuStrip BuildTrayMenu()
        {
            var menu = new ContextMenuStrip();
            menu.Items.Add("Exit", null, OnExitClicked);
            return menu;
        }

        private void OnExitClicked(object sender, EventArgs eventArgs)
        {
            ExitThread();
        }

        private void OnPasteIntercepted(object sender, EventArgs eventArgs)
        {
            if (handlingPaste || invoker.IsDisposed)
            {
                return;
            }

            invoker.BeginInvoke((MethodInvoker)HandlePasteIntercepted);
        }

        private void HandlePasteIntercepted()
        {
            if (handlingPaste)
            {
                return;
            }

            handlingPaste = true;

            try
            {
                string folderPath;
                if (!ExplorerContextResolver.TryGetFrontFolderPath(out folderPath))
                {
                    return;
                }

                string payload;
                if (!ClipboardPayloadReader.TryReadPayload(out payload))
                {
                    return;
                }

                var foregroundWindow = NativeMethods.GetForegroundWindow();
                using (var dialog = new SaveDialogForm(foregroundWindow))
                {
                    if (dialog.ShowDialog() != DialogResult.OK)
                    {
                        return;
                    }

                    var targetPath = FileNameHelper.BuildUniqueFilePath(folderPath, dialog.FileNameValue, dialog.ExtensionValue);
                    File.WriteAllText(targetPath, payload, new UTF8Encoding(false));
                    ShowBalloon("Saved as " + Path.GetFileName(targetPath), ToolTipIcon.Info);
                }
            }
            catch (Exception exception)
            {
                ShowBalloon(exception.Message, ToolTipIcon.Error);
            }
            finally
            {
                handlingPaste = false;
            }
        }

        private void ShowBalloon(string text, ToolTipIcon icon)
        {
            trayIcon.BalloonTipTitle = "CopySave";
            trayIcon.BalloonTipText = text;
            trayIcon.BalloonTipIcon = icon;
            trayIcon.ShowBalloonTip(2000);
        }

        private static Icon LoadTrayIcon()
        {
            var icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            return icon ?? SystemIcons.Application;
        }

        protected override void ExitThreadCore()
        {
            keyboardInterceptor.PasteIntercepted -= OnPasteIntercepted;
            keyboardInterceptor.Dispose();
            trayIcon.Visible = false;
            trayIcon.Dispose();
            invoker.Dispose();
            base.ExitThreadCore();
        }
    }
}
