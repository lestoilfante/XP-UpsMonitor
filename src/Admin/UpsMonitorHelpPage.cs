using System;
using UpsMonitor.Common;
using VideoOS.Platform.Admin;

namespace UpsMonitor.Admin
{
    public partial class HelpPage : ItemNodeUserControl
    {
        public HelpPage()
        {
            InitializeComponent();
            if (!SiteLicenseHandler.IsValidLicense)
            {
                textBox1.Text += Environment.NewLine + Environment.NewLine + SiteLicenseHandler.KindLycenseMe();
                textBox1.Text += Environment.NewLine + UpsMonitor.Common.UpsMonitor.ManufacturerName;
            }
        }
    }
}
