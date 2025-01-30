using BrightIdeasSoftware;

using VSLauncher.Controls;

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
			FlowLayoutPanel flowLayoutPanel2;
			Label label3;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainDialog));
			TableLayoutPanel leftSubPanel;
			Label titleLabel;
			Label label2;
			this.toolStripSeparator1 = new ToolStripSeparator();
			this.toolStripSeparator2 = new ToolStripSeparator();
			this.txtFilter = new TextBoxEx();
			this.btnAddFolder = new Button();
			this.btnImportFolder = new Button();
			this.btnImportSoP = new Button();
			this.btnImportVS = new Button();
			this.btnRefresh = new Button();
			this.spacer1 = new Label();
			this.btnExplorer = new Button();
			this.spacer2 = new Label();
			this.btnSettings = new Button();
			this.olvFiles = new TreeListView();
			this.olvColumnFilename = new OLVColumn();
			this.olvColumnGit = new OLVColumn();
			this.olvColumnGitName = new OLVColumn();
			this.olvColumnDate = new OLVColumn();
			this.olvColumnVersion = new OLVColumn();
			this.olvColumnOptions = new OLVColumn();
			this.optionsRenderer = new MultiImageRenderer();
			this.imageList3 = new ImageList(this.components);
			this.mainPanel = new TableLayoutPanel();
			this.flowLayoutPanel1 = new FlowLayoutPanel();
			this.selectVisualStudioVersion = new VisualStudioCombobox();
			this.btnMainStartVisualStudio1 = new Button();
			this.btnMainStartVisualStudio2 = new Button();
			this.btnMainStartVisualStudio3 = new Button();
			this.btnMainStartVisualStudio4 = new Button();
			this.btnMainStartVisualStudio5 = new Button();
			this.btnMainOpenActivityLog = new Button();
			this.btnVsInstaller = new Button();
			this.imageListMainIcons = new ImageList(this.components);
			this.toolStripStatusLabel3 = new ToolStripStatusLabel();
			this.tooltipForButtons = new ToolTip(this.components);
			this.ctxMenu = new ContextMenuStrip(this.components);
			this.addToolStripMenuItem = new ToolStripMenuItem();
			this.newGroupToolStripMenuItem = new ToolStripMenuItem();
			this.fromFolderToolStripMenuItem = new ToolStripMenuItem();
			this.solutionProjectToolStripMenuItem = new ToolStripMenuItem();
			this.toolStripMenuItem3 = new ToolStripSeparator();
			this.runToolStripMenuItem = new ToolStripMenuItem();
			this.runAsAdminToolStripMenuItem = new ToolStripMenuItem();
			this.renameToolStripMenuItem = new ToolStripMenuItem();
			this.settingsToolStripMenuItem = new ToolStripMenuItem();
			this.removeToolStripMenuItem = new ToolStripMenuItem();
			this.toolStripMenuItem4 = new ToolStripSeparator();
			this.favoriteToolStripMenuItem = new ToolStripMenuItem();
			this.explorerToolStripMenuItem = new ToolStripMenuItem();
			this.gitToolStripMenuItem = new ToolStripMenuItem();
			this.fetchToolStripMenuItem = new ToolStripMenuItem();
			this.pullToolStripMenuItem = new ToolStripMenuItem();
			this.statusStrip1 = new StatusStrip();
			this.mainStatusLabel = new ToolStripStatusLabel();
			this.toolStripStatusGit = new ToolStripStatusLabel();
			this.gitTimer = new System.Windows.Forms.Timer(this.components);
			this.branchesToolStripMenuItem = new ToolStripMenuItem();
			this.toolStripMenuItem1 = new ToolStripSeparator();
			this.xToolStripMenuItem = new ToolStripMenuItem();
			flowLayoutPanel2 = new FlowLayoutPanel();
			label3 = new Label();
			leftSubPanel = new TableLayoutPanel();
			titleLabel = new Label();
			label2 = new Label();
			flowLayoutPanel2.SuspendLayout();
			leftSubPanel.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)this.olvFiles).BeginInit();
			this.mainPanel.SuspendLayout();
			this.flowLayoutPanel1.SuspendLayout();
			this.ctxMenu.SuspendLayout();
			this.statusStrip1.SuspendLayout();
			SuspendLayout();
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new Size(205, 6);
			// 
			// toolStripSeparator2
			// 
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			this.toolStripSeparator2.Size = new Size(205, 6);
			// 
			// flowLayoutPanel2
			// 
			flowLayoutPanel2.BackColor = SystemColors.ScrollBar;
			flowLayoutPanel2.Controls.Add(label3);
			flowLayoutPanel2.Controls.Add(this.txtFilter);
			flowLayoutPanel2.Controls.Add(this.btnAddFolder);
			flowLayoutPanel2.Controls.Add(this.btnImportFolder);
			flowLayoutPanel2.Controls.Add(this.btnImportSoP);
			flowLayoutPanel2.Controls.Add(this.btnImportVS);
			flowLayoutPanel2.Controls.Add(this.btnRefresh);
			flowLayoutPanel2.Controls.Add(this.spacer1);
			flowLayoutPanel2.Controls.Add(this.btnExplorer);
			flowLayoutPanel2.Controls.Add(this.spacer2);
			flowLayoutPanel2.Controls.Add(this.btnSettings);
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
			this.txtFilter.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			this.txtFilter.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point);
			this.txtFilter.Location = new Point(67, 3);
			this.txtFilter.Name = "txtFilter";
			this.txtFilter.ShowClearButton = true;
			this.txtFilter.Size = new Size(364, 29);
			this.txtFilter.TabIndex = 1;
			this.txtFilter.TextChanged += txtFilter_TextChanged;
			this.txtFilter.KeyPress += txtFilter_KeyPress;
			// 
			// btnAddFolder
			// 
			this.btnAddFolder.Image = (Image)resources.GetObject("btnAddFolder.Image");
			this.btnAddFolder.Location = new Point(435, 1);
			this.btnAddFolder.Margin = new Padding(1);
			this.btnAddFolder.Name = "btnAddFolder";
			this.btnAddFolder.Size = new Size(32, 32);
			this.btnAddFolder.TabIndex = 2;
			this.tooltipForButtons.SetToolTip(this.btnAddFolder, "Add new Group");
			this.btnAddFolder.UseVisualStyleBackColor = true;
			this.btnAddFolder.Click += mainFolderAdd_Click;
			// 
			// btnImportFolder
			// 
			this.btnImportFolder.Image = (Image)resources.GetObject("btnImportFolder.Image");
			this.btnImportFolder.Location = new Point(469, 1);
			this.btnImportFolder.Margin = new Padding(1);
			this.btnImportFolder.Name = "btnImportFolder";
			this.btnImportFolder.Size = new Size(32, 32);
			this.btnImportFolder.TabIndex = 2;
			this.tooltipForButtons.SetToolTip(this.btnImportFolder, "Import from Folder");
			this.btnImportFolder.UseVisualStyleBackColor = true;
			this.btnImportFolder.Click += mainImportFolder_Click;
			// 
			// btnImportSoP
			// 
			this.btnImportSoP.Image = (Image)resources.GetObject("btnImportSoP.Image");
			this.btnImportSoP.Location = new Point(503, 1);
			this.btnImportSoP.Margin = new Padding(1);
			this.btnImportSoP.Name = "btnImportSoP";
			this.btnImportSoP.Size = new Size(32, 32);
			this.btnImportSoP.TabIndex = 2;
			this.tooltipForButtons.SetToolTip(this.btnImportSoP, "Add a solution or project");
			this.btnImportSoP.UseVisualStyleBackColor = true;
			this.btnImportSoP.Click += mainImportSoP_Click;
			// 
			// btnImportVS
			// 
			this.btnImportVS.Image = (Image)resources.GetObject("btnImportVS.Image");
			this.btnImportVS.Location = new Point(537, 1);
			this.btnImportVS.Margin = new Padding(1);
			this.btnImportVS.Name = "btnImportVS";
			this.btnImportVS.Size = new Size(32, 32);
			this.btnImportVS.TabIndex = 2;
			this.tooltipForButtons.SetToolTip(this.btnImportVS, "Import Visual Studio Recents");
			this.btnImportVS.UseVisualStyleBackColor = true;
			this.btnImportVS.Click += mainImportVS_Click;
			// 
			// btnRefresh
			// 
			this.btnRefresh.Image = (Image)resources.GetObject("btnRefresh.Image");
			this.btnRefresh.Location = new Point(571, 1);
			this.btnRefresh.Margin = new Padding(1);
			this.btnRefresh.Name = "btnRefresh";
			this.btnRefresh.Size = new Size(32, 32);
			this.btnRefresh.TabIndex = 2;
			this.tooltipForButtons.SetToolTip(this.btnRefresh, "Refresh & Sync");
			this.btnRefresh.UseVisualStyleBackColor = true;
			this.btnRefresh.Click += mainRefresh_Click;
			// 
			// spacer1
			// 
			this.spacer1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom;
			this.spacer1.Location = new Point(607, 0);
			this.spacer1.Name = "spacer1";
			this.spacer1.Size = new Size(4, 35);
			this.spacer1.TabIndex = 0;
			this.spacer1.Text = " ";
			this.spacer1.TextAlign = ContentAlignment.MiddleRight;
			// 
			// btnExplorer
			// 
			this.btnExplorer.Image = (Image)resources.GetObject("btnExplorer.Image");
			this.btnExplorer.Location = new Point(615, 1);
			this.btnExplorer.Margin = new Padding(1);
			this.btnExplorer.Name = "btnExplorer";
			this.btnExplorer.Size = new Size(32, 32);
			this.btnExplorer.TabIndex = 2;
			this.tooltipForButtons.SetToolTip(this.btnExplorer, "Open in Explorer");
			this.btnExplorer.UseVisualStyleBackColor = true;
			this.btnExplorer.Click += mainOpenInExplorer_Click;
			// 
			// spacer2
			// 
			this.spacer2.Anchor = AnchorStyles.Top | AnchorStyles.Bottom;
			this.spacer2.Location = new Point(651, 0);
			this.spacer2.Name = "spacer2";
			this.spacer2.Size = new Size(4, 35);
			this.spacer2.TabIndex = 0;
			this.spacer2.Text = " ";
			this.spacer2.TextAlign = ContentAlignment.MiddleRight;
			// 
			// btnSettings
			// 
			this.btnSettings.Image = (Image)resources.GetObject("btnSettings.Image");
			this.btnSettings.Location = new Point(659, 1);
			this.btnSettings.Margin = new Padding(1);
			this.btnSettings.Name = "btnSettings";
			this.btnSettings.Size = new Size(32, 32);
			this.btnSettings.TabIndex = 2;
			this.tooltipForButtons.SetToolTip(this.btnSettings, "Application Settings");
			this.btnSettings.UseVisualStyleBackColor = true;
			this.btnSettings.Click += mainSettings_Click;
			// 
			// leftSubPanel
			// 
			leftSubPanel.BackColor = SystemColors.ScrollBar;
			leftSubPanel.ColumnCount = 1;
			leftSubPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			leftSubPanel.Controls.Add(titleLabel, 0, 0);
			leftSubPanel.Controls.Add(this.olvFiles, 0, 2);
			leftSubPanel.Controls.Add(flowLayoutPanel2, 0, 1);
			leftSubPanel.Dock = DockStyle.Fill;
			leftSubPanel.Location = new Point(0, 0);
			leftSubPanel.Margin = new Padding(0, 0, 4, 0);
			leftSubPanel.Name = "leftSubPanel";
			leftSubPanel.RowCount = 3;
			this.mainPanel.SetRowSpan(leftSubPanel, 2);
			leftSubPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
			leftSubPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F));
			leftSubPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
			leftSubPanel.Size = new Size(695, 497);
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
			this.olvFiles.AllColumns.Add(this.olvColumnFilename);
			this.olvFiles.AllColumns.Add(this.olvColumnGit);
			this.olvFiles.AllColumns.Add(this.olvColumnGitName);
			this.olvFiles.AllColumns.Add(this.olvColumnDate);
			this.olvFiles.AllColumns.Add(this.olvColumnVersion);
			this.olvFiles.AllColumns.Add(this.olvColumnOptions);
			this.olvFiles.AllowDrop = true;
			this.olvFiles.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
			this.olvFiles.CellEditUseWholeCell = false;
			this.olvFiles.Columns.AddRange(new ColumnHeader[] { this.olvColumnFilename, this.olvColumnGit, this.olvColumnDate, this.olvColumnVersion, this.olvColumnOptions });
			this.olvFiles.EmptyListMsg = "Add a group, import a folder or import recent items";
			this.olvFiles.EmptyListMsgFont = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point);
			this.olvFiles.FullRowSelect = true;
			this.olvFiles.Location = new Point(0, 75);
			this.olvFiles.Margin = new Padding(0);
			this.olvFiles.MultiSelect = false;
			this.olvFiles.Name = "olvFiles";
			this.olvFiles.SelectColumnsOnRightClickBehaviour = ObjectListView.ColumnSelectBehaviour.Submenu;
			this.olvFiles.ShowCommandMenuOnRightClick = true;
			this.olvFiles.ShowFilterMenuOnRightClick = false;
			this.olvFiles.ShowGroups = false;
			this.olvFiles.ShowItemToolTips = true;
			this.olvFiles.Size = new Size(695, 422);
			this.olvFiles.SmallImageList = this.imageList3;
			this.olvFiles.TabIndex = 1;
			this.olvFiles.UseCompatibleStateImageBehavior = false;
			this.olvFiles.UseFilterIndicator = true;
			this.olvFiles.UseFiltering = true;
			this.olvFiles.UseWaitCursorWhenExpanding = false;
			this.olvFiles.View = View.Details;
			this.olvFiles.VirtualMode = true;
			this.olvFiles.CellRightClick += olvFiles_CellRightClick;
			this.olvFiles.CellToolTipShowing += olvFiles_CellToolTipShowing;
			this.olvFiles.Dropped += olvFiles_Dropped;
			this.olvFiles.HotItemChanged += olvFiles_HotItemChanged;
			this.olvFiles.AfterLabelEdit += olvFiles_AfterLabelEdit;
			this.olvFiles.SelectedIndexChanged += olvFiles_SelectedIndexChanged;
			this.olvFiles.DoubleClick += olvFiles_DoubleClick;
			this.olvFiles.KeyDown += olvFiles_KeyDown;
			this.olvFiles.KeyPress += olvFiles_KeyPress;
			// 
			// olvColumnFilename
			// 
			this.olvColumnFilename.AspectName = "Name";
			this.olvColumnFilename.FillsFreeSpace = true;
			this.olvColumnFilename.Hideable = false;
			this.olvColumnFilename.MinimumWidth = 100;
			this.olvColumnFilename.Text = "Name";
			this.olvColumnFilename.ToolTipText = "";
			this.olvColumnFilename.Width = 140;
			// 
			// olvColumnGit
			// 
			this.olvColumnGit.AspectName = "Git";
			this.olvColumnGit.Groupable = false;
			this.olvColumnGit.IsEditable = false;
			this.olvColumnGit.MaximumWidth = 30;
			this.olvColumnGit.MinimumWidth = 30;
			this.olvColumnGit.Searchable = false;
			this.olvColumnGit.Sortable = false;
			this.olvColumnGit.Text = "Git";
			this.olvColumnGit.ToolTipText = "Git status ";
			this.olvColumnGit.Width = 30;
			// 
			// olvColumnGitName
			// 
			this.olvColumnGitName.AspectName = "GitName";
			this.olvColumnGitName.Groupable = false;
			this.olvColumnGitName.IsEditable = false;
			this.olvColumnGitName.MaximumWidth = 200;
			this.olvColumnGitName.MinimumWidth = 30;
			this.olvColumnGitName.Searchable = false;
			this.olvColumnGitName.Sortable = false;
			this.olvColumnGitName.Text = "Branch";
			this.olvColumnGitName.ToolTipText = "Current Branch Name ";
			this.olvColumnGitName.Width = 100;
			// 
			// olvColumnDate
			// 
			this.olvColumnDate.AspectName = "Date";
			this.olvColumnDate.Groupable = false;
			this.olvColumnDate.IsEditable = false;
			this.olvColumnDate.MaximumWidth = 200;
			this.olvColumnDate.MinimumWidth = 100;
			this.olvColumnDate.Searchable = false;
			this.olvColumnDate.Sortable = false;
			this.olvColumnDate.Text = "Last Modified";
			this.olvColumnDate.ToolTipText = "Time and date last modified ";
			this.olvColumnDate.Width = 140;
			// 
			// olvColumnVersion
			// 
			this.olvColumnVersion.AspectName = "Version";
			this.olvColumnVersion.Groupable = false;
			this.olvColumnVersion.IsEditable = false;
			this.olvColumnVersion.MaximumWidth = 200;
			this.olvColumnVersion.MinimumWidth = 75;
			this.olvColumnVersion.Searchable = false;
			this.olvColumnVersion.Sortable = false;
			this.olvColumnVersion.Text = "Version";
			this.olvColumnVersion.ToolTipText = "Visual Studio Version ";
			this.olvColumnVersion.Width = 75;
			// 
			// olvColumnOptions
			// 
			this.olvColumnOptions.AspectName = "Options";
			this.olvColumnOptions.Groupable = false;
			this.olvColumnOptions.Hideable = false;
			this.olvColumnOptions.IsEditable = false;
			this.olvColumnOptions.MaximumWidth = 100;
			this.olvColumnOptions.MinimumWidth = 80;
			this.olvColumnOptions.Renderer = this.optionsRenderer;
			this.olvColumnOptions.Searchable = false;
			this.olvColumnOptions.Sortable = false;
			this.olvColumnOptions.Text = "Run Options";
			this.olvColumnOptions.ToolTipText = "Blah ";
			this.olvColumnOptions.UseFiltering = false;
			this.olvColumnOptions.Width = 80;
			// 
			// optionsRenderer
			// 
			this.optionsRenderer.ImageName = "star";
			this.optionsRenderer.MaximumValue = 50;
			this.optionsRenderer.MaxNumberImages = 5;
			// 
			// imageList3
			// 
			this.imageList3.ColorDepth = ColorDepth.Depth8Bit;
			this.imageList3.ImageStream = (ImageListStreamer)resources.GetObject("imageList3.ImageStream");
			this.imageList3.TransparentColor = Color.Transparent;
			this.imageList3.Images.SetKeyName(0, "Add");
			this.imageList3.Images.SetKeyName(1, "AddFavorite");
			this.imageList3.Images.SetKeyName(2, "AddFolder");
			this.imageList3.Images.SetKeyName(3, "Group");
			this.imageList3.Images.SetKeyName(4, "ArrowDownEnd");
			this.imageList3.Images.SetKeyName(5, "ArrowUpEnd");
			this.imageList3.Images.SetKeyName(6, "ConnectArrow");
			this.imageList3.Images.SetKeyName(7, "CPPProject");
			this.imageList3.Images.SetKeyName(8, "CSProject");
			this.imageList3.Images.SetKeyName(9, "FolderClosed");
			this.imageList3.Images.SetKeyName(10, "FolderOpen");
			this.imageList3.Images.SetKeyName(11, "FSProject");
			this.imageList3.Images.SetKeyName(12, "OpenProjectFolder");
			this.imageList3.Images.SetKeyName(13, "Refresh");
			this.imageList3.Images.SetKeyName(14, "Run");
			this.imageList3.Images.SetKeyName(15, "RunAll");
			this.imageList3.Images.SetKeyName(16, "RunUpdate");
			this.imageList3.Images.SetKeyName(17, "Settings");
			this.imageList3.Images.SetKeyName(18, "TSProject");
			this.imageList3.Images.SetKeyName(19, "VBProject");
			this.imageList3.Images.SetKeyName(20, "None");
			this.imageList3.Images.SetKeyName(21, "RunAsAdmin");
			this.imageList3.Images.SetKeyName(22, "Application");
			this.imageList3.Images.SetKeyName(23, "ApplicationGroup");
			this.imageList3.Images.SetKeyName(24, "VsSolution");
			this.imageList3.Images.SetKeyName(25, "WebProject");
			this.imageList3.Images.SetKeyName(26, "RunAfter");
			this.imageList3.Images.SetKeyName(27, "RunBefore");
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
			// mainPanel
			// 
			this.mainPanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
			this.mainPanel.ColumnCount = 2;
			this.mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			this.mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 234F));
			this.mainPanel.Controls.Add(leftSubPanel, 0, 0);
			this.mainPanel.Controls.Add(this.flowLayoutPanel1, 1, 0);
			this.mainPanel.Controls.Add(this.btnVsInstaller, 1, 1);
			this.mainPanel.Location = new Point(0, 0);
			this.mainPanel.Margin = new Padding(4, 3, 3, 3);
			this.mainPanel.Name = "mainPanel";
			this.mainPanel.RowCount = 2;
			this.mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
			this.mainPanel.RowStyles.Add(new RowStyle());
			this.mainPanel.Size = new Size(933, 497);
			this.mainPanel.TabIndex = 0;
			this.mainPanel.Resize += mainPanel_Resize;
			// 
			// flowLayoutPanel1
			// 
			this.flowLayoutPanel1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
			this.flowLayoutPanel1.Controls.Add(label2);
			this.flowLayoutPanel1.Controls.Add(this.selectVisualStudioVersion);
			this.flowLayoutPanel1.Controls.Add(this.btnMainStartVisualStudio1);
			this.flowLayoutPanel1.Controls.Add(this.btnMainStartVisualStudio2);
			this.flowLayoutPanel1.Controls.Add(this.btnMainStartVisualStudio3);
			this.flowLayoutPanel1.Controls.Add(this.btnMainStartVisualStudio4);
			this.flowLayoutPanel1.Controls.Add(this.btnMainStartVisualStudio5);
			this.flowLayoutPanel1.Controls.Add(this.btnMainOpenActivityLog);
			this.flowLayoutPanel1.FlowDirection = FlowDirection.TopDown;
			this.flowLayoutPanel1.Location = new Point(699, 0);
			this.flowLayoutPanel1.Margin = new Padding(0);
			this.flowLayoutPanel1.Name = "flowLayoutPanel1";
			this.flowLayoutPanel1.Size = new Size(234, 449);
			this.flowLayoutPanel1.TabIndex = 1;
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
			this.selectVisualStudioVersion.ShowDefault = false;
			this.selectVisualStudioVersion.Size = new Size(232, 34);
			this.selectVisualStudioVersion.TabIndex = 0;
			this.tooltipForButtons.SetToolTip(this.selectVisualStudioVersion, "Visual Studio versions currently installed");
			this.selectVisualStudioVersion.DrawItem += selectVisualStudioVersion_DrawItem;
			this.selectVisualStudioVersion.SelectedIndexChanged += selectVisualStudioVersion_SelectedIndexChanged;
			// 
			// btnMainStartVisualStudio1
			// 
			this.btnMainStartVisualStudio1.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
			this.btnMainStartVisualStudio1.Image = (Image)resources.GetObject("btnMainStartVisualStudio1.Image");
			this.btnMainStartVisualStudio1.ImageAlign = ContentAlignment.MiddleLeft;
			this.btnMainStartVisualStudio1.Location = new Point(0, 80);
			this.btnMainStartVisualStudio1.Margin = new Padding(0);
			this.btnMainStartVisualStudio1.Name = "btnMainStartVisualStudio1";
			this.btnMainStartVisualStudio1.Padding = new Padding(4);
			this.btnMainStartVisualStudio1.Size = new Size(232, 64);
			this.btnMainStartVisualStudio1.TabIndex = 1;
			this.btnMainStartVisualStudio1.Tag = "Start {0}";
			this.btnMainStartVisualStudio1.Text = "Start Visual Studio";
			this.btnMainStartVisualStudio1.TextAlign = ContentAlignment.MiddleLeft;
			this.btnMainStartVisualStudio1.TextImageRelation = TextImageRelation.ImageBeforeText;
			this.tooltipForButtons.SetToolTip(this.btnMainStartVisualStudio1, "Test");
			this.btnMainStartVisualStudio1.UseVisualStyleBackColor = true;
			this.btnMainStartVisualStudio1.Click += btnMainStartVisualStudio1_Click;
			// 
			// btnMainStartVisualStudio2
			// 
			this.btnMainStartVisualStudio2.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
			this.btnMainStartVisualStudio2.Image = (Image)resources.GetObject("btnMainStartVisualStudio2.Image");
			this.btnMainStartVisualStudio2.ImageAlign = ContentAlignment.MiddleLeft;
			this.btnMainStartVisualStudio2.Location = new Point(0, 144);
			this.btnMainStartVisualStudio2.Margin = new Padding(0);
			this.btnMainStartVisualStudio2.Name = "btnMainStartVisualStudio2";
			this.btnMainStartVisualStudio2.Padding = new Padding(4);
			this.btnMainStartVisualStudio2.Size = new Size(232, 64);
			this.btnMainStartVisualStudio2.TabIndex = 2;
			this.btnMainStartVisualStudio2.Tag = "Start {0} as Admin";
			this.btnMainStartVisualStudio2.Text = "Start Visual Studio \r\nas Admin";
			this.btnMainStartVisualStudio2.TextAlign = ContentAlignment.MiddleLeft;
			this.btnMainStartVisualStudio2.TextImageRelation = TextImageRelation.ImageBeforeText;
			this.tooltipForButtons.SetToolTip(this.btnMainStartVisualStudio2, "Test");
			this.btnMainStartVisualStudio2.UseVisualStyleBackColor = true;
			this.btnMainStartVisualStudio2.Click += btnMainStartVisualStudio2_Click;
			// 
			// btnMainStartVisualStudio3
			// 
			this.btnMainStartVisualStudio3.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
			this.btnMainStartVisualStudio3.Image = (Image)resources.GetObject("btnMainStartVisualStudio3.Image");
			this.btnMainStartVisualStudio3.ImageAlign = ContentAlignment.MiddleLeft;
			this.btnMainStartVisualStudio3.Location = new Point(0, 208);
			this.btnMainStartVisualStudio3.Margin = new Padding(0);
			this.btnMainStartVisualStudio3.Name = "btnMainStartVisualStudio3";
			this.btnMainStartVisualStudio3.Padding = new Padding(4);
			this.btnMainStartVisualStudio3.Size = new Size(232, 64);
			this.btnMainStartVisualStudio3.TabIndex = 3;
			this.btnMainStartVisualStudio3.Tag = "New {0} Instance...";
			this.btnMainStartVisualStudio3.Text = "New Instance...";
			this.btnMainStartVisualStudio3.TextAlign = ContentAlignment.MiddleLeft;
			this.btnMainStartVisualStudio3.TextImageRelation = TextImageRelation.ImageBeforeText;
			this.tooltipForButtons.SetToolTip(this.btnMainStartVisualStudio3, "Test");
			this.btnMainStartVisualStudio3.UseVisualStyleBackColor = true;
			this.btnMainStartVisualStudio3.Click += btnMainStartVisualStudio3_Click;
			// 
			// btnMainStartVisualStudio4
			// 
			this.btnMainStartVisualStudio4.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
			this.btnMainStartVisualStudio4.Image = (Image)resources.GetObject("btnMainStartVisualStudio4.Image");
			this.btnMainStartVisualStudio4.ImageAlign = ContentAlignment.MiddleLeft;
			this.btnMainStartVisualStudio4.Location = new Point(0, 272);
			this.btnMainStartVisualStudio4.Margin = new Padding(0);
			this.btnMainStartVisualStudio4.Name = "btnMainStartVisualStudio4";
			this.btnMainStartVisualStudio4.Padding = new Padding(4);
			this.btnMainStartVisualStudio4.Size = new Size(232, 64);
			this.btnMainStartVisualStudio4.TabIndex = 4;
			this.btnMainStartVisualStudio4.Tag = "New {0} Project...";
			this.btnMainStartVisualStudio4.Text = "New Project...";
			this.btnMainStartVisualStudio4.TextAlign = ContentAlignment.MiddleLeft;
			this.btnMainStartVisualStudio4.TextImageRelation = TextImageRelation.ImageBeforeText;
			this.tooltipForButtons.SetToolTip(this.btnMainStartVisualStudio4, "Test");
			this.btnMainStartVisualStudio4.UseVisualStyleBackColor = true;
			this.btnMainStartVisualStudio4.Click += btnMainStartVisualStudio4_Click;
			// 
			// btnMainStartVisualStudio5
			// 
			this.btnMainStartVisualStudio5.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
			this.btnMainStartVisualStudio5.Image = (Image)resources.GetObject("btnMainStartVisualStudio5.Image");
			this.btnMainStartVisualStudio5.ImageAlign = ContentAlignment.MiddleLeft;
			this.btnMainStartVisualStudio5.Location = new Point(0, 336);
			this.btnMainStartVisualStudio5.Margin = new Padding(0);
			this.btnMainStartVisualStudio5.Name = "btnMainStartVisualStudio5";
			this.btnMainStartVisualStudio5.Padding = new Padding(4);
			this.btnMainStartVisualStudio5.Size = new Size(232, 64);
			this.btnMainStartVisualStudio5.TabIndex = 5;
			this.btnMainStartVisualStudio5.Tag = "Start {0}...";
			this.btnMainStartVisualStudio5.Text = "Start...";
			this.btnMainStartVisualStudio5.TextAlign = ContentAlignment.MiddleLeft;
			this.btnMainStartVisualStudio5.TextImageRelation = TextImageRelation.ImageBeforeText;
			this.tooltipForButtons.SetToolTip(this.btnMainStartVisualStudio5, "Test");
			this.btnMainStartVisualStudio5.UseVisualStyleBackColor = true;
			this.btnMainStartVisualStudio5.Click += btnMainStartVisualStudio5_Click;
			// 
			// btnMainOpenActivityLog
			// 
			this.btnMainOpenActivityLog.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
			this.btnMainOpenActivityLog.ImageAlign = ContentAlignment.MiddleLeft;
			this.btnMainOpenActivityLog.Location = new Point(0, 400);
			this.btnMainOpenActivityLog.Margin = new Padding(0);
			this.btnMainOpenActivityLog.Name = "btnMainOpenActivityLog";
			this.btnMainOpenActivityLog.Padding = new Padding(4);
			this.btnMainOpenActivityLog.Size = new Size(232, 32);
			this.btnMainOpenActivityLog.TabIndex = 6;
			this.btnMainOpenActivityLog.Tag = "Open {0} ActivityLog";
			this.btnMainOpenActivityLog.Text = "Open ActivityLog";
			this.btnMainOpenActivityLog.TextImageRelation = TextImageRelation.ImageBeforeText;
			this.tooltipForButtons.SetToolTip(this.btnMainOpenActivityLog, "Opens the activity log file of Visual Studio");
			this.btnMainOpenActivityLog.UseMnemonic = false;
			this.btnMainOpenActivityLog.UseVisualStyleBackColor = true;
			this.btnMainOpenActivityLog.Click += btnMainOpenActivityLog_Click;
			// 
			// btnVsInstaller
			// 
			this.btnVsInstaller.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
			this.btnVsInstaller.Image = Resources.ImportVS1;
			this.btnVsInstaller.ImageAlign = ContentAlignment.MiddleLeft;
			this.btnVsInstaller.Location = new Point(699, 449);
			this.btnVsInstaller.Margin = new Padding(0);
			this.btnVsInstaller.Name = "btnVsInstaller";
			this.btnVsInstaller.Padding = new Padding(4, 0, 4, 0);
			this.btnVsInstaller.Size = new Size(232, 48);
			this.btnVsInstaller.TabIndex = 5;
			this.btnVsInstaller.Tag = "";
			this.btnVsInstaller.Text = "Visual Studio Installer";
			this.btnVsInstaller.TextAlign = ContentAlignment.MiddleLeft;
			this.btnVsInstaller.TextImageRelation = TextImageRelation.ImageBeforeText;
			this.btnVsInstaller.UseVisualStyleBackColor = true;
			this.btnVsInstaller.Click += btnVsInstaller_Click;
			// 
			// imageListMainIcons
			// 
			this.imageListMainIcons.ColorDepth = ColorDepth.Depth8Bit;
			this.imageListMainIcons.ImageStream = (ImageListStreamer)resources.GetObject("imageListMainIcons.ImageStream");
			this.imageListMainIcons.TransparentColor = Color.Transparent;
			this.imageListMainIcons.Images.SetKeyName(0, "CPPProject");
			this.imageListMainIcons.Images.SetKeyName(1, "CSProject");
			this.imageListMainIcons.Images.SetKeyName(2, "FolderClosed");
			this.imageListMainIcons.Images.SetKeyName(3, "FolderOpen");
			this.imageListMainIcons.Images.SetKeyName(4, "FSProject");
			this.imageListMainIcons.Images.SetKeyName(5, "TSProject");
			this.imageListMainIcons.Images.SetKeyName(6, "VBProject");
			this.imageListMainIcons.Images.SetKeyName(7, "Application");
			this.imageListMainIcons.Images.SetKeyName(8, "ApplicationGroup");
			this.imageListMainIcons.Images.SetKeyName(9, "VsSolution");
			// 
			// toolStripStatusLabel3
			// 
			this.toolStripStatusLabel3.Name = "toolStripStatusLabel3";
			this.toolStripStatusLabel3.Size = new Size(829, 17);
			this.toolStripStatusLabel3.Spring = true;
			this.toolStripStatusLabel3.Text = "toolStripStatusLabel3";
			this.toolStripStatusLabel3.TextAlign = ContentAlignment.MiddleRight;
			// 
			// ctxMenu
			// 
			this.ctxMenu.Items.AddRange(new ToolStripItem[] { this.gitToolStripMenuItem, this.addToolStripMenuItem, this.toolStripMenuItem3, this.runToolStripMenuItem, this.runAsAdminToolStripMenuItem, this.renameToolStripMenuItem, this.toolStripSeparator1, this.settingsToolStripMenuItem, this.toolStripSeparator2, this.removeToolStripMenuItem, this.toolStripMenuItem4, this.favoriteToolStripMenuItem, this.explorerToolStripMenuItem });
			this.ctxMenu.Name = "ctxMenu";
			this.ctxMenu.Size = new Size(209, 248);
			this.ctxMenu.Opening += ctxMenu_Opening;
			// 
			// addToolStripMenuItem
			// 
			this.addToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { this.newGroupToolStripMenuItem, this.fromFolderToolStripMenuItem, this.solutionProjectToolStripMenuItem });
			this.addToolStripMenuItem.Name = "addToolStripMenuItem";
			this.addToolStripMenuItem.Size = new Size(208, 22);
			this.addToolStripMenuItem.Text = "Add...";
			// 
			// newGroupToolStripMenuItem
			// 
			this.newGroupToolStripMenuItem.Name = "newGroupToolStripMenuItem";
			this.newGroupToolStripMenuItem.Size = new Size(169, 22);
			this.newGroupToolStripMenuItem.Text = "New Group...";
			this.newGroupToolStripMenuItem.Click += newGroupToolStripMenuItem_Click;
			// 
			// fromFolderToolStripMenuItem
			// 
			this.fromFolderToolStripMenuItem.Name = "fromFolderToolStripMenuItem";
			this.fromFolderToolStripMenuItem.Size = new Size(169, 22);
			this.fromFolderToolStripMenuItem.Text = "From Folder...";
			this.fromFolderToolStripMenuItem.Click += fromFolderToolStripMenuItem_Click;
			// 
			// solutionProjectToolStripMenuItem
			// 
			this.solutionProjectToolStripMenuItem.Name = "solutionProjectToolStripMenuItem";
			this.solutionProjectToolStripMenuItem.Size = new Size(169, 22);
			this.solutionProjectToolStripMenuItem.Text = "Solution/Project...";
			this.solutionProjectToolStripMenuItem.Click += solutionProjectToolStripMenuItem_Click;
			// 
			// toolStripMenuItem3
			// 
			this.toolStripMenuItem3.Name = "toolStripMenuItem3";
			this.toolStripMenuItem3.Size = new Size(205, 6);
			// 
			// runToolStripMenuItem
			// 
			this.runToolStripMenuItem.Name = "runToolStripMenuItem";
			this.runToolStripMenuItem.ShortcutKeys = Keys.F5;
			this.runToolStripMenuItem.Size = new Size(208, 22);
			this.runToolStripMenuItem.Text = "Run";
			this.runToolStripMenuItem.Click += runToolStripMenuItem_Click;
			// 
			// runAsAdminToolStripMenuItem
			// 
			this.runAsAdminToolStripMenuItem.Name = "runAsAdminToolStripMenuItem";
			this.runAsAdminToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.F5;
			this.runAsAdminToolStripMenuItem.Size = new Size(208, 22);
			this.runAsAdminToolStripMenuItem.Text = "Run as Admin";
			this.runAsAdminToolStripMenuItem.Click += runAsAdminToolStripMenuItem_Click;
			// 
			// renameToolStripMenuItem
			// 
			this.renameToolStripMenuItem.Name = "renameToolStripMenuItem";
			this.renameToolStripMenuItem.Size = new Size(208, 22);
			this.renameToolStripMenuItem.Text = "Rename...";
			this.renameToolStripMenuItem.Click += renameToolStripMenuItem_Click;
			// 
			// settingsToolStripMenuItem
			// 
			this.settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
			this.settingsToolStripMenuItem.ShortcutKeys = Keys.Alt | Keys.Return;
			this.settingsToolStripMenuItem.Size = new Size(208, 22);
			this.settingsToolStripMenuItem.Text = "Settings...";
			this.settingsToolStripMenuItem.Click += settingsToolStripMenuItem_Click;
			// 
			// removeToolStripMenuItem
			// 
			this.removeToolStripMenuItem.Name = "removeToolStripMenuItem";
			this.removeToolStripMenuItem.ShortcutKeys = Keys.Shift | Keys.Delete;
			this.removeToolStripMenuItem.Size = new Size(208, 22);
			this.removeToolStripMenuItem.Text = "Remove...";
			this.removeToolStripMenuItem.Click += removeToolStripMenuItem_Click;
			// 
			// toolStripMenuItem4
			// 
			this.toolStripMenuItem4.Name = "toolStripMenuItem4";
			this.toolStripMenuItem4.Size = new Size(205, 6);
			// 
			// favoriteToolStripMenuItem
			// 
			this.favoriteToolStripMenuItem.Name = "favoriteToolStripMenuItem";
			this.favoriteToolStripMenuItem.Size = new Size(208, 22);
			this.favoriteToolStripMenuItem.Text = "Favorite";
			this.favoriteToolStripMenuItem.Click += favoriteToolStripMenuItem_Click;
			// 
			// explorerToolStripMenuItem
			// 
			this.explorerToolStripMenuItem.Name = "explorerToolStripMenuItem";
			this.explorerToolStripMenuItem.Size = new Size(208, 22);
			this.explorerToolStripMenuItem.Text = "Open location in Explorer";
			this.explorerToolStripMenuItem.Click += explorerToolStripMenuItem_Click;
			// 
			// gitToolStripMenuItem
			// 
			this.gitToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { this.branchesToolStripMenuItem, this.toolStripMenuItem1, this.fetchToolStripMenuItem, this.pullToolStripMenuItem });
			this.gitToolStripMenuItem.Name = "gitToolStripMenuItem";
			this.gitToolStripMenuItem.Size = new Size(208, 22);
			this.gitToolStripMenuItem.Text = "Git";
			// 
			// fetchToolStripMenuItem
			// 
			this.fetchToolStripMenuItem.Name = "fetchToolStripMenuItem";
			this.fetchToolStripMenuItem.Size = new Size(180, 22);
			this.fetchToolStripMenuItem.Text = "Fetch";
			this.fetchToolStripMenuItem.Click += fetchToolStripMenuItem_Click;
			// 
			// pullToolStripMenuItem
			// 
			this.pullToolStripMenuItem.Name = "pullToolStripMenuItem";
			this.pullToolStripMenuItem.Size = new Size(180, 22);
			this.pullToolStripMenuItem.Text = "Pull";
			this.pullToolStripMenuItem.Click += pullToolStripMenuItem_Click;
			// 
			// statusStrip1
			// 
			this.statusStrip1.Items.AddRange(new ToolStripItem[] { this.mainStatusLabel, this.toolStripStatusGit });
			this.statusStrip1.Location = new Point(0, 497);
			this.statusStrip1.Name = "statusStrip1";
			this.statusStrip1.Size = new Size(933, 22);
			this.statusStrip1.TabIndex = 1;
			this.statusStrip1.Text = "statusStrip1";
			// 
			// mainStatusLabel
			// 
			this.mainStatusLabel.Name = "mainStatusLabel";
			this.mainStatusLabel.Size = new Size(896, 17);
			this.mainStatusLabel.Spring = true;
			this.mainStatusLabel.Text = "Lets do something incredible today";
			this.mainStatusLabel.TextAlign = ContentAlignment.MiddleLeft;
			// 
			// toolStripStatusGit
			// 
			this.toolStripStatusGit.Name = "toolStripStatusGit";
			this.toolStripStatusGit.Size = new Size(22, 17);
			this.toolStripStatusGit.Text = "Git";
			// 
			// gitTimer
			// 
			this.gitTimer.Interval = 1000;
			// 
			// branchesToolStripMenuItem
			// 
			this.branchesToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { this.xToolStripMenuItem });
			this.branchesToolStripMenuItem.Name = "branchesToolStripMenuItem";
			this.branchesToolStripMenuItem.Size = new Size(180, 22);
			this.branchesToolStripMenuItem.Text = "Branches";
			// 
			// toolStripMenuItem1
			// 
			this.toolStripMenuItem1.Name = "toolStripMenuItem1";
			this.toolStripMenuItem1.Size = new Size(177, 6);
			// 
			// xToolStripMenuItem
			// 
			this.xToolStripMenuItem.Name = "xToolStripMenuItem";
			this.xToolStripMenuItem.Size = new Size(180, 22);
			this.xToolStripMenuItem.Text = "x";
			// 
			// MainDialog
			// 
			this.AutoScaleDimensions = new SizeF(7F, 15F);
			this.AutoScaleMode = AutoScaleMode.Font;
			this.ClientSize = new Size(933, 519);
			this.Controls.Add(this.statusStrip1);
			this.Controls.Add(this.mainPanel);
			this.Icon = (Icon)resources.GetObject("$this.Icon");
			this.Margin = new Padding(4, 3, 4, 3);
			this.Name = "MainDialog";
			this.Text = "Visual Studio Launcher";
			FormClosing += MainDialog_FormClosing;
			Load += MainDialog_Load;
			flowLayoutPanel2.ResumeLayout(false);
			flowLayoutPanel2.PerformLayout();
			leftSubPanel.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)this.olvFiles).EndInit();
			this.mainPanel.ResumeLayout(false);
			this.flowLayoutPanel1.ResumeLayout(false);
			this.flowLayoutPanel1.PerformLayout();
			this.ctxMenu.ResumeLayout(false);
			this.statusStrip1.ResumeLayout(false);
			this.statusStrip1.PerformLayout();
			ResumeLayout(false);
			PerformLayout();
		}

		#endregion
		private TreeListView olvFiles;
		private OLVColumn olvColumnFilename;
		private OLVColumn olvColumnGit;
		private OLVColumn olvColumnGitName;
		private OLVColumn olvColumnDate;
		private OLVColumn olvColumnVersion;
		private OLVColumn olvColumnOptions;

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
		private ToolStripMenuItem removeToolStripMenuItem;
		private TextBoxEx txtFilter;
		private Button btnAddFolder;
		private Button btnExplorer;
		private Button btnImportFolder;
		private Button btnImportVS;
		private Button btnRefresh;
		private Button btnSettings;
		private Label spacer1;
		private Label spacer2;
		private ToolStripSeparator toolStripMenuItem3;
		private ToolStripMenuItem addToolStripMenuItem;
		private ToolStripMenuItem newGroupToolStripMenuItem;
		private ToolStripMenuItem fromFolderToolStripMenuItem;
		private ToolStripMenuItem solutionProjectToolStripMenuItem;
		private ToolStripSeparator toolStripMenuItem4;
		private ToolStripMenuItem favoriteToolStripMenuItem;
		private ToolStripMenuItem explorerToolStripMenuItem;
		private Button btnImportSoP;
		private Button btnVsInstaller;
		private System.Windows.Forms.Timer gitTimer;
		private Button btnMainOpenActivityLog;
		private ToolStripStatusLabel toolStripStatusGit;
		private ToolStripMenuItem gitToolStripMenuItem;
		private ToolStripMenuItem fetchToolStripMenuItem;
		private ToolStripMenuItem pullToolStripMenuItem;
		private ToolStripSeparator toolStripSeparator1;
		private ToolStripSeparator toolStripSeparator2;
		private ToolStripMenuItem branchesToolStripMenuItem;
		private ToolStripMenuItem xToolStripMenuItem;
		private ToolStripSeparator toolStripMenuItem1;
	}
}

