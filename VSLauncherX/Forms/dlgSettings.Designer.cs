namespace VSLauncher.Forms
{
	partial class dlgSettings
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(dlgSettings));
			btnCancel = new Button();
			btnOk = new Button();
			chkAlwaysAdmin = new CheckBox();
			chkSync = new CheckBox();
			chkShowPath = new CheckBox();
			chkAutostart = new CheckBox();
			chkDarkMode = new CheckBox();
			SuspendLayout();
			// 
			// btnCancel
			// 
			btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
			btnCancel.DialogResult = DialogResult.Cancel;
			btnCancel.Image = Resources.Cancel_24x24;
			btnCancel.Location = new Point(334, 261);
			btnCancel.Name = "btnCancel";
			btnCancel.Size = new Size(100, 40);
			btnCancel.TabIndex = 5;
			btnCancel.Text = " Cancel";
			btnCancel.TextImageRelation = TextImageRelation.ImageBeforeText;
			btnCancel.UseVisualStyleBackColor = true;
			// 
			// btnOk
			// 
			btnOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
			btnOk.DialogResult = DialogResult.OK;
			btnOk.Image = Resources.Check_24x24;
			btnOk.Location = new Point(440, 261);
			btnOk.Name = "btnOk";
			btnOk.Size = new Size(100, 40);
			btnOk.TabIndex = 6;
			btnOk.Text = " Ok";
			btnOk.TextImageRelation = TextImageRelation.ImageBeforeText;
			btnOk.UseVisualStyleBackColor = true;
			btnOk.Click += btnOk_Click;
			// 
			// chkAlwaysAdmin
			// 
			chkAlwaysAdmin.AutoSize = true;
			chkAlwaysAdmin.Location = new Point(12, 12);
			chkAlwaysAdmin.Name = "chkAlwaysAdmin";
			chkAlwaysAdmin.Size = new Size(142, 19);
			chkAlwaysAdmin.TabIndex = 0;
			chkAlwaysAdmin.Text = "Always start as Admin";
			chkAlwaysAdmin.UseVisualStyleBackColor = true;
			// 
			// chkSync
			// 
			chkSync.AutoSize = true;
			chkSync.Location = new Point(12, 62);
			chkSync.Name = "chkSync";
			chkSync.Size = new Size(192, 19);
			chkSync.TabIndex = 2;
			chkSync.Text = "Synchronize with VS Recent List";
			chkSync.UseVisualStyleBackColor = true;
			// 
			// chkShowPath
			// 
			chkShowPath.AutoSize = true;
			chkShowPath.Location = new Point(12, 87);
			chkShowPath.Name = "chkShowPath";
			chkShowPath.Size = new Size(219, 19);
			chkShowPath.TabIndex = 3;
			chkShowPath.Text = "Show path for solutions and projects";
			chkShowPath.UseVisualStyleBackColor = true;
			// 
			// chkAutostart
			// 
			chkAutostart.AutoSize = true;
			chkAutostart.Location = new Point(12, 37);
			chkAutostart.Name = "chkAutostart";
			chkAutostart.Size = new Size(126, 19);
			chkAutostart.TabIndex = 1;
			chkAutostart.Text = "Autostart on logon";
			chkAutostart.UseVisualStyleBackColor = true;
			// 
			// chkDarkMode
			// 
			chkDarkMode.AutoSize = true;
			chkDarkMode.Location = new Point(12, 112);
			chkDarkMode.Name = "chkDarkMode";
			chkDarkMode.Size = new Size(133, 19);
			chkDarkMode.TabIndex = 4;
			chkDarkMode.Text = "Enable dark theme";
			chkDarkMode.UseVisualStyleBackColor = true;
			// 
			// dlgSettings
			// 
			this.AcceptButton = btnOk;
			this.AutoScaleDimensions = new SizeF(7F, 15F);
			this.AutoScaleMode = AutoScaleMode.Font;
			this.CancelButton = btnCancel;
			this.ClientSize = new Size(552, 313);
			this.ControlBox = false;
			this.Controls.Add(chkDarkMode);
			this.Controls.Add(chkShowPath);
			this.Controls.Add(chkSync);
			this.Controls.Add(chkAutostart);
			this.Controls.Add(chkAlwaysAdmin);
			this.Controls.Add(btnCancel);
			this.Controls.Add(btnOk);
			this.FormBorderStyle = FormBorderStyle.FixedDialog;
			this.Icon = (Icon)resources.GetObject("$this.Icon");
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "dlgSettings";
			this.SizeGripStyle = SizeGripStyle.Hide;
			this.StartPosition = FormStartPosition.CenterParent;
			this.Text = "Visual Studio Launcher General Settings";
			Load += dlgSettings_Load;
			ResumeLayout(false);
			PerformLayout();
		}

		#endregion

		private Button btnCancel;
		private Button btnOk;
		private CheckBox chkAlwaysAdmin;
		private CheckBox chkSync;
		private CheckBox chkShowPath;
		private CheckBox chkAutostart;
		private CheckBox chkDarkMode;
	}
}