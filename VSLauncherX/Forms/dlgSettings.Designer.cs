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
			grpVsCodeIdes = new GroupBox();
			lvVsCodeIdes = new ListView();
			colIdeName = new ColumnHeader();
			colIdeSource = new ColumnHeader();
			colIdePath = new ColumnHeader();
			btnAddVsCodeIde = new Button();
			btnRemoveVsCodeIde = new Button();
			btnRescanVsCodeIdes = new Button();
			grpVsCodeIdes.SuspendLayout();
			SuspendLayout();
			// 
			// btnCancel
			// 
			btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
			btnCancel.DialogResult = DialogResult.Cancel;
			btnCancel.Image = Resources.Cancel_24x24;
			btnCancel.Location = new Point(342, 388);
			btnCancel.Name = "btnCancel";
			btnCancel.Size = new Size(100, 40);
			btnCancel.TabIndex = 6;
			btnCancel.Text = " Cancel";
			btnCancel.TextImageRelation = TextImageRelation.ImageBeforeText;
			btnCancel.UseVisualStyleBackColor = true;
			// 
			// btnOk
			// 
			btnOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
			btnOk.DialogResult = DialogResult.OK;
			btnOk.Image = Resources.Check_24x24;
			btnOk.Location = new Point(448, 388);
			btnOk.Name = "btnOk";
			btnOk.Size = new Size(100, 40);
			btnOk.TabIndex = 7;
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
			// grpVsCodeIdes
			// 
			grpVsCodeIdes.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
			grpVsCodeIdes.Controls.Add(lvVsCodeIdes);
			grpVsCodeIdes.Controls.Add(btnAddVsCodeIde);
			grpVsCodeIdes.Controls.Add(btnRemoveVsCodeIde);
			grpVsCodeIdes.Controls.Add(btnRescanVsCodeIdes);
			grpVsCodeIdes.Location = new Point(12, 142);
			grpVsCodeIdes.Name = "grpVsCodeIdes";
			grpVsCodeIdes.Size = new Size(536, 236);
			grpVsCodeIdes.TabIndex = 5;
			grpVsCodeIdes.TabStop = false;
			grpVsCodeIdes.Text = "VS Code-based IDEs";
			// 
			// lvVsCodeIdes
			// 
			lvVsCodeIdes.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
			lvVsCodeIdes.Columns.AddRange(new ColumnHeader[] { colIdeName, colIdeSource, colIdePath });
			lvVsCodeIdes.FullRowSelect = true;
			lvVsCodeIdes.HideSelection = false;
			lvVsCodeIdes.Location = new Point(10, 22);
			lvVsCodeIdes.MultiSelect = false;
			lvVsCodeIdes.Name = "lvVsCodeIdes";
			lvVsCodeIdes.ShowItemToolTips = true;
			lvVsCodeIdes.Size = new Size(408, 202);
			lvVsCodeIdes.TabIndex = 0;
			lvVsCodeIdes.UseCompatibleStateImageBehavior = false;
			lvVsCodeIdes.View = View.Details;
			lvVsCodeIdes.SelectedIndexChanged += lvVsCodeIdes_SelectedIndexChanged;
			// 
			// colIdeName
			// 
			colIdeName.Text = "Name";
			colIdeName.Width = 125;
			// 
			// colIdeSource
			// 
			colIdeSource.Text = "Source";
			colIdeSource.Width = 60;
			// 
			// colIdePath
			// 
			colIdePath.Text = "Executable Path";
			colIdePath.Width = 218;
			// 
			// btnAddVsCodeIde
			// 
			btnAddVsCodeIde.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnAddVsCodeIde.Location = new Point(426, 22);
			btnAddVsCodeIde.Name = "btnAddVsCodeIde";
			btnAddVsCodeIde.Size = new Size(100, 30);
			btnAddVsCodeIde.TabIndex = 1;
			btnAddVsCodeIde.Text = "Add...";
			btnAddVsCodeIde.UseVisualStyleBackColor = true;
			btnAddVsCodeIde.Click += btnAddVsCodeIde_Click;
			// 
			// btnRemoveVsCodeIde
			// 
			btnRemoveVsCodeIde.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnRemoveVsCodeIde.Enabled = false;
			btnRemoveVsCodeIde.Location = new Point(426, 58);
			btnRemoveVsCodeIde.Name = "btnRemoveVsCodeIde";
			btnRemoveVsCodeIde.Size = new Size(100, 30);
			btnRemoveVsCodeIde.TabIndex = 2;
			btnRemoveVsCodeIde.Text = "Remove";
			btnRemoveVsCodeIde.UseVisualStyleBackColor = true;
			btnRemoveVsCodeIde.Click += btnRemoveVsCodeIde_Click;
			// 
			// btnRescanVsCodeIdes
			// 
			btnRescanVsCodeIdes.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnRescanVsCodeIdes.Location = new Point(426, 100);
			btnRescanVsCodeIdes.Name = "btnRescanVsCodeIdes";
			btnRescanVsCodeIdes.Size = new Size(100, 30);
			btnRescanVsCodeIdes.TabIndex = 3;
			btnRescanVsCodeIdes.Text = "Rescan IDEs";
			btnRescanVsCodeIdes.UseVisualStyleBackColor = true;
			btnRescanVsCodeIdes.Click += btnRescanVsCodeIdes_Click;
			// 
			// dlgSettings
			// 
			this.AcceptButton = btnOk;
			this.AutoScaleDimensions = new SizeF(7F, 15F);
			this.AutoScaleMode = AutoScaleMode.Font;
			this.CancelButton = btnCancel;
			this.ClientSize = new Size(560, 440);
			this.ControlBox = false;
			this.Controls.Add(grpVsCodeIdes);
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
			grpVsCodeIdes.ResumeLayout(false);
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
		private GroupBox grpVsCodeIdes;
		private ListView lvVsCodeIdes;
		private ColumnHeader colIdeName;
		private ColumnHeader colIdeSource;
		private ColumnHeader colIdePath;
		private Button btnAddVsCodeIde;
		private Button btnRemoveVsCodeIde;
		private Button btnRescanVsCodeIdes;
	}
}
