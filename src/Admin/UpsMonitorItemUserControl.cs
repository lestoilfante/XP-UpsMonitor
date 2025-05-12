using System;
using System.Windows.Forms;
using UpsMonitor.Common;
using VideoOS.Platform;

namespace UpsMonitor.Admin
{
    public partial class UpsMonitorItemUserControl : UserControl
    {
        internal event EventHandler ConfigurationChangedByUser;

        const string _pollHelp = "Intervals at which Event Server will perform UPS polling.\r\n" +
            "Minimum value is 30 seconds.\r\n\r\n" +
            "This value is system-wide and not specific to any individual unit.\r\n" +
            "Any change requires Event Server restart.";

        public UpsMonitorItemUserControl()
        {
            InitializeComponent();
            numericPollIntval.Minimum = UpsMonitor.Common.UpsMonitor.MinPollInterval;
            numericPollIntval.Maximum = UpsMonitor.Common.UpsMonitor.MaxPollInterval;
        }

        internal String DisplayName
        {
            get { return textBoxName.Text; }
            set { textBoxName.Text = value; }
        }

        internal int PollInterval
        {
            get { return (int)numericPollIntval.Value; }
            set { numericPollIntval.Value = value; }
        }

        internal void OnUserChange(object sender, EventArgs e)
        {
            ConfigurationChangedByUser?.Invoke(this, new EventArgs());
        }

        internal void FillContent(Item item)
        {
            if (item is UpsItem upsItem)
            {
                textBoxName.Text = upsItem.Name;
                checkBoxEnabled.Checked = upsItem.Enabled;
                textBoxIP.Text = upsItem.Host;
                textBoxSNMPcommunity.Text = upsItem.Community;
                comboMib.DataSource = UpsMonitorHelper.MibFamilyList;
                comboMib.DisplayMember = "Value";
                comboMib.ValueMember = "Key";
                comboMib.SelectedIndex = (int)upsItem.MibFamily;

                numericPollIntval.Value = UpsMonitorHelper.LoadPollInterval();
                textBoxPollHelp.Text = _pollHelp;
            }
        }

        internal void UpdateItem(Item item)
        {
            if (item is UpsItem upsItem)
            {
                upsItem.Name = DisplayName;
                upsItem.Host = textBoxIP.Text.Trim();
                string snmpCommunity = textBoxSNMPcommunity.Text.Trim();
                upsItem.Community = (String.IsNullOrEmpty(snmpCommunity) || String.IsNullOrWhiteSpace(snmpCommunity)) ? "public" : snmpCommunity;
                upsItem.Enabled = checkBoxEnabled.Checked;
                upsItem.MibFamily = (MibFamily)comboMib.SelectedValue;
            }
        }

        internal void ClearContent()
        {
            textBoxName.Text = String.Empty;
            textBoxIP.Text = String.Empty;
            textBoxSNMPcommunity.Text = String.Empty;
            checkBoxEnabled.Checked = false;
        }
    }
}
