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
			ToolStripSeparator toolStripMenuItem1;
			ToolStripSeparator toolStripMenuItem2;
			FlowLayoutPanel flowLayoutPanel2;
			Label label3;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainDialog));
			TableLayoutPanel leftSubPanel;
			Label titleLabel;
			txtFilter = new TextBox();
			button1 = new Button();
			button2 = new Button();
			button3 = new Button();
			button4 = new Button();
			spacer = new Label();
			button5 = new Button();
			olvFiles = new TreeListView();
			olvColumnFilename = new OLVColumn();
			olvColumnDate = new OLVColumn();
			olvColumnOptions = new OLVColumn();
			optionsRenderer = new MultiImageRenderer();
			imageList3 = new ImageList(this.components);
			mainPanel = new TableLayoutPanel();
			flowLayoutPanel1 = new FlowLayoutPanel();
			label2 = new Label();
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
			toolStripMenuItem1 = new ToolStripSeparator();
			toolStripMenuItem2 = new ToolStripSeparator();
			flowLayoutPanel2 = new FlowLayoutPanel();
			label3 = new Label();
			leftSubPanel = new TableLayoutPanel();
			titleLabel = new Label();
			flowLayoutPanel2.SuspendLayout();
			leftSubPanel.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)olvFiles).BeginInit();
			mainPanel.SuspendLayout();
			flowLayoutPanel1.SuspendLayout();
			ctxMenu.SuspendLayout();
			statusStrip1.SuspendLayout();
			SuspendLayout();
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
			// flowLayoutPanel2
			// 
			flowLayoutPanel2.BackColor = SystemColors.ScrollBar;
			flowLayoutPanel2.Controls.Add(label3);
			flowLayoutPanel2.Controls.Add(txtFilter);
			flowLayoutPanel2.Controls.Add(button1);
			flowLayoutPanel2.Controls.Add(button2);
			flowLayoutPanel2.Controls.Add(button3);
			flowLayoutPanel2.Controls.Add(button4);
			flowLayoutPanel2.Controls.Add(spacer);
			flowLayoutPanel2.Controls.Add(button5);
			flowLayoutPanel2.Dock = DockStyle.Fill;
			flowLayoutPanel2.Location = new Point(0, 40);
			flowLayoutPanel2.Margin = new Padding(0);
			flowLayoutPanel2.Name = "flowLayoutPanel2";
			flowLayoutPanel2.Size = new Size(695, 35);
			flowLayoutPanel2.TabIndex = 2;
			flowLayoutPanel2.WrapContents = false;
			// 
			// label3
			// 
			label3.Anchor = AnchorStyles.Top | AnchorStyles.Bottom;
			label3.Location = new Point(3, 0);
			label3.Name = "label3";
			label3.Size = new Size(58, 35);
			label3.TabIndex = 0;
			label3.Text = "Search:";
			label3.TextAlign = ContentAlignment.MiddleRight;
			// 
			// txtFilter
			// 
			txtFilter.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			txtFilter.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point);
			txtFilter.Location = new Point(67, 3);
			txtFilter.Name = "txtFilter";
			txtFilter.Size = new Size(439, 29);
			txtFilter.TabIndex = 1;
			txtFilter.Text = "type here";
			txtFilter.TextChanged += txtFilter_TextChanged;
			// 
			// button1
			// 
			button1.Image = (Image)resources.GetObject("button1.Image");
			button1.Location = new Point(510, 1);
			button1.Margin = new Padding(1);
			button1.Name = "button1";
			button1.Size = new Size(32, 32);
			button1.TabIndex = 2;
			tooltipForButtons.SetToolTip(button1, "Add new Group");
			button1.UseVisualStyleBackColor = true;
			button1.Click += mainFolderAdd_Click;
			// 
			// button2
			// 
			button2.Image = (Image)resources.GetObject("button2.Image");
			button2.Location = new Point(544, 1);
			button2.Margin = new Padding(1);
			button2.Name = "button2";
			button2.Size = new Size(32, 32);
			button2.TabIndex = 2;
			tooltipForButtons.SetToolTip(button2, "Import from Folder");
			button2.UseVisualStyleBackColor = true;
			button2.Click += mainImportFolder_Click;
			// 
			// button3
			// 
			button3.Image = (Image)resources.GetObject("button3.Image");
			button3.Location = new Point(578, 1);
			button3.Margin = new Padding(1);
			button3.Name = "button3";
			button3.Size = new Size(32, 32);
			button3.TabIndex = 2;
			tooltipForButtons.SetToolTip(button3, "Import Visual Studio Recents");
			button3.UseVisualStyleBackColor = true;
			button3.Click += mainImportVS_Click;
			// 
			// button4
			// 
			button4.Image = (Image)resources.GetObject("button4.Image");
			button4.Location = new Point(612, 1);
			button4.Margin = new Padding(1);
			button4.Name = "button4";
			button4.Size = new Size(32, 32);
			button4.TabIndex = 2;
			tooltipForButtons.SetToolTip(button4, "Refresh & Sync");
			button4.UseVisualStyleBackColor = true;
			button4.Click += mainRefresh_Click;
			// 
			// spacer
			// 
			spacer.Anchor = AnchorStyles.Top | AnchorStyles.Bottom;
			spacer.Location = new Point(648, 0);
			spacer.Name = "spacer";
			spacer.Size = new Size(4, 35);
			spacer.TabIndex = 0;
			spacer.Text = " ";
			spacer.TextAlign = ContentAlignment.MiddleRight;
			// 
			// button5
			// 
			button5.Image = (Image)resources.GetObject("button5.Image");
			button5.Location = new Point(656, 1);
			button5.Margin = new Padding(1);
			button5.Name = "button5";
			button5.Size = new Size(32, 32);
			button5.TabIndex = 2;
			tooltipForButtons.SetToolTip(button5, "Application Settings");
			button5.UseVisualStyleBackColor = true;
			button5.Click += mainSettings_Click;
			// 
			// leftSubPanel
			// 
			leftSubPanel.BackColor = SystemColors.ScrollBar;
			leftSubPanel.ColumnCount = 1;
			leftSubPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			leftSubPanel.Controls.Add(titleLabel, 0, 0);
			leftSubPanel.Controls.Add(olvFiles, 0, 2);
			leftSubPanel.Controls.Add(flowLayoutPanel2, 0, 1);
			leftSubPanel.Dock = DockStyle.Fill;
			leftSubPanel.Location = new Point(0, 0);
			leftSubPanel.Margin = new Padding(0, 0, 4, 0);
			leftSubPanel.Name = "leftSubPanel";
			leftSubPanel.RowCount = 3;
			leftSubPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
			leftSubPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F));
			leftSubPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
			leftSubPanel.Size = new Size(695, 519);
			leftSubPanel.TabIndex = 0;
			// 
			// titleLabel
			// 
			titleLabel.BackColor = SystemColors.ScrollBar;
			titleLabel.CausesValidation = false;
			titleLabel.Dock = DockStyle.Fill;
			titleLabel.Font = new Font("Verdana", 18F, FontStyle.Bold, GraphicsUnit.Point);
			titleLabel.Location = new Point(4, 0);
			titleLabel.Margin = new Padding(4, 0, 4, 0);
			titleLabel.Name = "titleLabel";
			titleLabel.Size = new Size(687, 40);
			titleLabel.TabIndex = 0;
			titleLabel.Text = "Solutions & Projects";
			titleLabel.TextAlign = ContentAlignment.MiddleLeft;
			titleLabel.UseMnemonic = false;
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
			olvFiles.Location = new Point(0, 75);
			olvFiles.Margin = new Padding(0);
			olvFiles.MultiSelect = false;
			olvFiles.Name = "olvFiles";
			olvFiles.SelectColumnsOnRightClickBehaviour = ObjectListView.ColumnSelectBehaviour.Submenu;
			olvFiles.ShowCommandMenuOnRightClick = true;
			olvFiles.ShowGroups = false;
			olvFiles.ShowItemToolTips = true;
			olvFiles.Size = new Size(695, 444);
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
			imageList3.Images.SetKeyName(14, "Run");
			imageList3.Images.SetKeyName(15, "RunAll");
			imageList3.Images.SetKeyName(16, "RunUpdate");
			imageList3.Images.SetKeyName(17, "Settings");
			imageList3.Images.SetKeyName(18, "TSProject");
			imageList3.Images.SetKeyName(19, "VBProject");
			imageList3.Images.SetKeyName(20, "None");
			imageList3.Images.SetKeyName(21, "RunAsAdmin");
			imageList3.Images.SetKeyName(22, "Application");
			imageList3.Images.SetKeyName(23, "ApplicationGroup");
			imageList3.Images.SetKeyName(24, "VsSolution");
			imageList3.Images.SetKeyName(25, "WebProject");
			imageList3.Images.SetKeyName(26, "RunAfter");
			imageList3.Images.SetKeyName(27, "RunBefore");
			// 
			// mainPanel
			// 
			mainPanel.ColumnCount = 2;
			mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 234F));
			mainPanel.Controls.Add(leftSubPanel, 0, 0);
			mainPanel.Controls.Add(flowLayoutPanel1, 1, 0);
			mainPanel.Dock = DockStyle.Fill;
			mainPanel.Location = new Point(0, 0);
			mainPanel.Margin = new Padding(4, 3, 3, 3);
			mainPanel.Name = "mainPanel";
			mainPanel.RowCount = 1;
			mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
			mainPanel.Size = new Size(933, 519);
			mainPanel.TabIndex = 0;
			mainPanel.Resize += mainPanel_Resize;
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
			flowLayoutPanel1.Size = new Size(234, 519);
			flowLayoutPanel1.TabIndex = 1;
			// 
			// label2
			// 
			label2.AutoSize = true;
			label2.Location = new Point(3, 25);
			label2.Margin = new Padding(3, 25, 3, 0);
			label2.Name = "label2";
			label2.Size = new Size(119, 15);
			label2.TabIndex = 6;
			label2.Text = "Visual Studio version:";
			// 
			// selectVisualStudioVersion
			// 
			this.selectVisualStudioVersion.DrawMode = DrawMode.OwnerDrawFixed;
			this.selectVisualStudioVersion.DropDownHeight = 300;
			this.selectVisualStudioVersion.DropDownStyle = ComboBoxStyle.DropDownList;
			this.selectVisualStudioVersion.IntegralHeight = false;
			this.selectVisualStudioVersion.ItemHeight = 28;
			this.selectVisualStudioVersion.Location = new Point(0, 40);
			this.selectVisualStudioVersion.Margin = new Padding(0, 0, 0, 6);
			this.selectVisualStudioVersion.Name = "selectVisualStudioVersion";
			this.selectVisualStudioVersion.SelectedItem = null;
			this.selectVisualStudioVersion.Size = new Size(232, 34);
			this.selectVisualStudioVersion.TabIndex = 0;
			tooltipForButtons.SetToolTip(this.selectVisualStudioVersion, "Visual Studio versions currently instlled");
			this.selectVisualStudioVersion.DrawItem += selectVisualStudioVersion_DrawItem;
			this.selectVisualStudioVersion.SelectedIndexChanged += selectVisualStudioVersion_SelectedIndexChanged;
			// 
			// btnMainStartVisualStudio1
			// 
			btnMainStartVisualStudio1.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
			btnMainStartVisualStudio1.Image = (Image)resources.GetObject("btnMainStartVisualStudio1.Image");
			btnMainStartVisualStudio1.ImageAlign = ContentAlignment.MiddleLeft;
			btnMainStartVisualStudio1.Location = new Point(0, 80);
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
			btnMainStartVisualStudio2.Location = new Point(0, 144);
			btnMainStartVisualStudio2.Margin = new Padding(0);
			btnMainStartVisualStudio2.Name = "btnMainStartVisualStudio2";
			btnMainStartVisualStudio2.Padding = new Padding(4);
			btnMainStartVisualStudio2.Size = new Size(232, 64);
			btnMainStartVisualStudio2.TabIndex = 2;
			btnMainStartVisualStudio2.Tag = "Start {0} as Admin";
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
			btnMainStartVisualStudio3.Location = new Point(0, 208);
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
			btnMainStartVisualStudio4.Location = new Point(0, 272);
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
			btnMainStartVisualStudio5.Location = new Point(0, 336);
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
			mainStatusLabel.Size = new Size(193, 17);
			mainStatusLabel.Text = "Lets do something incredible today";
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
			FormClosing += MainDialog_FormClosing;
			Load += MainDialog_Load;
			flowLayoutPanel2.ResumeLayout(false);
			flowLayoutPanel2.PerformLayout();
			leftSubPanel.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)olvFiles).EndInit();
			mainPanel.ResumeLayout(false);
			flowLayoutPanel1.ResumeLayout(false);
			flowLayoutPanel1.PerformLayout();
			ctxMenu.ResumeLayout(false);
			statusStrip1.ResumeLayout(false);
			statusStrip1.PerformLayout();
			ResumeLayout(false);
			PerformLayout();
		}

		#endregion
		private TreeListView olvFiles;
		private OLVColumn olvColumnFilename;
		private OLVColumn olvColumnOptions;
		private OLVColumn olvColumnDate;

		private ToolStripStatusLabel toolStripStatusLabel3;
		private TableLayoutPanel mainPanel;
		private ImageList imageList3;
		private ImageList imageListMainIcons;
		private MultiImageRenderer optionsRenderer;
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
		private ToolStripMenuItem settingsToolStripMenuItem;
		private StatusStrip statusStrip1;
		private ToolStripStatusLabel mainStatusLabel;
		private ToolStripMenuItem deleteToolStripMenuItem;
		private Label label2;
		private Label label3;
		private TextBox txtFilter;
		private Button button1;
		private Button button2;
		private Button button3;
		private Button button4;
		private Button button5;
		private Label spacer;
	}
}

