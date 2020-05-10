using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CoreTimer
{
    public class BorderLessMoveFormWithMouse : Form
    {

        protected MouseButtons MoveMouseButton = MouseButtons.Left;
        protected MouseButtons MenuMouseButton = MouseButtons.Right;
        readonly ContextMenuStrip _menuStrip = new ContextMenuStrip();
        private bool _mouseButtonDown;
        private readonly Timer _hackDontEvenAsk = new Timer {Interval = 33, Enabled = false};

        private Point _startPoint;
        private bool _alwaysOnTop = false;

        private void AlwaysOnTopToggle()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }
            var alwaysOnTopButton = new ToolStripButton{Text = "Always On Top"};
            alwaysOnTopButton.Click+= (sender, args) =>  AlwaysOnTopToggle();
            _menuStrip.Items.Add(alwaysOnTopButton);
            SetWindowPosHelper. SetWindowPos(this.Handle, _alwaysOnTop ? SetWindowPosHelper.HWND_NOTOPMOST :SetWindowPosHelper. HWND_TOPMOST, 0, 0, 0, 0, SetWindowPosHelper.TOPMOST_FLAGS);

            _alwaysOnTop = !_alwaysOnTop;
        }
        
        protected BorderLessMoveFormWithMouse()
        {
            this.ShowInTaskbar = true;
       
            // this is a hack as if i set formborder style to non in constructor the size won't stay the same ...
            // shrug no idea why caught me out on debugging for 10 mins
            _hackDontEvenAsk.Enabled = true;
            _hackDontEvenAsk.Tick += (sender, args) =>
            {
                this.FormBorderStyle = FormBorderStyle.None;
                _hackDontEvenAsk.Enabled = false;
            };

  
            var exitButton = new ToolStripButton{Text = "Exit"};
            exitButton.Click += (sender, args) => Application.Exit();
            

            _menuStrip.Width = 555;
            AlwaysOnTopToggle();
            _menuStrip.Items.Add(exitButton);
      
            this.ContextMenuStrip = _menuStrip;

        }


        protected void MouseDown(object ob, MouseEventArgs e)
        {
            if (e.Button == MenuMouseButton)
            {
                _menuStrip.Location = this.Location;
                _menuStrip.Show();
            }

            if (e.Button == MoveMouseButton)
            {
                _mouseButtonDown = true;
                _startPoint = e.Location;
            }
        }

        protected void MouseUp(object ob, MouseEventArgs e)
        {
            if (e.Button == MoveMouseButton)
            {
                _mouseButtonDown = false;
            }
        }

        private void MarkUsMovableItems(Control item)
        {
            if (item == _menuStrip)
            {
                return;
            }

            item.MouseDown += MouseDown;
            item.MouseMove += MouseMove;
            item.MouseUp += MouseUp;

            foreach (var t in item.Controls)
            {
                if (t is Control control)
                {
                    MarkUsMovableItems(control);
                }
            }
        }

        protected void InitMoveWindow()
        {
            foreach (var item in Controls)
            {
                MarkUsMovableItems((Control) item);
            }
        }

        protected void MouseMove(object ob, MouseEventArgs e)
        {
            if (_mouseButtonDown)
            {
                this.Location = new Point(this.Location.X - (_startPoint.X - e.Location.X), this.Location.Y - (_startPoint.Y - e.Location.Y));
            }
        }
    }
}