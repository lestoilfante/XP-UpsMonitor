namespace UpsMonitor.Admin
{
	partial class UpsMonitorItemUserControl
	{
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this.backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label6 = new System.Windows.Forms.Label();
            this.comboMib = new System.Windows.Forms.ComboBox();
            this.textBoxSNMPcommunity = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.textBoxIP = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.checkBoxEnabled = new System.Windows.Forms.CheckBox();
            this.textBoxName = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.textBoxPollHelp = new System.Windows.Forms.TextBox();
            this.numericPollIntval = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            this.backgroundWorker2 = new System.ComponentModel.BackgroundWorker();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericPollIntval)).BeginInit();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label6);
            this.groupBox1.Controls.Add(this.comboMib);
            this.groupBox1.Controls.Add(this.textBoxSNMPcommunity);
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Controls.Add(this.textBoxIP);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.checkBoxEnabled);
            this.groupBox1.Controls.Add(this.textBoxName);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Location = new System.Drawing.Point(16, 10);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(372, 154);
            this.groupBox1.TabIndex = 7;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "UPS unit";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(16, 121);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(63, 13);
            this.label6.TabIndex = 15;
            this.label6.Text = "SNMP MIB:";
            // 
            // comboMib
            // 
            this.comboMib.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboMib.FormattingEnabled = true;
            this.comboMib.Location = new System.Drawing.Point(112, 118);
            this.comboMib.Name = "comboMib";
            this.comboMib.Size = new System.Drawing.Size(254, 21);
            this.comboMib.TabIndex = 14;
            this.comboMib.SelectedIndexChanged += new System.EventHandler(this.OnUserChange);
            // 
            // textBoxSNMPcommunity
            // 
            this.textBoxSNMPcommunity.Location = new System.Drawing.Point(112, 92);
            this.textBoxSNMPcommunity.Name = "textBoxSNMPcommunity";
            this.textBoxSNMPcommunity.Size = new System.Drawing.Size(254, 20);
            this.textBoxSNMPcommunity.TabIndex = 13;
            this.textBoxSNMPcommunity.TextChanged += new System.EventHandler(this.OnUserChange);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(16, 95);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(95, 13);
            this.label5.TabIndex = 12;
            this.label5.Text = "SNMP Community:";
            // 
            // textBoxIP
            // 
            this.textBoxIP.Location = new System.Drawing.Point(112, 68);
            this.textBoxIP.Name = "textBoxIP";
            this.textBoxIP.Size = new System.Drawing.Size(254, 20);
            this.textBoxIP.TabIndex = 11;
            this.textBoxIP.TextChanged += new System.EventHandler(this.OnUserChange);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(16, 71);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(61, 13);
            this.label4.TabIndex = 10;
            this.label4.Text = "IP Address:";
            // 
            // checkBoxEnabled
            // 
            this.checkBoxEnabled.AutoSize = true;
            this.checkBoxEnabled.Location = new System.Drawing.Point(19, 19);
            this.checkBoxEnabled.Name = "checkBoxEnabled";
            this.checkBoxEnabled.Size = new System.Drawing.Size(65, 17);
            this.checkBoxEnabled.TabIndex = 9;
            this.checkBoxEnabled.Text = "Enabled";
            this.checkBoxEnabled.UseVisualStyleBackColor = true;
            this.checkBoxEnabled.CheckedChanged += new System.EventHandler(this.OnUserChange);
            // 
            // textBoxName
            // 
            this.textBoxName.Location = new System.Drawing.Point(112, 44);
            this.textBoxName.Name = "textBoxName";
            this.textBoxName.Size = new System.Drawing.Size(254, 20);
            this.textBoxName.TabIndex = 10;
            this.textBoxName.TextChanged += new System.EventHandler(this.OnUserChange);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(16, 47);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(38, 13);
            this.label1.TabIndex = 7;
            this.label1.Text = "Name:";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.textBoxPollHelp);
            this.groupBox2.Controls.Add(this.numericPollIntval);
            this.groupBox2.Controls.Add(this.label2);
            this.groupBox2.Location = new System.Drawing.Point(16, 170);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(372, 147);
            this.groupBox2.TabIndex = 8;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "System wide";
            // 
            // textBoxPollHelp
            // 
            this.textBoxPollHelp.Enabled = false;
            this.textBoxPollHelp.Location = new System.Drawing.Point(19, 56);
            this.textBoxPollHelp.Multiline = true;
            this.textBoxPollHelp.Name = "textBoxPollHelp";
            this.textBoxPollHelp.ReadOnly = true;
            this.textBoxPollHelp.Size = new System.Drawing.Size(347, 72);
            this.textBoxPollHelp.TabIndex = 18;
            // 
            // numericPollIntval
            // 
            this.numericPollIntval.Location = new System.Drawing.Point(112, 27);
            this.numericPollIntval.Minimum = new decimal(new int[] {
            60,
            0,
            0,
            0});
            this.numericPollIntval.Name = "numericPollIntval";
            this.numericPollIntval.Size = new System.Drawing.Size(254, 20);
            this.numericPollIntval.TabIndex = 17;
            this.numericPollIntval.Value = new decimal(new int[] {
            60,
            0,
            0,
            0});
            this.numericPollIntval.ValueChanged += new System.EventHandler(this.OnUserChange);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(15, 29);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(64, 13);
            this.label2.TabIndex = 16;
            this.label2.Text = "Poll interval:";
            // 
            // UpsMonitorItemUserControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.WhiteSmoke;
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Name = "UpsMonitorItemUserControl";
            this.Size = new System.Drawing.Size(420, 320);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericPollIntval)).EndInit();
            this.ResumeLayout(false);

		}

		#endregion
        private System.ComponentModel.BackgroundWorker backgroundWorker1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.CheckBox checkBoxEnabled;
        private System.Windows.Forms.TextBox textBoxName;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBoxSNMPcommunity;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox textBoxIP;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ComboBox comboMib;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label label2;
        private System.ComponentModel.BackgroundWorker backgroundWorker2;
        private System.Windows.Forms.TextBox textBoxPollHelp;
        private System.Windows.Forms.NumericUpDown numericPollIntval;
    }
}
