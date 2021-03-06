using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CoreTimer
{
    public sealed class Program : BorderLessMoveFormWithMouse
    {
        private const string StopSymbol = "■";
        private const string PlaySymbol = "»";
        private const string PauseSymbol = "||";
        private const int SetupWidthConst = 22;
        private static readonly Size FormDefaultSize = new Size(100, 100);
        private readonly bool _isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        private bool _countDown = true;
        private bool _pause;

        private bool _alwaysOnTop; // this will get set to true in constructor after its setup
        private TimeSpan _requestedTimeSpan;
        private TimeSpan _soFarTimeSpan;

        // Form items
        private readonly IContainer components = new Container();
        private readonly Timer _secondTimer = new Timer {Interval = 1000, Enabled = false};
        private readonly Timer _milisecondTimer = new Timer {Interval = 300, Enabled = false};
        private readonly ContextMenuStrip _menuStrip = new ContextMenuStrip {AutoSize = false, Width = 130};
        private readonly NotifyIcon _notifyIcon;

        // Setup Panel Items
        private readonly Panel _setupPanel = new Panel();
        private readonly Label _setupLabelHour = new Label {Text = "HH", AutoSize = false, Width = SetupWidthConst};
        private readonly Label _setupLabelMin = new Label {Text = "MM", AutoSize = false, Width = SetupWidthConst};
        private readonly Label _setupLabelSec = new Label {Text = "SS", AutoSize = false, Width = SetupWidthConst};
        private readonly TextBox _setupHourTextBox = new TextBox {MaxLength = 2, Width = SetupWidthConst, Location = new Point(5, 15)};
        private readonly TextBox _setupMinTextBox = new TextBox {MaxLength = 2, Width = SetupWidthConst,};
        private readonly TextBox _setupSecTextBox = new TextBox {MaxLength = 2, Width = SetupWidthConst,};
        private readonly Button _setupStartBtn = new Button {Text = "Start", Height = 20, AutoSize = false};

        // Display Panel Items
        private readonly Panel _displayPanel = new Panel {Visible = false};

        private readonly Label _displayCountDownUpLabelMain =
            new Label {Left = 1, Font = new Font(FontFamily.GenericMonospace, 17), AutoSize = false, Text = ""};

        private readonly Label _displayCountDownUpLabelSec = new Label {Font = new Font(FontFamily.GenericMonospace, 10), AutoSize = true, Text = ""};
        private readonly Label _displayStopBtn = new Label {Text = StopSymbol, BorderStyle = BorderStyle.FixedSingle};
        private readonly Label _displayPausePlayBtn = new Label {Text = PauseSymbol, BorderStyle = BorderStyle.FixedSingle};


        private Program() : base(FormDefaultSize)
        {
            _notifyIcon = new System.Windows.Forms.NotifyIcon(this.components) {Text = "MkbMain ExampleTimer", Visible = true};
            _secondTimer.Tick += SecondTick;
            _milisecondTimer.Tick += (sender, args) => Console.Beep(); // BEEPER 
            AutoSize = false;
            Text = "MkbMain ExampleTimer";
            SecondaryActionOccurance += OnSecondaryActionOccurance;
            InitIcon();
            InitMenu();
            InitSetupPanel();
            InitDisplayPanel();
            InitMoveWindow();
        }


        private void InitMenu()
        {
            var exitButton = new ToolStripButton {Text = "Exit"};
            exitButton.Click += (sender, args) => Application.Exit();

            ContextMenuStrip = _menuStrip;

            var countDown = new ToolStripButton {Text = "Count Up"};
            countDown.Click += (sender, args) =>
            {
                _countDown = !_countDown;
                var btn = sender as ToolStripButton;
                btn.Text = _countDown ? "Count Up " : "Count Down";
            };

            if (_isWindows)
            {
                AlwaysOnTopToggle();
                var alwaysOnTopButton = new ToolStripButton {Text = "Always On Top"};
                alwaysOnTopButton.Click += (sender, args) => AlwaysOnTopToggle();
                _menuStrip.Items.Add(alwaysOnTopButton);
            }

            var showInTaskBad = new ToolStripButton {Text = "Show In Task bar"};
            showInTaskBad.Click += (sender, args) =>
            {
                this.ShowInTaskbar = !this.ShowInTaskbar;
                SetupFormSize(FormDefaultSize);
            };
            _menuStrip.Items.Add(showInTaskBad);
            _menuStrip.Items.Add(countDown);

            _menuStrip.Items.Add(exitButton);
            _notifyIcon.ContextMenuStrip = ContextMenuStrip;
            _menuStrip.Height = 10 + (_menuStrip.Items.Count * 20);
        }

        private void InitIcon()
        {
            var path = $"{Environment.CurrentDirectory}{(_isWindows ? "\\icon.ico" : "/icon.ico")}";
            if (System.IO.File.Exists(path))
            {
                var icon = Icon.ExtractAssociatedIcon(path);
                _notifyIcon.Icon = icon;
                this.Icon = icon;
            }
        }

        private void OnSecondaryActionOccurance(object sender, MouseEventArgs eventargs)
        {
            _menuStrip.Location = Location;
            _menuStrip.Show();
        }

        private static string FormatInt(int item)
        {
            var output = item.ToString();
            return output.Length < 2 ? $"0{output}" : output;
        }

        private void SecondTick(object _, EventArgs args)
        {
            if (_pause)
            {
                return;
            }

            _soFarTimeSpan = _soFarTimeSpan.Add(new TimeSpan(0, 0, 1));
            var timeSpan = _countDown ? (_requestedTimeSpan - _soFarTimeSpan) : _soFarTimeSpan;

            _displayCountDownUpLabelMain.Text = $"{FormatInt((int) timeSpan.TotalHours)}:{FormatInt(timeSpan.Minutes)}";
            _displayCountDownUpLabelSec.Text = FormatInt(timeSpan.Seconds);

            Text = timeSpan.ToString();

            if (_soFarTimeSpan >= _requestedTimeSpan)
            {
                _milisecondTimer.Enabled = true;
            }
        }

        private void SetupStartBtnOnClick(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_setupHourTextBox.Text))
            {
                _setupHourTextBox.Text = "0";
            }

            if (string.IsNullOrWhiteSpace(_setupMinTextBox.Text))
            {
                _setupMinTextBox.Text = "0";
            }

            if (string.IsNullOrWhiteSpace(_setupSecTextBox.Text))
            {
                _setupSecTextBox.Text = "0";
            }

            _soFarTimeSpan = new TimeSpan();

            if (!int.TryParse(_setupHourTextBox.Text, out var hour) || !int.TryParse(_setupMinTextBox.Text, out var min) ||
                !int.TryParse(_setupSecTextBox.Text, out var sec))
            {
                ErrorMessage();
                return;
            }


            _requestedTimeSpan = new TimeSpan(hour, min, sec);
            if (_requestedTimeSpan < new TimeSpan(0, 0, 1))
            {
                ErrorMessage();
                return;
            }

            _setupPanel.Visible = false;
            _displayPanel.Visible = true;
            _secondTimer.Enabled = true;
        }

        private static void ErrorMessage()
        {
            MessageBox.Show("Error Please Check values in text box they are not numbers", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void InitDisplayPanel()
        {
            _displayPanel.Size = FormDefaultSize;

            _displayCountDownUpLabelMain.Width = _displayPanel.Width;
            _displayCountDownUpLabelMain.TextAlign = ContentAlignment.MiddleCenter;
            _displayCountDownUpLabelSec.Top = _displayCountDownUpLabelMain.Bottom;
            _displayCountDownUpLabelSec.Left = 33;

            _displayPanel.Controls.Add(_displayCountDownUpLabelMain);
            _displayPanel.Controls.Add(_displayCountDownUpLabelSec);
            _displayStopBtn.Location = _setupStartBtn.Location;
            _displayStopBtn.Height = _setupStartBtn.Height;
            _displayStopBtn.Width = (_setupStartBtn.Width / 2) - 5;
            _displayPausePlayBtn.Size = _displayStopBtn.Size;
            _displayPausePlayBtn.Location = new Point(_displayStopBtn.Right + 5, _displayStopBtn.Location.Y);


            this.Controls.Add(_displayPanel);
            _displayPanel.Controls.Add(_displayStopBtn);
            _displayPanel.Controls.Add(_displayPausePlayBtn);

            _displayPausePlayBtn.Click += (sender, args) =>
            {
                _pause = !_pause;
                _displayPausePlayBtn.Text = _pause ? PlaySymbol : PauseSymbol;
            };
            _displayStopBtn.Click += (sender, args) =>
            {
                _secondTimer.Enabled = false;
                _milisecondTimer.Enabled = false;
                _displayPanel.Visible = false;
                _setupPanel.Visible = true;
            };
        }

        private void InitSetupPanel()
        {
            _setupMinTextBox.Location = new Point(_setupHourTextBox.Location.X + _setupHourTextBox.Width + 3, _setupHourTextBox.Location.Y);
            _setupSecTextBox.Location = new Point(_setupMinTextBox.Location.X + _setupMinTextBox.Width + 3, _setupMinTextBox.Location.Y);
            _setupLabelHour.Location = new Point(_setupHourTextBox.Location.X, 1);
            _setupLabelMin.Location = new Point(_setupMinTextBox.Location.X, 1);
            _setupLabelSec.Location = new Point(_setupSecTextBox.Location.X, 1);
            _setupStartBtn.Location = new Point(_setupHourTextBox.Location.X, _setupHourTextBox.Bottom + 3);
            _setupStartBtn.Width = _setupSecTextBox.Right - _setupHourTextBox.Left;
            _setupPanel.Width = Width;
            _setupPanel.Height = Height;

            _setupPanel.Controls.Add(_setupHourTextBox);
            _setupPanel.Controls.Add(_setupMinTextBox);
            _setupPanel.Controls.Add(_setupSecTextBox);
            _setupPanel.Controls.Add(_setupLabelHour);
            _setupPanel.Controls.Add(_setupLabelMin);
            _setupPanel.Controls.Add(_setupLabelSec);
            _setupPanel.Controls.Add(_setupStartBtn);
            this.Controls.Add(_setupPanel);

            _setupStartBtn.Click += SetupStartBtnOnClick;
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                components?.Dispose();
            }

            base.Dispose(disposing);
        }


        private void AlwaysOnTopToggle()
        {
            SetWindowPosHelper.SetWindowPos(Handle, _alwaysOnTop ? SetWindowPosHelper.HWND_NOTOPMOST : SetWindowPosHelper.HWND_TOPMOST, 0, 0, 0, 0,
                SetWindowPosHelper.TOPMOST_FLAGS);
            _alwaysOnTop = !_alwaysOnTop;
        }


        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Program());
        }
    }
}