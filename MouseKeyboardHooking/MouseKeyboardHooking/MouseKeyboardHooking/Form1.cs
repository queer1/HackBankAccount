using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MouseKeyboardHooking
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        public void updateMouse(string value) {
            labelMouse.Text = "Mouse Click: " + value;
        }

        public void updateKey(string value)
        {
            labelKey.Text = "Key Press: " + value;
        }
    }
}
