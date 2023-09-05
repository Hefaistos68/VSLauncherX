namespace VSLauncher
{
	partial class dlgExecuteVisualStudio
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

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			Label label1;
			Label label2;
			Label label3;
			Label label4;
			Label label5;
			Label label6;
			btnCancel = new Button();
			btnOk = new Button();
			txtCommand = new TextBox();
			this.cbxVisualStudioVersion = new VisualStudioCombobox();
			cbxInstance = new ComboBox();
			chkAdmin = new CheckBox();
			chkSplash = new CheckBox();
			btnSelectFolder = new Button();
			txtFoldername = new TextBox();
			txtInfo = new LinkLabel();
			toolTip1 = new ToolTip(this.components);
			btnBeforeAfter = new Button();
			cbxMonitors = new ComboBox();
			txtInfoCommands = new LinkLabel();
			txtInfoInstances = new LinkLabel();
			btnPingMonitor = new Button();
			txtName = new TextBox();
			label1 = new Label();
			label2 = new Label();
			label3 = new Label();
			label4 = new Label();
			label5 = new Label();
			label6 = new Label();
			this.SuspendLayout();
			// 
			// label1
			// 
			label1.AutoSize = true;
			label1.Location = new Point(12, 181);
			label1.Name = "label1";
			label1.Size = new Size(64, 15);
			label1.TabIndex = 8;
			label1.Text = "Command";
			// 
			// label2
			// 
			label2.AutoSize = true;
			label2.Location = new Point(12, 60);
			label2.Name = "label2";
			label2.Size = new Size(48, 15);
			label2.TabIndex = 2;
			label2.Text = "Version:";
			// 
			// label3
			// 
			label3.AutoSize = true;
			label3.Location = new Point(12, 125);
			label3.Name = "label3";
			label3.Size = new Size(54, 15);
			label3.TabIndex = 5;
			label3.Text = "Instance:";
			// 
			// label4
			// 
			label4.AutoSize = true;
			label4.Location = new Point(12, 240);
			label4.Name = "label4";
			label4.Size = new Size(108, 15);
			label4.TabIndex = 11;
			label4.Text = "Project or Solution:";
			// 
			// label5
			// 
			label5.AutoSize = true;
			label5.Location = new Point(12, 298);
			label5.Name = "label5";
			label5.Size = new Size(101, 15);
			label5.TabIndex = 14;
			label5.Text = "Preferred Monitor";
			// 
			// btnCancel
			// 
			btnCancel.DialogResult = DialogResult.Cancel;
			btnCancel.Image = Resources.Cancel_24x24;
			btnCancel.Location = new Point(193, 399);
			btnCancel.Name = "btnCancel";
			btnCancel.Size = new Size(100, 40);
			btnCancel.TabIndex = 20;
			btnCancel.Text = " Cancel";
			btnCancel.TextImageRelation = TextImageRelation.ImageBeforeText;
			btnCancel.UseVisualStyleBackColor = true;
			// 
			// btnOk
			// 
			btnOk.DialogResult = DialogResult.OK;
			btnOk.Image = Resources.Check_24x24;
			btnOk.Location = new Point(299, 399);
			btnOk.Name = "btnOk";
			btnOk.Size = new Size(100, 40);
			btnOk.TabIndex = 21;
			btnOk.Tag = "&Save";
			btnOk.Text = "&Run";
			btnOk.TextImageRelation = TextImageRelation.ImageBeforeText;
			btnOk.UseVisualStyleBackColor = true;
			btnOk.Click += this.btnOk_Click;
			// 
			// txtCommand
			// 
			txtCommand.Location = new Point(12, 199);
			txtCommand.Name = "txtCommand";
			txtCommand.Size = new Size(387, 23);
			txtCommand.TabIndex = 9;
			txtCommand.TextChanged += this.txtInstanceName_TextChanged;
			// 
			// cbxVisualStudioVersion
			// 
			this.cbxVisualStudioVersion.DrawMode = DrawMode.OwnerDrawFixed;
			this.cbxVisualStudioVersion.DropDownStyle = ComboBoxStyle.DropDownList;
			this.cbxVisualStudioVersion.FormattingEnabled = true;
			this.cbxVisualStudioVersion.IntegralHeight = false;
			this.cbxVisualStudioVersion.ItemHeight = 26;
			this.cbxVisualStudioVersion.Location = new Point(12, 78);
			this.cbxVisualStudioVersion.Name = "cbxVisualStudioVersion";
			this.cbxVisualStudioVersion.SelectedItem = null;
			this.cbxVisualStudioVersion.Size = new Size(387, 32);
			this.cbxVisualStudioVersion.TabIndex = 3;
			this.cbxVisualStudioVersion.SelectedIndexChanged += this.cbxVisualStudioVersion_SelectedIndexChanged;
			this.cbxVisualStudioVersion.ShowDefault = true;
			// 
			// cbxInstance
			// 
			cbxInstance.FormattingEnabled = true;
			cbxInstance.Location = new Point(12, 143);
			cbxInstance.Name = "cbxInstance";
			cbxInstance.Size = new Size(387, 23);
			cbxInstance.TabIndex = 6;
			// 
			// chkAdmin
			// 
			chkAdmin.AutoSize = true;
			chkAdmin.Location = new Point(12, 325);
			chkAdmin.Name = "chkAdmin";
			chkAdmin.Size = new Size(100, 19);
			chkAdmin.TabIndex = 17;
			chkAdmin.Text = "Run as Admin";
			chkAdmin.UseVisualStyleBackColor = true;
			// 
			// chkSplash
			// 
			chkSplash.AutoSize = true;
			chkSplash.Location = new Point(12, 350);
			chkSplash.Name = "chkSplash";
			chkSplash.Size = new Size(210, 19);
			chkSplash.TabIndex = 18;
			chkSplash.Text = "Show Visual Studio Splash Window";
			chkSplash.UseVisualStyleBackColor = true;
			// 
			// btnSelectFolder
			// 
			btnSelectFolder.Location = new Point(374, 259);
			btnSelectFolder.Name = "btnSelectFolder";
			btnSelectFolder.Size = new Size(25, 25);
			btnSelectFolder.TabIndex = 13;
			btnSelectFolder.Text = "...";
			btnSelectFolder.UseVisualStyleBackColor = true;
			btnSelectFolder.Click += this.btnSelectFolder_Click;
			// 
			// txtFoldername
			// 
			txtFoldername.Location = new Point(12, 260);
			txtFoldername.Name = "txtFoldername";
			txtFoldername.Size = new Size(356, 23);
			txtFoldername.TabIndex = 12;
			txtFoldername.TextChanged += this.txtFoldername_TextChanged;
			// 
			// txtInfo
			// 
			txtInfo.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			txtInfo.Location = new Point(166, 60);
			txtInfo.Name = "txtInfo";
			txtInfo.Size = new Size(233, 15);
			txtInfo.TabIndex = 4;
			txtInfo.TabStop = true;
			txtInfo.Text = "?";
			txtInfo.TextAlign = ContentAlignment.MiddleRight;
			toolTip1.SetToolTip(txtInfo, "Information");
			txtInfo.LinkClicked += this.txtInfo_LinkClicked;
			// 
			// btnBeforeAfter
			// 
			btnBeforeAfter.Enabled = false;
			btnBeforeAfter.Location = new Point(12, 399);
			btnBeforeAfter.Name = "btnBeforeAfter";
			btnBeforeAfter.Size = new Size(100, 40);
			btnBeforeAfter.TabIndex = 19;
			btnBeforeAfter.Text = "Before/After...";
			btnBeforeAfter.UseVisualStyleBackColor = true;
			btnBeforeAfter.Click += this.btnBeforeAfter_Click;
			// 
			// cbxMonitors
			// 
			cbxMonitors.DropDownStyle = ComboBoxStyle.DropDownList;
			cbxMonitors.Location = new Point(239, 295);
			cbxMonitors.Name = "cbxMonitors";
			cbxMonitors.Size = new Size(129, 23);
			cbxMonitors.TabIndex = 15;
			// 
			// txtInfoCommands
			// 
			txtInfoCommands.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			txtInfoCommands.Location = new Point(166, 181);
			txtInfoCommands.Name = "txtInfoCommands";
			txtInfoCommands.Size = new Size(233, 15);
			txtInfoCommands.TabIndex = 10;
			txtInfoCommands.TabStop = true;
			txtInfoCommands.Text = "?";
			txtInfoCommands.TextAlign = ContentAlignment.MiddleRight;
			txtInfoCommands.LinkClicked += this.txtInfoCommands_LinkClicked;
			// 
			// txtInfoInstances
			// 
			txtInfoInstances.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			txtInfoInstances.Location = new Point(166, 125);
			txtInfoInstances.Name = "txtInfoInstances";
			txtInfoInstances.Size = new Size(233, 15);
			txtInfoInstances.TabIndex = 7;
			txtInfoInstances.TabStop = true;
			txtInfoInstances.Text = "?";
			txtInfoInstances.TextAlign = ContentAlignment.MiddleRight;
			txtInfoInstances.LinkClicked += this.txtInfoInstances_LinkClicked;
			// 
			// btnPingMonitor
			// 
			btnPingMonitor.Location = new Point(374, 293);
			btnPingMonitor.Name = "btnPingMonitor";
			btnPingMonitor.Size = new Size(25, 25);
			btnPingMonitor.TabIndex = 16;
			btnPingMonitor.Text = "!";
			btnPingMonitor.UseVisualStyleBackColor = true;
			btnPingMonitor.Click += this.btnPingMonitor_Click;
			// 
			// label6
			// 
			label6.AutoSize = true;
			label6.Location = new Point(12, 6);
			label6.Name = "label6";
			label6.Size = new Size(39, 15);
			label6.TabIndex = 0;
			label6.Text = "Name";
			// 
			// txtName
			// 
			txtName.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
			txtName.Location = new Point(12, 24);
			txtName.Name = "txtName";
			txtName.Size = new Size(387, 25);
			txtName.TabIndex = 0;
			txtName.TextChanged += this.txtInstanceName_TextChanged;
			// 
			// dlgExecuteVisualStudio
			// 
			this.AcceptButton = btnOk;
			this.AutoScaleDimensions = new SizeF(7F, 15F);
			this.AutoScaleMode = AutoScaleMode.Font;
			this.CancelButton = btnCancel;
			this.ClientSize = new Size(411, 451);
			this.ControlBox = false;
			this.Controls.Add(cbxMonitors);
			this.Controls.Add(label5);
			this.Controls.Add(btnBeforeAfter);
			this.Controls.Add(txtInfoInstances);
			this.Controls.Add(txtInfoCommands);
			this.Controls.Add(txtInfo);
			this.Controls.Add(btnPingMonitor);
			this.Controls.Add(btnSelectFolder);
			this.Controls.Add(txtFoldername);
			this.Controls.Add(label4);
			this.Controls.Add(chkSplash);
			this.Controls.Add(chkAdmin);
			this.Controls.Add(cbxInstance);
			this.Controls.Add(this.cbxVisualStudioVersion);
			this.Controls.Add(btnCancel);
			this.Controls.Add(btnOk);
			this.Controls.Add(txtName);
			this.Controls.Add(txtCommand);
			this.Controls.Add(label2);
			this.Controls.Add(label6);
			this.Controls.Add(label3);
			this.Controls.Add(label1);
			this.FormBorderStyle = FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MdiChildrenMinimizedAnchorBottom = false;
			this.MinimizeBox = false;
			this.Name = "dlgExecuteVisualStudio";
			this.ShowIcon = false;
			this.SizeGripStyle = SizeGripStyle.Hide;
			this.StartPosition = FormStartPosition.CenterParent;
			this.Text = "Execute Visual Studio";
			this.Load += this.dlgExecuteVisualStudio_Load;
			this.ResumeLayout(false);
			this.PerformLayout();
		}

		#endregion

		private Button btnCancel;
		private Button btnOk;
		private TextBox txtCommand;
		private ComboBox cbxInstance;
		private CheckBox chkAdmin;
		private CheckBox chkSplash;
		private Button btnSelectFolder;
		private TextBox txtFoldername;
		private LinkLabel txtInfo;
		private ToolTip toolTip1;
		private Button btnBeforeAfter;
		private ComboBox cbxMonitors;
		private LinkLabel txtInfoCommands;
		private LinkLabel txtInfoInstances;
		private Button btnPingMonitor;
		private VisualStudioCombobox cbxVisualStudioVersion;
		private TextBox txtName;
	}
}