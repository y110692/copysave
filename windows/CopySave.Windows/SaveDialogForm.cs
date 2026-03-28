using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CopySave.Windows
{
    internal sealed class SaveDialogForm : Form
    {
        private readonly IntPtr previousForegroundWindow;
        private readonly TextBox fileNameTextBox;
        private readonly TextBox extensionTextBox;
        private readonly Button okButton;
        private readonly Timer focusRetryTimer;
        private int focusRetryCount;

        public SaveDialogForm(IntPtr previousForegroundWindow)
        {
            this.previousForegroundWindow = previousForegroundWindow;
            Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            BackColor = Color.Fuchsia;
            TransparencyKey = Color.Fuchsia;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            ShowInTaskbar = false;
            TopMost = true;
            Width = 360;
            Height = 232;
            KeyPreview = true;

            var surface = new RoundedSurface
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(18, 16, 18, 16),
                FillColor = Color.FromArgb(255, 12, 12, 12),
                BorderColor = Color.Transparent,
                BorderThickness = 0,
                CornerRadius = 20
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                BackColor = Color.Transparent
            };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var nameLabel = BuildLabel("name");
            var nameFieldHost = BuildFieldHost(out fileNameTextBox, "clipboard");
            var extensionLabel = BuildLabel("extension");
            var extensionFieldHost = BuildFieldHost(out extensionTextBox, "txt");
            extensionFieldHost.Margin = new Padding(0, 0, 0, 12);

            okButton = new Button
            {
                Text = "ok",
                AutoSize = true,
                Anchor = AnchorStyles.Right,
                BackColor = Color.FromArgb(255, 255, 255),
                ForeColor = Color.FromArgb(255, 0, 0, 0),
                FlatStyle = FlatStyle.Flat,
                Padding = new Padding(16, 7, 16, 7),
                Margin = new Padding(0),
                UseVisualStyleBackColor = false,
                TabStop = false
            };
            okButton.FlatAppearance.BorderSize = 0;
            okButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(212, 212, 212);
            okButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(228, 228, 228);
            okButton.Click += Submit;

            var buttonHost = new Panel
            {
                Dock = DockStyle.Fill,
                Height = 42,
                BackColor = Color.Transparent
            };
            okButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            buttonHost.Controls.Add(okButton);
            buttonHost.Resize += delegate
            {
                okButton.Left = buttonHost.ClientSize.Width - okButton.Width;
                okButton.Top = 0;
            };

            layout.Controls.Add(nameLabel, 0, 0);
            layout.Controls.Add(nameFieldHost, 0, 1);
            layout.Controls.Add(extensionLabel, 0, 2);
            layout.Controls.Add(extensionFieldHost, 0, 3);
            layout.Controls.Add(buttonHost, 0, 4);

            surface.Controls.Add(layout);
            Controls.Add(surface);

            focusRetryTimer = new Timer();
            focusRetryTimer.Interval = 90;
            focusRetryTimer.Tick += OnFocusRetryTick;

            AcceptButton = okButton;
            ActiveControl = fileNameTextBox;
            Shown += OnShown;
            Activated += OnActivated;
            FormClosed += OnFormClosed;
            KeyDown += OnKeyDown;
        }

        public string FileNameValue
        {
            get { return FileNameHelper.SanitizeName(fileNameTextBox.Text); }
        }

        public string ExtensionValue
        {
            get { return FileNameHelper.SanitizeExtension(extensionTextBox.Text); }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                const int WS_EX_TOOLWINDOW = 0x00000080;
                var createParams = base.CreateParams;
                createParams.ExStyle |= WS_EX_TOOLWINDOW;
                return createParams;
            }
        }

        private static Label BuildLabel(string text)
        {
            return new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                AutoSize = true,
                ForeColor = Color.FromArgb(255, 196, 196, 196),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point),
                Padding = new Padding(0, 0, 0, 6),
                Margin = new Padding(0)
            };
        }

        private static Panel BuildFieldHost(out TextBox textBox, string text)
        {
            var host = new Panel
            {
                Dock = DockStyle.Top,
                Height = 36,
                Padding = new Padding(12, 9, 12, 9),
                Margin = new Padding(0, 0, 0, 12),
                BackColor = Color.FromArgb(255, 28, 28, 28)
            };

            textBox = new TextBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                Text = text,
                Margin = new Padding(0),
                BackColor = Color.FromArgb(255, 28, 28, 28),
                ForeColor = Color.FromArgb(255, 240, 240, 240)
            };
            host.Controls.Add(textBox);
            return host;
        }

        private void OnShown(object sender, EventArgs eventArgs)
        {
            focusRetryCount = 0;
            ForceForegroundAndFocus();
            focusRetryTimer.Start();
        }

        private void OnActivated(object sender, EventArgs eventArgs)
        {
            ForceForegroundAndFocus();
        }

        private void OnFormClosed(object sender, FormClosedEventArgs eventArgs)
        {
            focusRetryTimer.Stop();
            focusRetryTimer.Dispose();
        }

        private void OnFocusRetryTick(object sender, EventArgs eventArgs)
        {
            focusRetryCount += 1;
            ForceForegroundAndFocus();

            if (ContainsFocus || focusRetryCount >= 4)
            {
                focusRetryTimer.Stop();
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs eventArgs)
        {
            if (eventArgs.KeyCode == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                Close();
            }
        }

        private void Submit(object sender, EventArgs eventArgs)
        {
            if (string.IsNullOrWhiteSpace(FileNameValue) || string.IsNullOrWhiteSpace(ExtensionValue))
            {
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        private void FocusNameField()
        {
            ActiveControl = fileNameTextBox;
            fileNameTextBox.Focus();
            if (fileNameTextBox.IsHandleCreated)
            {
                NativeMethods.SetFocus(fileNameTextBox.Handle);
            }
            fileNameTextBox.SelectAll();
        }

        private void ForceForegroundAndFocus()
        {
            if (!IsHandleCreated)
            {
                return;
            }

            var sourceWindow = previousForegroundWindow != IntPtr.Zero
                ? previousForegroundWindow
                : NativeMethods.GetForegroundWindow();

            uint sourceProcessId;
            var sourceThreadId = sourceWindow != IntPtr.Zero
                ? NativeMethods.GetWindowThreadProcessId(sourceWindow, out sourceProcessId)
                : 0;
            var currentThreadId = NativeMethods.GetCurrentThreadId();
            var attached = false;

            try
            {
                if (sourceThreadId != 0 && sourceThreadId != currentThreadId)
                {
                    attached = NativeMethods.AttachThreadInput(sourceThreadId, currentThreadId, true);
                }

                NativeMethods.ShowWindow(Handle, NativeMethods.SW_SHOWNORMAL);
                NativeMethods.SetWindowPos(
                    Handle,
                    NativeMethods.HWND_TOPMOST,
                    0,
                    0,
                    0,
                    0,
                    NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_SHOWWINDOW);
                NativeMethods.BringWindowToTop(Handle);
                NativeMethods.SetForegroundWindow(Handle);
                NativeMethods.SetActiveWindow(Handle);
                Activate();
                FocusNameField();
            }
            finally
            {
                if (attached)
                {
                    NativeMethods.AttachThreadInput(sourceThreadId, currentThreadId, false);
                }
            }
        }

        private sealed class RoundedSurface : Panel
        {
            public int CornerRadius { get; set; }

            public int BorderThickness { get; set; }

            public Color FillColor { get; set; }

            public Color BorderColor { get; set; }

            public RoundedSurface()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
                BackColor = Color.Transparent;
                CornerRadius = 20;
                BorderThickness = 0;
            }

            protected override void OnPaint(PaintEventArgs eventArgs)
            {
                eventArgs.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var path = BuildPath(ClientRectangle, CornerRadius))
                using (var fillBrush = new SolidBrush(FillColor))
                {
                    eventArgs.Graphics.FillPath(fillBrush, path);
                    if (BorderThickness > 0)
                    {
                        using (var borderPen = new Pen(BorderColor, BorderThickness))
                        {
                            eventArgs.Graphics.DrawPath(borderPen, path);
                        }
                    }
                }
            }

            protected override void OnResize(EventArgs eventArgs)
            {
                base.OnResize(eventArgs);
                using (var path = BuildPath(ClientRectangle, CornerRadius))
                {
                    Region = new Region(path);
                }
            }

            private static GraphicsPath BuildPath(Rectangle bounds, int radius)
            {
                var rectangle = new Rectangle(bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
                var diameter = radius * 2;
                var path = new GraphicsPath();

                path.AddArc(rectangle.X, rectangle.Y, diameter, diameter, 180, 90);
                path.AddArc(rectangle.Right - diameter, rectangle.Y, diameter, diameter, 270, 90);
                path.AddArc(rectangle.Right - diameter, rectangle.Bottom - diameter, diameter, diameter, 0, 90);
                path.AddArc(rectangle.X, rectangle.Bottom - diameter, diameter, diameter, 90, 90);
                path.CloseFigure();
                return path;
            }
        }
    }
}
