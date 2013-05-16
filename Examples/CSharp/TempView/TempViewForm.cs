using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Globalization;
using MeasurementComputing.DAQFlex;

namespace TempView
{
    public partial class TempViewForm : Form
    {
        public TempViewForm()
        {
            InitializeComponent();
            this.Text = "TempView";
        }
    }
}
