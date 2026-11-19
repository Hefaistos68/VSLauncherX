using VSLauncher.Controls;

namespace VSLauncher
{
	partial class dlgExecuteVsCode
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
			Label lblName;
			Label lblEdition;
			Label lblProfile;
			Label lblWindowMode;
			Label lblCommands;
			Label lblPath;
			Label lblMonitor;
			this.txtName = new TextBox();
			this.cbxVsCodeEdition = new IdeCombobox();
			this.cbxProfile = new ComboBox();
			this.cbxWindowMode = new ComboBox();
			this.lnkCommandsHelp = new LinkLabel();
			this.txtCommand = new TextBox();
			this.txtPath = new TextBox();
			this.btnBrowseFile = new Button();
			this.btnBrowseFolder = new Button();
			this.cbxMonitors = new ComboBox();
			this.btnPingMonitor = new Button();
			this.chkAdmin = new CheckBox();
			this.chkDisableExtensions = new CheckBox();
			this.btnBeforeAfter = new Button();
			this.btnCancel = new Button();
			this.btnOk = new Button();
			this.toolTip1 = new ToolTip(this.components);
			lblName = new Label();
			lblEdition = new Label();
			lblProfile = new Label();
			lblWindowMode = new Label();
			lblCommands = new Label();
			lblPath = new Label();
			lblMonitor = new Label();
			this.SuspendLayout();
			//
			// lblName
			//
			lblName.AutoSize = true;
			lblName.Location = new Point(12, 9);
			lblName.Name = "lblName";
			lblName.Size = new Size(42, 15);
			lblName.TabIndex = 0;
			lblName.Text = "Name:";
			//
			// txtName
			//
			this.txtName.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
			this.txtName.Location = new Point(12, 27);
			this.txtName.Name = "txtName";
			this.txtName.Size = new Size(387, 25);
			this.txtName.TabIndex = 1;
			//
			// lblEdition
			//
			lblEdition.AutoSize = true;
			lblEdition.Location = new Point(12, 62);
			lblEdition.Name = "lblEdition";
			lblEdition.Size = new Size(46, 15);
			lblEdition.TabIndex = 2;
			lblEdition.Text = "Edition:";
			//
			// cbxVsCodeEdition
			//
			this.cbxVsCodeEdition.DrawMode = DrawMode.OwnerDrawFixed;
			this.cbxVsCodeEdition.DropDownStyle = ComboBoxStyle.DropDownList;
			this.cbxVsCodeEdition.FormattingEnabled = true;
			this.cbxVsCodeEdition.IntegralHeight = false;
			this.cbxVsCodeEdition.ItemHeight = 26;
			this.cbxVsCodeEdition.Location = new Point(12, 80);
			this.cbxVsCodeEdition.Name = "cbxVsCodeEdition";
			this.cbxVsCodeEdition.ShowDefault = true;
			this.cbxVsCodeEdition.Size = new Size(387, 32);
			this.cbxVsCodeEdition.TabIndex = 3;
			this.cbxVsCodeEdition.SelectedIndexChanged += this.cbxVsCodeEdition_SelectedIndexChanged;
			//
			// lblProfile
			//
			lblProfile.AutoSize = true;
			lblProfile.Location = new Point(12, 122);
			lblProfile.Name = "lblProfile";
			lblProfile.Size = new Size(43, 15);
			lblProfile.TabIndex = 4;
			lblProfile.Text = "Profile:";
			//
			// cbxProfile
			//
			this.cbxProfile.DropDownStyle = ComboBoxStyle.DropDown;
			this.cbxProfile.FormattingEnabled = true;
			this.cbxProfile.Location = new Point(12, 140);
			this.cbxProfile.Name = "cbxProfile";
			this.cbxProfile.Size = new Size(387, 23);
			this.cbxProfile.TabIndex = 5;
			//
			// lblWindowMode
			//
			lblWindowMode.AutoSize = true;
			lblWindowMode.Location = new Point(12, 173);
			lblWindowMode.Name = "lblWindowMode";
			lblWindowMode.Size = new Size(86, 15);
			lblWindowMode.TabIndex = 6;
			lblWindowMode.Text = "Window Mode:";
			//
			// cbxWindowMode
			//
			this.cbxWindowMode.DropDownStyle = ComboBoxStyle.DropDownList;
			this.cbxWindowMode.FormattingEnabled = true;
			this.cbxWindowMode.Items.AddRange(new object[]
			{
				"Default",
				"New Window",
				"Reuse Window"
			});
			this.cbxWindowMode.Location = new Point(12, 191);
			this.cbxWindowMode.Name = "cbxWindowMode";
			this.cbxWindowMode.Size = new Size(387, 23);
			this.cbxWindowMode.TabIndex = 7;
			//
			// lblCommands
			//
			lblCommands.AutoSize = true;
			lblCommands.Location = new Point(12, 224);
			lblCommands.Name = "lblCommands";
			lblCommands.Size = new Size(125, 15);
			lblCommands.TabIndex = 8;
			lblCommands.Text = "Additional Arguments:";
			//
			// lnkCommandsHelp
			//
			this.lnkCommandsHelp.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			this.lnkCommandsHelp.Location = new Point(166, 224);
			this.lnkCommandsHelp.Name = "lnkCommandsHelp";
			this.lnkCommandsHelp.Size = new Size(233, 15);
			this.lnkCommandsHelp.TabIndex = 9;
			this.lnkCommandsHelp.TabStop = true;
			this.lnkCommandsHelp.Text = "?";
			this.lnkCommandsHelp.TextAlign = ContentAlignment.MiddleRight;
			this.toolTip1.SetToolTip(this.lnkCommandsHelp, "Open VS Code command-line documentation");
			this.lnkCommandsHelp.LinkClicked += this.lnkCommandsHelp_LinkClicked;
			//
			// txtCommand
			//
			this.txtCommand.Location = new Point(12, 242);
			this.txtCommand.Name = "txtCommand";
			this.txtCommand.Size = new Size(387, 23);
			this.txtCommand.TabIndex = 10;
			//
			// lblPath
			//
			lblPath.AutoSize = true;
			lblPath.Location = new Point(12, 275);
			lblPath.Name = "lblPath";
			lblPath.Size = new Size(118, 15);
			lblPath.TabIndex = 11;
			lblPath.Text = "Workspace or Folder:";
			//
			// txtPath
			//
			this.txtPath.Location = new Point(12, 293);
			this.txtPath.Name = "txtPath";
			this.txtPath.Size = new Size(259, 23);
			this.txtPath.TabIndex = 12;
			this.txtPath.TextChanged += this.txtPath_TextChanged;
			//
			// btnBrowseFile
			//
			this.btnBrowseFile.Location = new Point(275, 292);
			this.btnBrowseFile.Name = "btnBrowseFile";
			this.btnBrowseFile.Size = new Size(54, 25);
			this.btnBrowseFile.TabIndex = 13;
			this.btnBrowseFile.Text = "File...";
			this.toolTip1.SetToolTip(this.btnBrowseFile, "Browse for a .code-workspace file");
			this.btnBrowseFile.UseVisualStyleBackColor = true;
			this.btnBrowseFile.Click += this.btnBrowseFile_Click;
			//
			// btnBrowseFolder
			//
			this.btnBrowseFolder.Location = new Point(333, 292);
			this.btnBrowseFolder.Name = "btnBrowseFolder";
			this.btnBrowseFolder.Size = new Size(66, 25);
			this.btnBrowseFolder.TabIndex = 14;
			this.btnBrowseFolder.Text = "Folder...";
			this.toolTip1.SetToolTip(this.btnBrowseFolder, "Browse for a project folder");
			this.btnBrowseFolder.UseVisualStyleBackColor = true;
			this.btnBrowseFolder.Click += this.btnBrowseFolder_Click;
			//
			// lblMonitor
			//
			lblMonitor.AutoSize = true;
			lblMonitor.Location = new Point(12, 330);
			lblMonitor.Name = "lblMonitor";
			lblMonitor.Size = new Size(104, 15);
			lblMonitor.TabIndex = 15;
			lblMonitor.Text = "Preferred Monitor:";
			//
			// cbxMonitors
			//
			this.cbxMonitors.DropDownStyle = ComboBoxStyle.DropDownList;
			this.cbxMonitors.FormattingEnabled = true;
			this.cbxMonitors.Location = new Point(239, 327);
			this.cbxMonitors.Name = "cbxMonitors";
			this.cbxMonitors.Size = new Size(129, 23);
			this.cbxMonitors.TabIndex = 16;
			//
			// btnPingMonitor
			//
			this.btnPingMonitor.Location = new Point(374, 325);
			this.btnPingMonitor.Name = "btnPingMonitor";
			this.btnPingMonitor.Size = new Size(25, 25);
			this.btnPingMonitor.TabIndex = 17;
			this.btnPingMonitor.Text = "!";
			this.toolTip1.SetToolTip(this.btnPingMonitor, "Identify selected monitor");
			this.btnPingMonitor.UseVisualStyleBackColor = true;
			this.btnPingMonitor.Click += this.btnPingMonitor_Click;
			//
			// chkAdmin
			//
			this.chkAdmin.AutoSize = true;
			this.chkAdmin.Location = new Point(12, 360);
			this.chkAdmin.Name = "chkAdmin";
			this.chkAdmin.Size = new Size(100, 19);
			this.chkAdmin.TabIndex = 18;
			this.chkAdmin.Text = "Run as Admin";
			this.chkAdmin.UseVisualStyleBackColor = true;
			//
			// chkDisableExtensions
			//
			this.chkDisableExtensions.AutoSize = true;
			this.chkDisableExtensions.Location = new Point(12, 385);
			this.chkDisableExtensions.Name = "chkDisableExtensions";
			this.chkDisableExtensions.Size = new Size(250, 19);
			this.chkDisableExtensions.TabIndex = 19;
			this.chkDisableExtensions.Text = "Disable Extensions (--disable-extensions)";
			this.chkDisableExtensions.UseVisualStyleBackColor = true;
			//
			// btnBeforeAfter
			//
			this.btnBeforeAfter.Enabled = false;
			this.btnBeforeAfter.Location = new Point(12, 421);
			this.btnBeforeAfter.Name = "btnBeforeAfter";
			this.btnBeforeAfter.Size = new Size(100, 40);
			this.btnBeforeAfter.TabIndex = 20;
			this.btnBeforeAfter.Text = "Before/After...";
			this.btnBeforeAfter.UseVisualStyleBackColor = true;
			this.btnBeforeAfter.Click += this.btnBeforeAfter_Click;
			//
			// btnCancel
			//
			this.btnCancel.DialogResult = DialogResult.Cancel;
			this.btnCancel.Image = Resources.Cancel_24x24;
			this.btnCancel.Location = new Point(193, 421);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new Size(100, 40);
			this.btnCancel.TabIndex = 21;
			this.btnCancel.Text = " Cancel";
			this.btnCancel.TextImageRelation = TextImageRelation.ImageBeforeText;
			this.btnCancel.UseVisualStyleBackColor = true;
			//
			// btnOk
			//
			this.btnOk.DialogResult = DialogResult.OK;
			this.btnOk.Image = Resources.Check_24x24;
			this.btnOk.Location = new Point(299, 421);
			this.btnOk.Name = "btnOk";
			this.btnOk.Size = new Size(100, 40);
			this.btnOk.TabIndex = 22;
			this.btnOk.Text = "&Save";
			this.btnOk.TextImageRelation = TextImageRelation.ImageBeforeText;
			this.btnOk.UseVisualStyleBackColor = true;
			this.btnOk.Click += this.btnOk_Click;
			//
			// dlgExecuteVsCode
			//
			this.AcceptButton = this.btnOk;
			this.AutoScaleDimensions = new SizeF(7F, 15F);
			this.AutoScaleMode = AutoScaleMode.Font;
			this.CancelButton = this.btnCancel;
			this.ClientSize = new Size(411, 473);
			this.ControlBox = false;
			this.Controls.Add(this.btnBeforeAfter);
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.btnOk);
			this.Controls.Add(this.chkDisableExtensions);
			this.Controls.Add(this.chkAdmin);
			this.Controls.Add(this.btnPingMonitor);
			this.Controls.Add(this.cbxMonitors);
			this.Controls.Add(lblMonitor);
			this.Controls.Add(this.btnBrowseFolder);
			this.Controls.Add(this.btnBrowseFile);
			this.Controls.Add(this.txtPath);
			this.Controls.Add(lblPath);
			this.Controls.Add(this.txtCommand);
			this.Controls.Add(this.lnkCommandsHelp);
			this.Controls.Add(lblCommands);
			this.Controls.Add(this.cbxWindowMode);
			this.Controls.Add(lblWindowMode);
			this.Controls.Add(this.cbxProfile);
			this.Controls.Add(lblProfile);
			this.Controls.Add(this.cbxVsCodeEdition);
			this.Controls.Add(lblEdition);
			this.Controls.Add(this.txtName);
			this.Controls.Add(lblName);
			this.FormBorderStyle = FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MdiChildrenMinimizedAnchorBottom = false;
			this.MinimizeBox = false;
			this.Name = "dlgExecuteVsCode";
			this.ShowIcon = false;
			this.SizeGripStyle = SizeGripStyle.Hide;
			this.StartPosition = FormStartPosition.CenterParent;
			this.Text = "VS Code Settings";
			this.Load += this.dlgExecuteVsCode_Load;
			this.ResumeLayout(false);
			this.PerformLayout();
		}

		#endregion

		private TextBox txtName;
		private IdeCombobox cbxVsCodeEdition;
		private ComboBox cbxProfile;
		private ComboBox cbxWindowMode;
		private LinkLabel lnkCommandsHelp;
		private TextBox txtCommand;
		private TextBox txtPath;
		private Button btnBrowseFile;
		private Button btnBrowseFolder;
		private ComboBox cbxMonitors;
		private Button btnPingMonitor;
		private CheckBox chkAdmin;
		private CheckBox chkDisableExtensions;
		private Button btnBeforeAfter;
		private Button btnCancel;
		private Button btnOk;
		private ToolTip toolTip1;
	}
}
