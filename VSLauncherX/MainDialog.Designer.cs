using BrightIdeasSoftware;

namespace VSLauncher
{
	partial class MainDialog
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainDialog));
			mainPanel = new TableLayoutPanel();
			leftSubPanel = new TableLayoutPanel();
			label1 = new Label();
			mainToolstrip = new ToolStrip();
			mainFolderAdd = new ToolStripButton();
			mainImportFolder = new ToolStripButton();
			mainImportVS = new ToolStripButton();
			mainRefresh = new ToolStripButton();
			_1 = new ToolStripSeparator();
			mainSettings = new ToolStripButton();
			olvFiles = new TreeListView();
			olvColumnFilename = new OLVColumn();
			olvColumnPath = new OLVColumn();
			olvColumnDate = new OLVColumn();
			olvColumnOptions = new OLVColumn();
			optionsRenderer = new MultiImageRenderer();
			imageList3 = new ImageList(this.components);
			imageListMainIcons = new ImageList(this.components);
			toolStripStatusLabel3 = new ToolStripStatusLabel();
			mainPanel.SuspendLayout();
			leftSubPanel.SuspendLayout();
			mainToolstrip.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)olvFiles).BeginInit();
			SuspendLayout();
			// 
			// mainPanel
			// 
			mainPanel.ColumnCount = 2;
			mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 75F));
			mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
			mainPanel.Controls.Add(leftSubPanel, 0, 0);
			mainPanel.Dock = DockStyle.Fill;
			mainPanel.Location = new Point(0, 0);
			mainPanel.Margin = new Padding(4, 3, 4, 3);
			mainPanel.Name = "mainPanel";
			mainPanel.RowCount = 1;
			mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
			mainPanel.Size = new Size(933, 519);
			mainPanel.TabIndex = 0;
			// 
			// leftSubPanel
			// 
			leftSubPanel.ColumnCount = 1;
			leftSubPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			leftSubPanel.Controls.Add(label1, 0, 0);
			leftSubPanel.Controls.Add(mainToolstrip, 0, 1);
			leftSubPanel.Controls.Add(olvFiles, 0, 2);
			leftSubPanel.Dock = DockStyle.Fill;
			leftSubPanel.Location = new Point(4, 3);
			leftSubPanel.Margin = new Padding(4, 3, 4, 3);
			leftSubPanel.Name = "leftSubPanel";
			leftSubPanel.RowCount = 3;
			leftSubPanel.RowStyles.Add(new RowStyle());
			leftSubPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 37F));
			leftSubPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
			leftSubPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 23F));
			leftSubPanel.Size = new Size(691, 513);
			leftSubPanel.TabIndex = 0;
			// 
			// label1
			// 
			label1.AutoSize = true;
			label1.Font = new Font("Verdana", 14.25F, FontStyle.Bold, GraphicsUnit.Point);
			label1.Location = new Point(4, 0);
			label1.Margin = new Padding(4, 0, 4, 0);
			label1.Name = "label1";
			label1.Size = new Size(230, 23);
			label1.TabIndex = 0;
			label1.Text = "Solutions && Projects";
			// 
			// mainToolstrip
			// 
			mainToolstrip.AllowMerge = false;
			mainToolstrip.Dock = DockStyle.Fill;
			mainToolstrip.GripMargin = new Padding(0);
			mainToolstrip.GripStyle = ToolStripGripStyle.Hidden;
			mainToolstrip.Items.AddRange(new ToolStripItem[] { mainFolderAdd, mainImportFolder, mainImportVS, mainRefresh, _1, mainSettings });
			mainToolstrip.Location = new Point(0, 23);
			mainToolstrip.Name = "mainToolstrip";
			mainToolstrip.Padding = new Padding(0);
			mainToolstrip.RenderMode = ToolStripRenderMode.System;
			mainToolstrip.Size = new Size(691, 37);
			mainToolstrip.TabIndex = 2;
			mainToolstrip.Text = "toolStrip1";
			// 
			// mainFolderAdd
			// 
			mainFolderAdd.DisplayStyle = ToolStripItemDisplayStyle.Image;
			mainFolderAdd.Image = (Image)resources.GetObject("mainFolderAdd.Image");
			mainFolderAdd.ImageTransparentColor = Color.Magenta;
			mainFolderAdd.Name = "mainFolderAdd";
			mainFolderAdd.Size = new Size(23, 34);
			mainFolderAdd.Text = "&New";
			mainFolderAdd.Click += mainFolderAdd_Click;
			// 
			// mainImportFolder
			// 
			mainImportFolder.DisplayStyle = ToolStripItemDisplayStyle.Image;
			mainImportFolder.Image = VSLauncher.Resources.OpenProjectFolder;
			mainImportFolder.ImageTransparentColor = Color.Magenta;
			mainImportFolder.Name = "mainImportFolder";
			mainImportFolder.Size = new Size(23, 34);
			mainImportFolder.Text = "Import From Folder";
			mainImportFolder.Click += mainImportFolder_Click;
			// 
			// mainImportVS
			// 
			mainImportVS.DisplayStyle = ToolStripItemDisplayStyle.Image;
			mainImportVS.Image = VSLauncher.Resources.ImportVS;
			mainImportVS.ImageTransparentColor = Color.Magenta;
			mainImportVS.Name = "mainImportVS";
			mainImportVS.Size = new Size(23, 34);
			mainImportVS.Text = "toolStripButton2";
			mainImportVS.ToolTipText = "Import from Visual Studio";
			mainImportVS.Click += mainImportVS_Click;
			// 
			// mainRefresh
			// 
			mainRefresh.DisplayStyle = ToolStripItemDisplayStyle.Image;
			mainRefresh.Image = (Image)resources.GetObject("mainRefresh.Image");
			mainRefresh.ImageTransparentColor = Color.Magenta;
			mainRefresh.Name = "mainRefresh";
			mainRefresh.Size = new Size(23, 34);
			mainRefresh.Text = "He&lp";
			// 
			// _1
			// 
			_1.Name = "_1";
			_1.Size = new Size(6, 37);
			// 
			// mainSettings
			// 
			mainSettings.DisplayStyle = ToolStripItemDisplayStyle.Image;
			mainSettings.Image = (Image)resources.GetObject("mainSettings.Image");
			mainSettings.ImageTransparentColor = Color.Magenta;
			mainSettings.Name = "mainSettings";
			mainSettings.Size = new Size(23, 34);
			mainSettings.Text = "mainSettings";
			mainSettings.ToolTipText = "Settings";
			mainSettings.Click += mainSettings_Click;
			// 
			// olvFiles
			// 
			olvFiles.AllColumns.Add(olvColumnFilename);
			olvFiles.AllColumns.Add(olvColumnPath);
			olvFiles.AllColumns.Add(olvColumnDate);
			olvFiles.AllColumns.Add(olvColumnOptions);
			olvFiles.AllowColumnReorder = true;
			olvFiles.AllowDrop = true;
			olvFiles.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
			olvFiles.CellEditUseWholeCell = false;
			olvFiles.Columns.AddRange(new ColumnHeader[] { olvColumnFilename, olvColumnPath, olvColumnDate, olvColumnOptions });
			olvFiles.EmptyListMsg = "Add a group to start";
			olvFiles.EmptyListMsgFont = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point);
			olvFiles.FullRowSelect = true;
			olvFiles.Location = new Point(3, 63);
			olvFiles.MultiSelect = false;
			olvFiles.Name = "olvFiles";
			olvFiles.SelectColumnsOnRightClickBehaviour = ObjectListView.ColumnSelectBehaviour.Submenu;
			olvFiles.ShowCommandMenuOnRightClick = true;
			olvFiles.ShowGroups = false;
			olvFiles.ShowItemToolTips = true;
			olvFiles.Size = new Size(685, 447);
			olvFiles.SmallImageList = imageList3;
			olvFiles.TabIndex = 13;
			olvFiles.UseCompatibleStateImageBehavior = false;
			olvFiles.UseFilterIndicator = true;
			olvFiles.UseFiltering = true;
			olvFiles.View = View.Details;
			olvFiles.VirtualMode = true;
			olvFiles.CellClick += listViewFiles_CellClick;
			olvFiles.CellRightClick += listViewFiles_CellRightClick;
			olvFiles.CellToolTipShowing += listViewFiles_CellToolTipShowing;
			olvFiles.HotItemChanged += olv_HotItemChanged;
			olvFiles.ItemActivate += listViewFiles_ItemActivate;
			// 
			// olvColumnFilename
			// 
			olvColumnFilename.AspectName = "Name";
			olvColumnFilename.Hideable = false;
			olvColumnFilename.IsEditable = false;
			olvColumnFilename.IsTileViewColumn = true;
			olvColumnFilename.MaximumWidth = 200;
			olvColumnFilename.MinimumWidth = 100;
			olvColumnFilename.Text = "Name";
			olvColumnFilename.ToolTipText = "Blah ";
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
			olvColumnPath.ToolTipText = "Blah ";
			olvColumnPath.Width = 140;
			// 
			// olvColumnDate
			// 
			olvColumnDate.AspectName = "Date";
			olvColumnDate.Groupable = false;
			olvColumnDate.IsEditable = false;
			olvColumnDate.MaximumWidth = 200;
			olvColumnDate.MinimumWidth = 100;
			olvColumnDate.Searchable = false;
			olvColumnDate.Sortable = false;
			olvColumnDate.Text = "Last Modified";
			olvColumnDate.ToolTipText = "Blah ";
			olvColumnDate.Width = 140;
			// 
			// olvColumnOptions
			// 
			olvColumnOptions.AspectName = "Options";
			olvColumnOptions.Groupable = false;
			olvColumnOptions.Hideable = false;
			olvColumnOptions.IsEditable = false;
			olvColumnOptions.MaximumWidth = 100;
			olvColumnOptions.MinimumWidth = 100;
			olvColumnOptions.Renderer = optionsRenderer;
			olvColumnOptions.Searchable = false;
			olvColumnOptions.Sortable = false;
			olvColumnOptions.Text = "Run Options";
			olvColumnOptions.ToolTipText = "Blah ";
			olvColumnOptions.UseFiltering = false;
			olvColumnOptions.Width = 100;
			// 
			// optionsRenderer
			// 
			optionsRenderer.ImageName = "star";
			optionsRenderer.MaximumValue = 50;
			optionsRenderer.MaxNumberImages = 5;
			// 
			// imageList3
			// 
			imageList3.ColorDepth = ColorDepth.Depth8Bit;
			imageList3.ImageStream = (ImageListStreamer)resources.GetObject("imageList3.ImageStream");
			imageList3.TransparentColor = Color.Transparent;
			imageList3.Images.SetKeyName(0, "Add");
			imageList3.Images.SetKeyName(1, "AddFavorite");
			imageList3.Images.SetKeyName(2, "AddFolder");
			imageList3.Images.SetKeyName(3, "Group");
			imageList3.Images.SetKeyName(4, "ArrowDownEnd");
			imageList3.Images.SetKeyName(5, "ArrowUpEnd");
			imageList3.Images.SetKeyName(6, "ConnectArrow");
			imageList3.Images.SetKeyName(7, "CPPProjectNode");
			imageList3.Images.SetKeyName(8, "CSProjectNode");
			imageList3.Images.SetKeyName(9, "FolderClosedBlue");
			imageList3.Images.SetKeyName(10, "FolderOpenBlue");
			imageList3.Images.SetKeyName(11, "FSProjectNode");
			imageList3.Images.SetKeyName(12, "OpenProjectFolder");
			imageList3.Images.SetKeyName(13, "Refresh");
			imageList3.Images.SetKeyName(14, "RightArrowAsterisk");
			imageList3.Images.SetKeyName(15, "Run");
			imageList3.Images.SetKeyName(16, "RunAll");
			imageList3.Images.SetKeyName(17, "RunUpdate");
			imageList3.Images.SetKeyName(18, "Settings");
			imageList3.Images.SetKeyName(19, "TSProjectNode");
			imageList3.Images.SetKeyName(20, "VBProjectNode");
			imageList3.Images.SetKeyName(21, "RunAfter");
			imageList3.Images.SetKeyName(22, "RunBefore");
			imageList3.Images.SetKeyName(23, "None");
			imageList3.Images.SetKeyName(24, "RunAsAdmin");
			// 
			// imageListMainIcons
			// 
			imageListMainIcons.ColorDepth = ColorDepth.Depth8Bit;
			imageListMainIcons.ImageStream = (ImageListStreamer)resources.GetObject("imageListMainIcons.ImageStream");
			imageListMainIcons.TransparentColor = Color.Transparent;
			imageListMainIcons.Images.SetKeyName(0, "CPPProjectNode");
			imageListMainIcons.Images.SetKeyName(1, "CSProjectNode");
			imageListMainIcons.Images.SetKeyName(2, "FolderClosed");
			imageListMainIcons.Images.SetKeyName(3, "FolderOpen");
			imageListMainIcons.Images.SetKeyName(4, "FSProjectNode");
			imageListMainIcons.Images.SetKeyName(5, "TSProjectNode");
			imageListMainIcons.Images.SetKeyName(6, "VBProjectNode");
			// 
			// toolStripStatusLabel3
			// 
			toolStripStatusLabel3.Name = "toolStripStatusLabel3";
			toolStripStatusLabel3.Size = new Size(829, 17);
			toolStripStatusLabel3.Spring = true;
			toolStripStatusLabel3.Text = "toolStripStatusLabel3";
			toolStripStatusLabel3.TextAlign = ContentAlignment.MiddleRight;
			// 
			// MainDialog
			// 
			this.AutoScaleDimensions = new SizeF(7F, 15F);
			this.AutoScaleMode = AutoScaleMode.Font;
			this.ClientSize = new Size(933, 519);
			this.Controls.Add(mainPanel);
			this.Margin = new Padding(4, 3, 4, 3);
			this.Name = "MainDialog";
			this.Text = "Visual Studio Launcher";
			mainPanel.ResumeLayout(false);
			leftSubPanel.ResumeLayout(false);
			leftSubPanel.PerformLayout();
			mainToolstrip.ResumeLayout(false);
			mainToolstrip.PerformLayout();
			((System.ComponentModel.ISupportInitialize)olvFiles).EndInit();
			ResumeLayout(false);
		}

		#endregion
		private ToolStrip mainToolstrip;
		private ToolStripButton mainFolderAdd;
		private ToolStripButton mainRefresh;
		private ToolStripButton mainSettings;
		private TreeListView olvFiles;
		private OLVColumn olvColumnFilename;
		private OLVColumn olvColumnPath;
		private OLVColumn olvColumnOptions;
		private OLVColumn olvColumnDate;

		private ContextMenuStrip contextMenuStrip2;
		private ToolStripStatusLabel toolStripStatusLabel3;
		private TableLayoutPanel mainPanel;
		private TableLayoutPanel leftSubPanel;
		private Label label1;
		private ToolStripSeparator _1;
		private ImageList imageList3;
		private ImageList imageListMainIcons;
		private MultiImageRenderer optionsRenderer;
		private ToolStripButton mainImportFolder;
		private ToolStripButton mainImportVS;
	}
}

