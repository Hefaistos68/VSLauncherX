using BrightIdeasSoftware;

namespace VSLauncher
{
	partial class dlgImportVisualStudio
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(dlgImportVisualStudio));
			btnOk = new Button();
			btnCancel = new Button();
			colPath = new ColumnHeader();
			btnRefresh = new Button();
			olvFiles = new TreeListView();
			olvColumnFilename = new OLVColumn();
			olvColumnPath = new OLVColumn();
			imageList = new ImageList(this.components);
			label1 = new Label();
			((System.ComponentModel.ISupportInitialize)olvFiles).BeginInit();
			SuspendLayout();
			// 
			// label1
			// 
			label1.AutoSize = true;
			label1.Location = new Point(12, 19);
			label1.Name = "label1";
			label1.Size = new Size(79, 15);
			label1.TabIndex = 0;
			label1.Text = "MRU's found:";
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
			btnOk.Text = " Import";
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
			btnCancel.Text = " Cancel";
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
			btnRefresh.Location = new Point(536, 9);
			btnRefresh.Name = "btnRefresh";
			btnRefresh.Size = new Size(25, 25);
			btnRefresh.TabIndex = 6;
			btnRefresh.UseVisualStyleBackColor = true;
			btnRefresh.Click += btnSelectFolder_Click;
			// 
			// olvFiles
			// 
			olvFiles.AllColumns.Add(olvColumnFilename);
			olvFiles.AllColumns.Add(olvColumnPath);
			olvFiles.AllowDrop = true;
			olvFiles.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
			olvFiles.CellEditUseWholeCell = false;
			olvFiles.CheckBoxes = true;
			olvFiles.Columns.AddRange(new ColumnHeader[] { olvColumnFilename, olvColumnPath });
			olvFiles.EmptyListMsg = "";
			olvFiles.FullRowSelect = true;
			olvFiles.Location = new Point(12, 40);
			olvFiles.Name = "olvFiles";
			olvFiles.SelectColumnsOnRightClickBehaviour = ObjectListView.ColumnSelectBehaviour.Submenu;
			olvFiles.ShowCommandMenuOnRightClick = true;
			olvFiles.ShowGroups = false;
			olvFiles.ShowImagesOnSubItems = true;
			olvFiles.ShowItemToolTips = true;
			olvFiles.Size = new Size(549, 220);
			olvFiles.SmallImageList = imageList;
			olvFiles.TabIndex = 7;
			olvFiles.UseCompatibleStateImageBehavior = false;
			olvFiles.View = View.Details;
			olvFiles.VirtualMode = true;
			olvFiles.CellToolTipShowing += listViewFiles_CellToolTipShowing;
			// 
			// olvColumnFilename
			// 
			olvColumnFilename.AspectName = "Name";
			olvColumnFilename.Hideable = false;
			olvColumnFilename.IsEditable = false;
			olvColumnFilename.IsTileViewColumn = true;
			olvColumnFilename.MaximumWidth = 1000;
			olvColumnFilename.MinimumWidth = 100;
			olvColumnFilename.Searchable = false;
			olvColumnFilename.Sortable = false;
			olvColumnFilename.Text = "Name";
			olvColumnFilename.ToolTipText = "";
			olvColumnFilename.Width = 140;
			// 
			// olvColumnPath
			// 
			olvColumnPath.AspectName = "Path";
			olvColumnPath.FillsFreeSpace = true;
			olvColumnPath.Groupable = false;
			olvColumnPath.IsEditable = false;
			olvColumnPath.MaximumWidth = 800;
			olvColumnPath.MinimumWidth = 100;
			olvColumnPath.Text = "Path";
			olvColumnPath.ToolTipText = "";
			olvColumnPath.Width = 140;
			// 
			// imageList
			// 
			imageList.ColorDepth = ColorDepth.Depth8Bit;
			imageList.ImageStream = (ImageListStreamer)resources.GetObject("imageList.ImageStream");
			imageList.TransparentColor = Color.Transparent;
			imageList.Images.SetKeyName(0, "Application");
			imageList.Images.SetKeyName(1, "ApplicationGroup");
			imageList.Images.SetKeyName(2, "CPPProject");
			imageList.Images.SetKeyName(3, "CSProject");
			imageList.Images.SetKeyName(4, "FolderClosed");
			imageList.Images.SetKeyName(5, "FSProject");
			imageList.Images.SetKeyName(6, "JSProject");
			imageList.Images.SetKeyName(7, "TSProject");
			imageList.Images.SetKeyName(8, "VBProject");
			imageList.Images.SetKeyName(9, "VsSolution");
			imageList.Images.SetKeyName(10, "WebProject");
			imageList.Images.SetKeyName(11, "VSLogo");
			// 
			// dlgImportVisualStudio
			// 
			this.AcceptButton = btnOk;
			this.AutoScaleDimensions = new SizeF(7F, 15F);
			this.AutoScaleMode = AutoScaleMode.Font;
			this.CancelButton = btnCancel;
			this.ClientSize = new Size(573, 321);
			this.ControlBox = false;
			this.Controls.Add(olvFiles);
			this.Controls.Add(btnRefresh);
			this.Controls.Add(btnCancel);
			this.Controls.Add(btnOk);
			this.Controls.Add(label1);
			this.Icon = (Icon)resources.GetObject("$this.Icon");
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "dlgImportVisualStudio";
			this.SizeGripStyle = SizeGripStyle.Show;
			this.StartPosition = FormStartPosition.CenterParent;
			this.Text = "Import from Visual Studio Recent List";
			((System.ComponentModel.ISupportInitialize)olvFiles).EndInit();
			ResumeLayout(false);
			PerformLayout();
		}

		#endregion

		private Label label1;
		private Button btnOk;
		private Button btnCancel;
		private ColumnHeader colPath;
		private Button btnRefresh;

		private TreeListView olvFiles;
		private OLVColumn olvColumnFilename;
		private OLVColumn olvColumnPath;
		private ImageList imageList;
	}
}