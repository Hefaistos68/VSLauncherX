using BrightIdeasSoftware;

namespace VSLauncher
{
	partial class dlgImportFolder
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
			Label label1;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(dlgImportFolder));
			btnSelectFolder = new Button();
			txtFoldername = new TextBox();
			btnOk = new Button();
			btnCancel = new Button();
			colPath = new ColumnHeader();
			btnRefresh = new Button();
			label1 = new Label();
			olvFiles = new TreeListView();
			olvColumnFilename = new OLVColumn();

			SuspendLayout();
			// 
			// label1
			// 
			label1.AutoSize = true;
			label1.Location = new Point(12, 9);
			label1.Name = "label1";
			label1.Size = new Size(73, 15);
			label1.TabIndex = 0;
			label1.Text = "Folder name";
			// 
			// btnSelectFolder
			// 
			btnSelectFolder.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnSelectFolder.Location = new Point(508, 26);
			btnSelectFolder.Name = "btnSelectFolder";
			btnSelectFolder.Size = new Size(25, 25);
			btnSelectFolder.TabIndex = 6;
			btnSelectFolder.Text = "...";
			btnSelectFolder.UseVisualStyleBackColor = true;
			btnSelectFolder.Click += btnSelectFolder_Click;
			// 
			// txtFoldername
			// 
			txtFoldername.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			txtFoldername.Location = new Point(12, 27);
			txtFoldername.Name = "txtFoldername";
			txtFoldername.ReadOnly = true;
			txtFoldername.Size = new Size(492, 23);
			txtFoldername.TabIndex = 1;
			txtFoldername.TextChanged += txtFoldername_TextChanged;
			// 
			// btnOk
			// 
			btnOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
			btnOk.DialogResult = DialogResult.OK;
			btnOk.Image = Resources.Check_24x24;
			btnOk.Location = new Point(461, 266);
			btnOk.Name = "btnOk";
			btnOk.Size = new Size(100, 40);
			btnOk.TabIndex = 4;
			btnOk.Text = "Ok";
			btnOk.TextImageRelation = TextImageRelation.ImageBeforeText;
			btnOk.UseVisualStyleBackColor = true;
			btnOk.Click += btnOk_Click;
			// 
			// btnCancel
			// 
			btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
			btnCancel.DialogResult = DialogResult.Cancel;
			btnCancel.Image = Resources.Cancel_24x24;
			btnCancel.Location = new Point(355, 266);
			btnCancel.Name = "btnCancel";
			btnCancel.Size = new Size(100, 40);
			btnCancel.TabIndex = 3;
			btnCancel.Text = "Cancel";
			btnCancel.TextImageRelation = TextImageRelation.ImageBeforeText;
			btnCancel.UseVisualStyleBackColor = true;
			// 
			// colPath
			// 
			colPath.Text = "Path";
			colPath.Width = 500;
			// 
			// btnRefresh
			// 
			btnRefresh.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnRefresh.Enabled = false;
			btnRefresh.Image = Resources.Refresh;
			btnRefresh.Location = new Point(536, 26);
			btnRefresh.Name = "btnRefresh";
			btnRefresh.Size = new Size(25, 25);
			btnRefresh.TabIndex = 6;
			btnRefresh.UseVisualStyleBackColor = true;
			btnRefresh.Click += btnSelectFolder_Click;
			// 
			// olvColumnFilename
			// 
			olvColumnFilename.AspectName = "Name";
			olvColumnFilename.Hideable = false;
			olvColumnFilename.IsEditable = false;
			olvColumnFilename.IsTileViewColumn = true;
			olvColumnFilename.MaximumWidth = 1000;
			olvColumnFilename.FillsFreeSpace = true;
			olvColumnFilename.MinimumWidth = 100;
			olvColumnFilename.Text = "Name";
			olvColumnFilename.ToolTipText = "Blah ";
			olvColumnFilename.Width = 140;
			// 
			// olvFiles
			// 
			olvFiles.AllColumns.Add(olvColumnFilename);
			olvFiles.AllowColumnReorder = true;
			olvFiles.AllowDrop = true;
			olvFiles.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
			olvFiles.CellEditUseWholeCell = false;
			olvFiles.Columns.AddRange(new ColumnHeader[] { olvColumnFilename });
			olvFiles.EmptyListMsg = "";
			olvFiles.FullRowSelect = true;
			olvFiles.Location =  new Point(12, 56);
			olvFiles.MultiSelect = false;
			olvFiles.CheckBoxes = true;
			olvFiles.Name = "olvFiles";
			olvFiles.SelectColumnsOnRightClickBehaviour = ObjectListView.ColumnSelectBehaviour.Submenu;
			olvFiles.ShowCommandMenuOnRightClick = true;
			olvFiles.ShowGroups = false;
			olvFiles.ShowItemToolTips = true;
			olvFiles.Size = new Size(549, 204);
			olvFiles.TabIndex = 7;
			olvFiles.UseCompatibleStateImageBehavior = false;
			olvFiles.UseFilterIndicator = true;
			olvFiles.UseFiltering = true;
			olvFiles.View = View.Details;
			olvFiles.VirtualMode = true;
			olvFiles.CellToolTipShowing += listViewFiles_CellToolTipShowing;
			// 
			// dlgImportFolder
			// 
			this.AcceptButton = btnOk;
			this.AutoScaleDimensions = new SizeF(7F, 15F);
			this.AutoScaleMode = AutoScaleMode.Font;
			this.CancelButton = btnCancel;
			this.ClientSize = new Size(573, 321);
			this.ControlBox = false;
			this.Controls.Add(olvFiles);
			this.Controls.Add(btnSelectFolder);
			this.Controls.Add(btnRefresh);
			this.Controls.Add(btnCancel);
			this.Controls.Add(btnOk);
			this.Controls.Add(txtFoldername);
			this.Controls.Add(label1);
			this.Icon = (Icon)resources.GetObject("$this.Icon");
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "dlgImportFolder";
			this.SizeGripStyle = SizeGripStyle.Show;
			this.StartPosition = FormStartPosition.CenterParent;
			this.Text = "Import from folder";
			ResumeLayout(false);
			PerformLayout();
		}

		#endregion

		private Label label1;
		private TextBox txtFoldername;
		private Button btnSelectFolder;
		private Button btnOk;
		private Button btnCancel;
		private ColumnHeader colPath;
		private Button btnRefresh;

		private TreeListView olvFiles;
		private OLVColumn olvColumnFilename;

	}
}