using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CoreTimer
{
    public class BorderLessMoveFormWithMouse : Form
    {
        protected delegate void SecondaryAction(object sender, MouseEventArgs eventArgs);

        protected event SecondaryAction SecondaryActionOccurance;
        protected MouseButtons MoveMouseButton = MouseButtons.Left;
        protected MouseButtons SecondaryActionMouseButton = MouseButtons.Right;
        private bool _mouseButtonDown;
        private Point _startPoint;
        
        protected BorderLessMoveFormWithMouse()
        {
            this.ShowInTaskbar = true;

            // this is a hack as if i set formborder style to non in constructor the size won't stay the same ...
            // shrug no idea why caught me out on debugging for 10 mins
            var timer = new Timer {Interval = 33, Enabled = false};
            timer.Enabled = true;
            timer.Tick += (sender, args) =>
            {
                this.FormBorderStyle = FormBorderStyle.None;
                timer.Enabled = false;
                timer.Dispose();
            };
        }

        protected new void MouseDown(object ob, MouseEventArgs e)
        {
            if (e.Button == SecondaryActionMouseButton)
            {
                SecondaryActionOccurance?.Invoke(ob, e);
            }

            if (e.Button == MoveMouseButton)
            {
                _mouseButtonDown = true;
                _startPoint = e.Location;
            }
        }

        protected new void MouseUp(object ob, MouseEventArgs e)
        {
            if (e.Button == MoveMouseButton)
            {
                _mouseButtonDown = false;
            }
        }
        
        protected new void MouseMove(object ob, MouseEventArgs e)
        {
            if (_mouseButtonDown)
            {
                this.Location = new Point(this.Location.X - (_startPoint.X - e.Location.X), this.Location.Y - (_startPoint.Y - e.Location.Y));
            }
        }

        private void MarkUsMovableItems(Control item)
        {
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
        
    }
}