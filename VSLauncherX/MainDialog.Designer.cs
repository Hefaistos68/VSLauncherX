﻿using BrightIdeasSoftware;

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
			ToolStripLabel toolStripLabel1;
			ToolStripSeparator toolStripMenuItem1;
			ToolStripSeparator toolStripMenuItem2;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainDialog));
			mainPanel = new TableLayoutPanel();
			leftSubPanel = new TableLayoutPanel();
			label1 = new Label();
			mainToolstrip = new ToolStrip();
			txtFilter = new ToolStripTextBox();
			toolStripSeparator1 = new ToolStripSeparator();
			mainFolderAdd = new ToolStripButton();
			mainImportFolder = new ToolStripButton();
			mainImportVS = new ToolStripButton();
			mainRefresh = new ToolStripButton();
			_1 = new ToolStripSeparator();
			mainSettings = new ToolStripButton();
			olvFiles = new TreeListView();
			olvColumnFilename = new OLVColumn();
			olvColumnDate = new OLVColumn();
			olvColumnOptions = new OLVColumn();
			optionsRenderer = new MultiImageRenderer();
			imageList3 = new ImageList(this.components);
			flowLayoutPanel1 = new FlowLayoutPanel();
			this.selectVisualStudioVersion = new VisualStudioCombobox();
			btnMainStartVisualStudio1 = new Button();
			btnMainStartVisualStudio2 = new Button();
			btnMainStartVisualStudio3 = new Button();
			btnMainStartVisualStudio4 = new Button();
			btnMainStartVisualStudio5 = new Button();
			imageListMainIcons = new ImageList(this.components);
			toolStripStatusLabel3 = new ToolStripStatusLabel();
			tooltipForButtons = new ToolTip(this.components);
			ctxMenu = new ContextMenuStrip(this.components);
			runToolStripMenuItem = new ToolStripMenuItem();
			runAsAdminToolStripMenuItem = new ToolStripMenuItem();
			renameToolStripMenuItem = new ToolStripMenuItem();
			deleteToolStripMenuItem = new ToolStripMenuItem();
			settingsToolStripMenuItem = new ToolStripMenuItem();
			statusStrip1 = new StatusStrip();
			mainStatusLabel = new ToolStripStatusLabel();
			label2 = new Label();
			toolStripLabel1 = new ToolStripLabel();
			toolStripMenuItem1 = new ToolStripSeparator();
			toolStripMenuItem2 = new ToolStripSeparator();
			mainPanel.SuspendLayout();
			leftSubPanel.SuspendLayout();
			mainToolstrip.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)olvFiles).BeginInit();
			flowLayoutPanel1.SuspendLayout();
			ctxMenu.SuspendLayout();
			statusStrip1.SuspendLayout();
			SuspendLayout();
			// 
			// toolStripLabel1
			// 
			toolStripLabel1.Name = "toolStripLabel1";
			toolStripLabel1.Size = new Size(45, 34);
			toolStripLabel1.Text = "Search:";
			// 
			// toolStripMenuItem1
			// 
			toolStripMenuItem1.Name = "toolStripMenuItem1";
			toolStripMenuItem1.Size = new Size(145, 6);
			// 
			// toolStripMenuItem2
			// 
			toolStripMenuItem2.Name = "toolStripMenuItem2";
			toolStripMenuItem2.Size = new Size(145, 6);
			// 
			// mainPanel
			// 
			mainPanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
			mainPanel.ColumnCount = 2;
			mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 75F));
			mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
			mainPanel.Controls.Add(leftSubPanel, 0, 0);
			mainPanel.Controls.Add(flowLayoutPanel1, 1, 0);
			mainPanel.Location = new Point(0, 0);
			mainPanel.Margin = new Padding(4, 3, 4, 3);
			mainPanel.Name = "mainPanel";
			mainPanel.RowCount = 1;
			mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
			mainPanel.Size = new Size(933, 496);
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
			leftSubPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
			leftSubPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 37F));
			leftSubPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
			leftSubPanel.Size = new Size(691, 490);
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
			mainToolstrip.Items.AddRange(new ToolStripItem[] { toolStripLabel1, txtFilter, toolStripSeparator1, mainFolderAdd, mainImportFolder, mainImportVS, mainRefresh, _1, mainSettings });
			mainToolstrip.Location = new Point(0, 26);
			mainToolstrip.Name = "mainToolstrip";
			mainToolstrip.Padding = new Padding(0);
			mainToolstrip.RenderMode = ToolStripRenderMode.System;
			mainToolstrip.Size = new Size(691, 37);
			mainToolstrip.Stretch = true;
			mainToolstrip.TabIndex = 1;
			mainToolstrip.Text = "toolStrip1";
			// 
			// txtFilter
			// 
			txtFilter.MaxLength = 200;
			txtFilter.Name = "txtFilter";
			txtFilter.Size = new Size(200, 37);
			txtFilter.TextChanged += txtFilter_TextChanged;
			// 
			// toolStripSeparator1
			// 
			toolStripSeparator1.Name = "toolStripSeparator1";
			toolStripSeparator1.Size = new Size(6, 37);
			// 
			// mainFolderAdd
			// 
			mainFolderAdd.DisplayStyle = ToolStripItemDisplayStyle.Image;
			mainFolderAdd.Image = (Image)resources.GetObject("mainFolderAdd.Image");
			mainFolderAdd.ImageTransparentColor = Color.Magenta;
			mainFolderAdd.Name = "mainFolderAdd";
			mainFolderAdd.Size = new Size(23, 34);
			mainFolderAdd.Text = "&New";
			mainFolderAdd.ToolTipText = "Add new group";
			mainFolderAdd.Click += mainFolderAdd_Click;
			// 
			// mainImportFolder
			// 
			mainImportFolder.DisplayStyle = ToolStripItemDisplayStyle.Image;
			mainImportFolder.Image = Resources.OpenProjectFolder;
			mainImportFolder.ImageTransparentColor = Color.Magenta;
			mainImportFolder.Name = "mainImportFolder";
			mainImportFolder.Size = new Size(23, 34);
			mainImportFolder.Text = "Import From Folder";
			mainImportFolder.Click += mainImportFolder_Click;
			// 
			// mainImportVS
			// 
			mainImportVS.DisplayStyle = ToolStripItemDisplayStyle.Image;
			mainImportVS.Image = Resources.ImportVS;
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
			mainRefresh.ToolTipText = "Reload";
			mainRefresh.Click += mainRefresh_Click;
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
			olvFiles.AllColumns.Add(olvColumnDate);
			olvFiles.AllColumns.Add(olvColumnOptions);
			olvFiles.AllowDrop = true;
			olvFiles.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
			olvFiles.CellEditUseWholeCell = false;
			olvFiles.Columns.AddRange(new ColumnHeader[] { olvColumnFilename, olvColumnDate, olvColumnOptions });
			olvFiles.EmptyListMsg = "Add a group, import a folder or import recent items";
			olvFiles.EmptyListMsgFont = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point);
			olvFiles.FullRowSelect = true;
			olvFiles.Location = new Point(3, 66);
			olvFiles.MultiSelect = false;
			olvFiles.Name = "olvFiles";
			olvFiles.SelectColumnsOnRightClickBehaviour = ObjectListView.ColumnSelectBehaviour.Submenu;
			olvFiles.ShowCommandMenuOnRightClick = true;
			olvFiles.ShowGroups = false;
			olvFiles.ShowItemToolTips = true;
			olvFiles.Size = new Size(685, 421);
			olvFiles.SmallImageList = imageList3;
			olvFiles.TabIndex = 1;
			olvFiles.UseCompatibleStateImageBehavior = false;
			olvFiles.UseFilterIndicator = true;
			olvFiles.UseFiltering = true;
			olvFiles.View = View.Details;
			olvFiles.VirtualMode = true;
			olvFiles.CellEditFinished += olvFiles_CellEditFinished;
			olvFiles.CellClick += olvFiles_CellClick;
			olvFiles.CellRightClick += olvFiles_CellRightClick;
			olvFiles.CellToolTipShowing += olvFiles_CellToolTipShowing;
			olvFiles.Dropped += olvFiles_Dropped;
			olvFiles.HotItemChanged += olvFiles_HotItemChanged;
			olvFiles.AfterLabelEdit += olvFiles_AfterLabelEdit;
			olvFiles.ItemActivate += olvFiles_ItemActivate;
			olvFiles.SelectedIndexChanged += olvFiles_SelectedIndexChanged;
			olvFiles.DoubleClick += olvFiles_DoubleClick;
			// 
			// olvColumnFilename
			// 
			olvColumnFilename.AspectName = "Name";
			olvColumnFilename.FillsFreeSpace = true;
			olvColumnFilename.Hideable = false;
			olvColumnFilename.MinimumWidth = 100;
			olvColumnFilename.Text = "Name";
			olvColumnFilename.ToolTipText = "";
			olvColumnFilename.Width = 140;
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
			imageList3.Images.SetKeyName(7, "CPPProject");
			imageList3.Images.SetKeyName(8, "CSProject");
			imageList3.Images.SetKeyName(9, "FolderClosed");
			imageList3.Images.SetKeyName(10, "FolderOpen");
			imageList3.Images.SetKeyName(11, "FSProject");
			imageList3.Images.SetKeyName(12, "OpenProjectFolder");
			imageList3.Images.SetKeyName(13, "Refresh");
			imageList3.Images.SetKeyName(14, "RightArrowAsterisk");
			imageList3.Images.SetKeyName(15, "Run");
			imageList3.Images.SetKeyName(16, "RunAll");
			imageList3.Images.SetKeyName(17, "RunUpdate");
			imageList3.Images.SetKeyName(18, "Settings");
			imageList3.Images.SetKeyName(19, "TSProject");
			imageList3.Images.SetKeyName(20, "VBProject");
			imageList3.Images.SetKeyName(21, "RunAfter");
			imageList3.Images.SetKeyName(22, "RunBefore");
			imageList3.Images.SetKeyName(23, "None");
			imageList3.Images.SetKeyName(24, "RunAsAdmin");
			imageList3.Images.SetKeyName(25, "Application");
			imageList3.Images.SetKeyName(26, "ApplicationGroup");
			imageList3.Images.SetKeyName(27, "VsSolution");
			imageList3.Images.SetKeyName(28, "WebProject");
			// 
			// flowLayoutPanel1
			// 
			flowLayoutPanel1.Controls.Add(label2);
			flowLayoutPanel1.Controls.Add(this.selectVisualStudioVersion);
			flowLayoutPanel1.Controls.Add(btnMainStartVisualStudio1);
			flowLayoutPanel1.Controls.Add(btnMainStartVisualStudio2);
			flowLayoutPanel1.Controls.Add(btnMainStartVisualStudio3);
			flowLayoutPanel1.Controls.Add(btnMainStartVisualStudio4);
			flowLayoutPanel1.Controls.Add(btnMainStartVisualStudio5);
			flowLayoutPanel1.Dock = DockStyle.Fill;
			flowLayoutPanel1.FlowDirection = FlowDirection.TopDown;
			flowLayoutPanel1.Location = new Point(699, 0);
			flowLayoutPanel1.Margin = new Padding(0);
			flowLayoutPanel1.Name = "flowLayoutPanel1";
			flowLayoutPanel1.Size = new Size(234, 496);
			flowLayoutPanel1.TabIndex = 1;
			// 
			// selectVisualStudioVersion
			// 
			this.selectVisualStudioVersion.DrawMode = DrawMode.OwnerDrawFixed;
			this.selectVisualStudioVersion.DropDownHeight = 300;
			this.selectVisualStudioVersion.DropDownStyle = ComboBoxStyle.DropDownList;
			this.selectVisualStudioVersion.IntegralHeight = false;
			this.selectVisualStudioVersion.ItemHeight = 26;
			this.selectVisualStudioVersion.Location = new Point(0, 18);
			this.selectVisualStudioVersion.Margin = new Padding(0, 0, 0, 18);
			this.selectVisualStudioVersion.Name = "selectVisualStudioVersion";
			this.selectVisualStudioVersion.Size = new Size(232, 32);
			this.selectVisualStudioVersion.TabIndex = 0;
			this.selectVisualStudioVersion.DrawItem += selectVisualStudioVersion_DrawItem;
			this.selectVisualStudioVersion.SelectedIndexChanged += selectVisualStudioVersion_SelectedIndexChanged;
			// 
			// btnMainStartVisualStudio1
			// 
			btnMainStartVisualStudio1.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
			btnMainStartVisualStudio1.Image = (Image)resources.GetObject("btnMainStartVisualStudio1.Image");
			btnMainStartVisualStudio1.ImageAlign = ContentAlignment.MiddleLeft;
			btnMainStartVisualStudio1.Location = new Point(0, 68);
			btnMainStartVisualStudio1.Margin = new Padding(0);
			btnMainStartVisualStudio1.Name = "btnMainStartVisualStudio1";
			btnMainStartVisualStudio1.Padding = new Padding(4);
			btnMainStartVisualStudio1.Size = new Size(232, 64);
			btnMainStartVisualStudio1.TabIndex = 1;
			btnMainStartVisualStudio1.Tag = "Start {0}";
			btnMainStartVisualStudio1.Text = "Start Visual Studio";
			btnMainStartVisualStudio1.TextAlign = ContentAlignment.MiddleLeft;
			btnMainStartVisualStudio1.TextImageRelation = TextImageRelation.ImageBeforeText;
			tooltipForButtons.SetToolTip(btnMainStartVisualStudio1, "Test");
			btnMainStartVisualStudio1.UseVisualStyleBackColor = true;
			btnMainStartVisualStudio1.Click += btnMainStartVisualStudio1_Click;
			// 
			// btnMainStartVisualStudio2
			// 
			btnMainStartVisualStudio2.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
			btnMainStartVisualStudio2.Image = (Image)resources.GetObject("btnMainStartVisualStudio2.Image");
			btnMainStartVisualStudio2.ImageAlign = ContentAlignment.MiddleLeft;
			btnMainStartVisualStudio2.Location = new Point(0, 132);
			btnMainStartVisualStudio2.Margin = new Padding(0);
			btnMainStartVisualStudio2.Name = "btnMainStartVisualStudio2";
			btnMainStartVisualStudio2.Padding = new Padding(4);
			btnMainStartVisualStudio2.Size = new Size(232, 64);
			btnMainStartVisualStudio2.TabIndex = 2;
			btnMainStartVisualStudio2.Tag = "Start {0} as admin";
			btnMainStartVisualStudio2.Text = "Start Visual Studio \r\nas Admin";
			btnMainStartVisualStudio2.TextAlign = ContentAlignment.MiddleLeft;
			btnMainStartVisualStudio2.TextImageRelation = TextImageRelation.ImageBeforeText;
			tooltipForButtons.SetToolTip(btnMainStartVisualStudio2, "Test");
			btnMainStartVisualStudio2.UseVisualStyleBackColor = true;
			btnMainStartVisualStudio2.Click += btnMainStartVisualStudio2_Click;
			// 
			// btnMainStartVisualStudio3
			// 
			btnMainStartVisualStudio3.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
			btnMainStartVisualStudio3.Image = (Image)resources.GetObject("btnMainStartVisualStudio3.Image");
			btnMainStartVisualStudio3.ImageAlign = ContentAlignment.MiddleLeft;
			btnMainStartVisualStudio3.Location = new Point(0, 196);
			btnMainStartVisualStudio3.Margin = new Padding(0);
			btnMainStartVisualStudio3.Name = "btnMainStartVisualStudio3";
			btnMainStartVisualStudio3.Padding = new Padding(4);
			btnMainStartVisualStudio3.Size = new Size(232, 64);
			btnMainStartVisualStudio3.TabIndex = 3;
			btnMainStartVisualStudio3.Tag = "New {0} Instance...";
			btnMainStartVisualStudio3.Text = "New Instance...";
			btnMainStartVisualStudio3.TextAlign = ContentAlignment.MiddleLeft;
			btnMainStartVisualStudio3.TextImageRelation = TextImageRelation.ImageBeforeText;
			tooltipForButtons.SetToolTip(btnMainStartVisualStudio3, "Test");
			btnMainStartVisualStudio3.UseVisualStyleBackColor = true;
			btnMainStartVisualStudio3.Click += btnMainStartVisualStudio3_Click;
			// 
			// btnMainStartVisualStudio4
			// 
			btnMainStartVisualStudio4.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
			btnMainStartVisualStudio4.Image = (Image)resources.GetObject("btnMainStartVisualStudio4.Image");
			btnMainStartVisualStudio4.ImageAlign = ContentAlignment.MiddleLeft;
			btnMainStartVisualStudio4.Location = new Point(0, 260);
			btnMainStartVisualStudio4.Margin = new Padding(0);
			btnMainStartVisualStudio4.Name = "btnMainStartVisualStudio4";
			btnMainStartVisualStudio4.Padding = new Padding(4);
			btnMainStartVisualStudio4.Size = new Size(232, 64);
			btnMainStartVisualStudio4.TabIndex = 4;
			btnMainStartVisualStudio4.Tag = "New {0} Project...";
			btnMainStartVisualStudio4.Text = "New Project...";
			btnMainStartVisualStudio4.TextAlign = ContentAlignment.MiddleLeft;
			btnMainStartVisualStudio4.TextImageRelation = TextImageRelation.ImageBeforeText;
			tooltipForButtons.SetToolTip(btnMainStartVisualStudio4, "Test");
			btnMainStartVisualStudio4.UseVisualStyleBackColor = true;
			btnMainStartVisualStudio4.Click += btnMainStartVisualStudio4_Click;
			// 
			// btnMainStartVisualStudio5
			// 
			btnMainStartVisualStudio5.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
			btnMainStartVisualStudio5.Image = (Image)resources.GetObject("btnMainStartVisualStudio5.Image");
			btnMainStartVisualStudio5.ImageAlign = ContentAlignment.MiddleLeft;
			btnMainStartVisualStudio5.Location = new Point(0, 324);
			btnMainStartVisualStudio5.Margin = new Padding(0);
			btnMainStartVisualStudio5.Name = "btnMainStartVisualStudio5";
			btnMainStartVisualStudio5.Padding = new Padding(4);
			btnMainStartVisualStudio5.Size = new Size(232, 64);
			btnMainStartVisualStudio5.TabIndex = 5;
			btnMainStartVisualStudio5.Tag = "Start {0}...";
			btnMainStartVisualStudio5.Text = "Start...";
			btnMainStartVisualStudio5.TextAlign = ContentAlignment.MiddleLeft;
			btnMainStartVisualStudio5.TextImageRelation = TextImageRelation.ImageBeforeText;
			tooltipForButtons.SetToolTip(btnMainStartVisualStudio5, "Test");
			btnMainStartVisualStudio5.UseVisualStyleBackColor = true;
			btnMainStartVisualStudio5.Click += btnMainStartVisualStudio5_Click;
			// 
			// imageListMainIcons
			// 
			imageListMainIcons.ColorDepth = ColorDepth.Depth8Bit;
			imageListMainIcons.ImageStream = (ImageListStreamer)resources.GetObject("imageListMainIcons.ImageStream");
			imageListMainIcons.TransparentColor = Color.Transparent;
			imageListMainIcons.Images.SetKeyName(0, "CPPProject");
			imageListMainIcons.Images.SetKeyName(1, "CSProject");
			imageListMainIcons.Images.SetKeyName(2, "FolderClosed");
			imageListMainIcons.Images.SetKeyName(3, "FolderOpen");
			imageListMainIcons.Images.SetKeyName(4, "FSProject");
			imageListMainIcons.Images.SetKeyName(5, "TSProject");
			imageListMainIcons.Images.SetKeyName(6, "VBProject");
			imageListMainIcons.Images.SetKeyName(7, "Application");
			imageListMainIcons.Images.SetKeyName(8, "ApplicationGroup");
			imageListMainIcons.Images.SetKeyName(9, "VsSolution");
			// 
			// toolStripStatusLabel3
			// 
			toolStripStatusLabel3.Name = "toolStripStatusLabel3";
			toolStripStatusLabel3.Size = new Size(829, 17);
			toolStripStatusLabel3.Spring = true;
			toolStripStatusLabel3.Text = "toolStripStatusLabel3";
			toolStripStatusLabel3.TextAlign = ContentAlignment.MiddleRight;
			// 
			// ctxMenu
			// 
			ctxMenu.Items.AddRange(new ToolStripItem[] { runToolStripMenuItem, runAsAdminToolStripMenuItem, renameToolStripMenuItem, toolStripMenuItem1, deleteToolStripMenuItem, toolStripMenuItem2, settingsToolStripMenuItem });
			ctxMenu.Name = "ctxMenu";
			ctxMenu.Size = new Size(149, 126);
			// 
			// runToolStripMenuItem
			// 
			runToolStripMenuItem.Name = "runToolStripMenuItem";
			runToolStripMenuItem.Size = new Size(148, 22);
			runToolStripMenuItem.Text = "Run";
			runToolStripMenuItem.Click += runToolStripMenuItem_Click;
			// 
			// runAsAdminToolStripMenuItem
			// 
			runAsAdminToolStripMenuItem.Name = "runAsAdminToolStripMenuItem";
			runAsAdminToolStripMenuItem.Size = new Size(148, 22);
			runAsAdminToolStripMenuItem.Text = "Run as Admin";
			runAsAdminToolStripMenuItem.Click += runAsAdminToolStripMenuItem_Click;
			// 
			// renameToolStripMenuItem
			// 
			renameToolStripMenuItem.Name = "renameToolStripMenuItem";
			renameToolStripMenuItem.Size = new Size(148, 22);
			renameToolStripMenuItem.Text = "Rename...";
			renameToolStripMenuItem.Click += renameToolStripMenuItem_Click;
			// 
			// deleteToolStripMenuItem
			// 
			deleteToolStripMenuItem.Name = "deleteToolStripMenuItem";
			deleteToolStripMenuItem.Size = new Size(148, 22);
			deleteToolStripMenuItem.Text = "Delete...";
			deleteToolStripMenuItem.Click += deleteToolStripMenuItem_Click;
			// 
			// settingsToolStripMenuItem
			// 
			settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
			settingsToolStripMenuItem.Size = new Size(148, 22);
			settingsToolStripMenuItem.Text = "Settings...";
			settingsToolStripMenuItem.Click += settingsToolStripMenuItem_Click;
			// 
			// statusStrip1
			// 
			statusStrip1.Items.AddRange(new ToolStripItem[] { mainStatusLabel });
			statusStrip1.Location = new Point(0, 497);
			statusStrip1.Name = "statusStrip1";
			statusStrip1.Size = new Size(933, 22);
			statusStrip1.TabIndex = 1;
			statusStrip1.Text = "statusStrip1";
			// 
			// mainStatusLabel
			// 
			mainStatusLabel.Name = "mainStatusLabel";
			mainStatusLabel.Size = new Size(94, 17);
			mainStatusLabel.Text = "mainStatusLabel";
			// 
			// label2
			// 
			label2.AutoSize = true;
			label2.Location = new Point(3, 3);
			label2.Margin = new Padding(3, 3, 3, 0);
			label2.Name = "label2";
			label2.Size = new Size(119, 15);
			label2.TabIndex = 6;
			label2.Text = "Visual Studio version:";
			// 
			// MainDialog
			// 
			this.AutoScaleDimensions = new SizeF(7F, 15F);
			this.AutoScaleMode = AutoScaleMode.Font;
			this.ClientSize = new Size(933, 519);
			this.Controls.Add(statusStrip1);
			this.Controls.Add(mainPanel);
			this.Icon = (Icon)resources.GetObject("$this.Icon");
			this.Margin = new Padding(4, 3, 4, 3);
			this.Name = "MainDialog";
			this.Text = "Visual Studio Launcher";
			Load += MainDialog_Load;
			mainPanel.ResumeLayout(false);
			leftSubPanel.ResumeLayout(false);
			leftSubPanel.PerformLayout();
			mainToolstrip.ResumeLayout(false);
			mainToolstrip.PerformLayout();
			((System.ComponentModel.ISupportInitialize)olvFiles).EndInit();
			flowLayoutPanel1.ResumeLayout(false);
			flowLayoutPanel1.PerformLayout();
			ctxMenu.ResumeLayout(false);
			statusStrip1.ResumeLayout(false);
			statusStrip1.PerformLayout();
			ResumeLayout(false);
			PerformLayout();
		}

		#endregion
		private ToolStrip mainToolstrip;
		private ToolStripButton mainFolderAdd;
		private ToolStripButton mainRefresh;
		private ToolStripButton mainSettings;
		private TreeListView olvFiles;
		private OLVColumn olvColumnFilename;
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
		private FlowLayoutPanel flowLayoutPanel1;
		private Button btnMainStartVisualStudio1;
		private Button btnMainStartVisualStudio2;
		private Button btnMainStartVisualStudio3;
		private Button btnMainStartVisualStudio4;
		private Button btnMainStartVisualStudio5;
		private VisualStudioCombobox selectVisualStudioVersion;
		private ToolTip tooltipForButtons;
		private ContextMenuStrip ctxMenu;
		private ToolStripMenuItem runToolStripMenuItem;
		private ToolStripMenuItem runAsAdminToolStripMenuItem;
		private ToolStripMenuItem renameToolStripMenuItem;
		private ToolStripSeparator toolStripMenuItem1;
		private ToolStripMenuItem settingsToolStripMenuItem;
		private ToolStripTextBox txtFilter;
		private ToolStripSeparator toolStripSeparator1;
		private StatusStrip statusStrip1;
		private ToolStripStatusLabel mainStatusLabel;
		private ToolStripMenuItem deleteToolStripMenuItem;
		private Label label2;
	}
}

