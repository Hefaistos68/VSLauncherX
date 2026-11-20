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
			components = new System.ComponentModel.Container();
			FlowLayoutPanel flowLayoutPanel2;
			Label label3;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainDialog));
			TableLayoutPanel leftSubPanel;
			Label titleLabel;
			Label label2;
			Label lblVsCodeEdition;
			txtFilter = new TextBoxEx();
			btnAddFolder = new Button();
			btnImportFolder = new Button();
			btnImportSoP = new Button();
			btnImportVS = new Button();
			btnRefresh = new Button();
			spacer1 = new Label();
			btnExplorer = new Button();
			spacer2 = new Label();
			btnSettings = new Button();
			olvFiles = new TreeListView();
			olvColumnFilename = new OLVColumn();
			olvColumnGit = new OLVColumn();
			olvColumnGitName = new OLVColumn();
			olvColumnDate = new OLVColumn();
			olvColumnVersion = new OLVColumn();
			olvColumnOptions = new OLVColumn();
			optionsRenderer = new MultiImageRenderer();
			imageList3 = new ImageList(components);
			toolStripSeparator1 = new ToolStripSeparator();
			toolStripSeparator2 = new ToolStripSeparator();
			mainPanel = new TableLayoutPanel();
			tabControlIdes = new TabControl();
			tabVisualStudio = new TabPage();
			flowLayoutPanel1 = new FlowLayoutPanel();
			selectVisualStudioVersion = new VisualStudioCombobox();
			btnMainStartVisualStudio1 = new Button();
			btnMainStartVisualStudio2 = new Button();
			btnMainStartVisualStudio3 = new Button();
			btnMainStartVisualStudio4 = new Button();
			btnMainStartVisualStudio5 = new Button();
			btnMainOpenActivityLog = new Button();
			btnVsInstaller = new Button();
			tabVsCode = new TabPage();
			flowLayoutPanelVsCode = new FlowLayoutPanel();
			selectVsCodeEdition = new IdeCombobox();
			btnVsCodeStart = new Button();
			btnVsCodeStartAdmin = new Button();
			btnVsCodeStartNewWindow = new Button();
			btnVsCodeStartNoExt = new Button();
			btnVsCodeOpenActivityLog = new Button();
			btnVsCodeDownload = new Button();
			imageListMainIcons = new ImageList(components);
			toolStripStatusLabel3 = new ToolStripStatusLabel();
			tooltipForButtons = new ToolTip(components);
			ctxMenu = new ContextMenuStrip(components);
			gitToolStripMenuItem = new ToolStripMenuItem();
			branchesToolStripMenuItem = new ToolStripMenuItem();
			xToolStripMenuItem = new ToolStripMenuItem();
			toolStripMenuItem1 = new ToolStripSeparator();
			fetchToolStripMenuItem = new ToolStripMenuItem();
			pullToolStripMenuItem = new ToolStripMenuItem();
			addToolStripMenuItem = new ToolStripMenuItem();
			newGroupToolStripMenuItem = new ToolStripMenuItem();
			fromFolderToolStripMenuItem = new ToolStripMenuItem();
			solutionProjectToolStripMenuItem = new ToolStripMenuItem();
			toolStripMenuItem3 = new ToolStripSeparator();
			runToolStripMenuItem = new ToolStripMenuItem();
			runAsAdminToolStripMenuItem = new ToolStripMenuItem();
			renameToolStripMenuItem = new ToolStripMenuItem();
			settingsToolStripMenuItem = new ToolStripMenuItem();
			removeToolStripMenuItem = new ToolStripMenuItem();
			toolStripMenuItem4 = new ToolStripSeparator();
			favoriteToolStripMenuItem = new ToolStripMenuItem();
			explorerToolStripMenuItem = new ToolStripMenuItem();
			openInVsCodeToolStripMenuItem = new ToolStripMenuItem();
			statusStrip1 = new StatusStrip();
			mainStatusLabel = new ToolStripStatusLabel();
			toolStripStatusGit = new ToolStripStatusLabel();
			gitTimer = new System.Windows.Forms.Timer(components);
			flowLayoutPanel2 = new FlowLayoutPanel();
			label3 = new Label();
			leftSubPanel = new TableLayoutPanel();
			titleLabel = new Label();
			label2 = new Label();
			lblVsCodeEdition = new Label();
			flowLayoutPanel2.SuspendLayout();
			leftSubPanel.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)olvFiles).BeginInit();
			mainPanel.SuspendLayout();
			tabControlIdes.SuspendLayout();
			tabVisualStudio.SuspendLayout();
			flowLayoutPanel1.SuspendLayout();
			tabVsCode.SuspendLayout();
			flowLayoutPanelVsCode.SuspendLayout();
			ctxMenu.SuspendLayout();
			statusStrip1.SuspendLayout();
			SuspendLayout();
			// 
			// flowLayoutPanel2
			// 
			flowLayoutPanel2.BackColor = SystemColors.ScrollBar;
			flowLayoutPanel2.Controls.Add(label3);
			flowLayoutPanel2.Controls.Add(txtFilter);
			flowLayoutPanel2.Controls.Add(btnAddFolder);
			flowLayoutPanel2.Controls.Add(btnImportFolder);
			flowLayoutPanel2.Controls.Add(btnImportSoP);
			flowLayoutPanel2.Controls.Add(btnImportVS);
			flowLayoutPanel2.Controls.Add(btnRefresh);
			flowLayoutPanel2.Controls.Add(spacer1);
			flowLayoutPanel2.Controls.Add(btnExplorer);
			flowLayoutPanel2.Controls.Add(spacer2);
			flowLayoutPanel2.Controls.Add(btnSettings);
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
			txtFilter.Font = new Font("Segoe UI", 12F);
			txtFilter.Location = new Point(67, 3);
			txtFilter.Name = "txtFilter";
			txtFilter.ShowClearButton = true;
			txtFilter.Size = new Size(364, 29);
			txtFilter.TabIndex = 1;
			txtFilter.TextChanged += txtFilter_TextChanged;
			txtFilter.KeyPress += txtFilter_KeyPress;
			// 
			// btnAddFolder
			// 
			btnAddFolder.Image = (Image)resources.GetObject("btnAddFolder.Image");
			btnAddFolder.Location = new Point(435, 1);
			btnAddFolder.Margin = new Padding(1);
			btnAddFolder.Name = "btnAddFolder";
			btnAddFolder.Size = new Size(32, 32);
			btnAddFolder.TabIndex = 2;
			tooltipForButtons.SetToolTip(btnAddFolder, "Add new Group");
			btnAddFolder.UseVisualStyleBackColor = true;
			btnAddFolder.Click += mainFolderAdd_Click;
			// 
			// btnImportFolder
			// 
			btnImportFolder.Image = (Image)resources.GetObject("btnImportFolder.Image");
			btnImportFolder.Location = new Point(469, 1);
			btnImportFolder.Margin = new Padding(1);
			btnImportFolder.Name = "btnImportFolder";
			btnImportFolder.Size = new Size(32, 32);
			btnImportFolder.TabIndex = 2;
			tooltipForButtons.SetToolTip(btnImportFolder, "Import from Folder");
			btnImportFolder.UseVisualStyleBackColor = true;
			btnImportFolder.Click += mainImportFolder_Click;
			// 
			// btnImportSoP
			// 
			btnImportSoP.Image = (Image)resources.GetObject("btnImportSoP.Image");
			btnImportSoP.Location = new Point(503, 1);
			btnImportSoP.Margin = new Padding(1);
			btnImportSoP.Name = "btnImportSoP";
			btnImportSoP.Size = new Size(32, 32);
			btnImportSoP.TabIndex = 2;
			tooltipForButtons.SetToolTip(btnImportSoP, "Add a solution or project");
			btnImportSoP.UseVisualStyleBackColor = true;
			btnImportSoP.Click += mainImportSoP_Click;
			// 
			// btnImportVS
			// 
			btnImportVS.Image = (Image)resources.GetObject("btnImportVS.Image");
			btnImportVS.Location = new Point(537, 1);
			btnImportVS.Margin = new Padding(1);
			btnImportVS.Name = "btnImportVS";
			btnImportVS.Size = new Size(32, 32);
			btnImportVS.TabIndex = 2;
			tooltipForButtons.SetToolTip(btnImportVS, "Import Recent Projects");
			btnImportVS.UseVisualStyleBackColor = true;
			btnImportVS.Click += mainImportVS_Click;
			// 
			// btnRefresh
			// 
			btnRefresh.Image = (Image)resources.GetObject("btnRefresh.Image");
			btnRefresh.Location = new Point(571, 1);
			btnRefresh.Margin = new Padding(1);
			btnRefresh.Name = "btnRefresh";
			btnRefresh.Size = new Size(32, 32);
			btnRefresh.TabIndex = 2;
			tooltipForButtons.SetToolTip(btnRefresh, "Refresh & Sync");
			btnRefresh.UseVisualStyleBackColor = true;
			btnRefresh.Click += mainRefresh_Click;
			// 
			// spacer1
			// 
			spacer1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom;
			spacer1.Location = new Point(607, 0);
			spacer1.Name = "spacer1";
			spacer1.Size = new Size(4, 35);
			spacer1.TabIndex = 0;
			spacer1.Text = " ";
			spacer1.TextAlign = ContentAlignment.MiddleRight;
			// 
			// btnExplorer
			// 
			btnExplorer.Image = (Image)resources.GetObject("btnExplorer.Image");
			btnExplorer.Location = new Point(615, 1);
			btnExplorer.Margin = new Padding(1);
			btnExplorer.Name = "btnExplorer";
			btnExplorer.Size = new Size(32, 32);
			btnExplorer.TabIndex = 2;
			tooltipForButtons.SetToolTip(btnExplorer, "Open in Explorer");
			btnExplorer.UseVisualStyleBackColor = true;
			btnExplorer.Click += mainOpenInExplorer_Click;
			// 
			// spacer2
			// 
			spacer2.Anchor = AnchorStyles.Top | AnchorStyles.Bottom;
			spacer2.Location = new Point(651, 0);
			spacer2.Name = "spacer2";
			spacer2.Size = new Size(4, 35);
			spacer2.TabIndex = 0;
			spacer2.Text = " ";
			spacer2.TextAlign = ContentAlignment.MiddleRight;
			// 
			// btnSettings
			// 
			btnSettings.Image = (Image)resources.GetObject("btnSettings.Image");
			btnSettings.Location = new Point(659, 1);
			btnSettings.Margin = new Padding(1);
			btnSettings.Name = "btnSettings";
			btnSettings.Size = new Size(32, 32);
			btnSettings.TabIndex = 2;
			tooltipForButtons.SetToolTip(btnSettings, "Application Settings");
			btnSettings.UseVisualStyleBackColor = true;
			btnSettings.Click += mainSettings_Click;
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
			mainPanel.SetRowSpan(leftSubPanel, 2);
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
			titleLabel.Font = new Font("Verdana", 18F, FontStyle.Bold);
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
			this.olvFiles.HotItemChanged += olvFiles_HotItemChanged;
			this.olvFiles.AfterLabelEdit += olvFiles_AfterLabelEdit;
			this.olvFiles.SelectedIndexChanged += olvFiles_SelectedIndexChanged;
			this.olvFiles.DoubleClick += olvFiles_DoubleClick;
			this.olvFiles.KeyDown += olvFiles_KeyDown;
			this.olvFiles.KeyPress += olvFiles_KeyPress;
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
			// olvColumnGit
			// 
			olvColumnGit.AspectName = "Git";
			olvColumnGit.Groupable = false;
			olvColumnGit.IsEditable = false;
			olvColumnGit.MaximumWidth = 30;
			olvColumnGit.MinimumWidth = 30;
			olvColumnGit.Searchable = false;
			olvColumnGit.Sortable = false;
			olvColumnGit.Text = "Git";
			olvColumnGit.ToolTipText = "Git status ";
			olvColumnGit.Width = 30;
			// 
			// olvColumnGitName
			// 
			olvColumnGitName.AspectName = "GitName";
			olvColumnGitName.Groupable = false;
			olvColumnGitName.IsEditable = false;
			olvColumnGitName.MaximumWidth = 200;
			olvColumnGitName.MinimumWidth = 30;
			olvColumnGitName.Searchable = false;
			olvColumnGitName.Sortable = false;
			olvColumnGitName.Text = "Branch";
			olvColumnGitName.ToolTipText = "Current Branch Name ";
			olvColumnGitName.Width = 100;
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
			olvColumnDate.ToolTipText = "Time and date last modified ";
			olvColumnDate.Width = 140;
			// 
			// olvColumnVersion
			// 
			olvColumnVersion.AspectName = "Version";
			olvColumnVersion.Groupable = false;
			olvColumnVersion.IsEditable = false;
			olvColumnVersion.MaximumWidth = 200;
			olvColumnVersion.MinimumWidth = 75;
			olvColumnVersion.Searchable = false;
			olvColumnVersion.Sortable = false;
			olvColumnVersion.Text = "Version";
			olvColumnVersion.ToolTipText = "Visual Studio Version ";
			olvColumnVersion.Width = 75;
			// 
			// olvColumnOptions
			// 
			olvColumnOptions.AspectName = "Options";
			olvColumnOptions.Groupable = false;
			olvColumnOptions.Hideable = false;
			olvColumnOptions.IsEditable = false;
			olvColumnOptions.MaximumWidth = 100;
			olvColumnOptions.MinimumWidth = 80;
			olvColumnOptions.Renderer = optionsRenderer;
			olvColumnOptions.Searchable = false;
			olvColumnOptions.Sortable = false;
			olvColumnOptions.Text = "Run Options";
			olvColumnOptions.ToolTipText = "Blah ";
			olvColumnOptions.UseFiltering = false;
			olvColumnOptions.Width = 80;
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
			// label2
			// 
			label2.AutoSize = true;
			label2.Location = new Point(3, 6);
			label2.Margin = new Padding(3, 6, 3, 0);
			label2.Name = "label2";
			label2.Size = new Size(119, 15);
			label2.TabIndex = 6;
			label2.Text = "Visual Studio version:";
			// 
			// lblVsCodeEdition
			// 
			lblVsCodeEdition.AutoSize = true;
			lblVsCodeEdition.Location = new Point(3, 6);
			lblVsCodeEdition.Margin = new Padding(3, 6, 3, 0);
			lblVsCodeEdition.Name = "lblVsCodeEdition";
			lblVsCodeEdition.Size = new Size(94, 15);
			lblVsCodeEdition.TabIndex = 0;
			lblVsCodeEdition.Text = "VS Code edition:";
			// 
			// toolStripSeparator1
			// 
			toolStripSeparator1.Name = "toolStripSeparator1";
			toolStripSeparator1.Size = new Size(205, 6);
			// 
			// toolStripSeparator2
			// 
			toolStripSeparator2.Name = "toolStripSeparator2";
			toolStripSeparator2.Size = new Size(205, 6);
			// 
			// mainPanel
			// 
			mainPanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
			mainPanel.ColumnCount = 2;
			mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 250F));
			mainPanel.Controls.Add(leftSubPanel, 0, 0);
			mainPanel.Controls.Add(tabControlIdes, 1, 0);
			mainPanel.Location = new Point(0, 0);
			mainPanel.Margin = new Padding(4, 3, 3, 3);
			mainPanel.Name = "mainPanel";
			mainPanel.RowCount = 2;
			mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
			mainPanel.RowStyles.Add(new RowStyle());
			mainPanel.SetRowSpan(tabControlIdes, 2);
			mainPanel.Size = new Size(949, 497);
			mainPanel.TabIndex = 0;
			mainPanel.Resize += mainPanel_Resize;
			// 
			// tabControlIdes
			// 
			tabControlIdes.Controls.Add(tabVisualStudio);
			tabControlIdes.Controls.Add(tabVsCode);
			tabControlIdes.Dock = DockStyle.Fill;
			tabControlIdes.Location = new Point(699, 0);
			tabControlIdes.Margin = new Padding(0);
			tabControlIdes.Name = "tabControlIdes";
			tabControlIdes.SelectedIndex = 0;
			tabControlIdes.Size = new Size(250, 497);
			tabControlIdes.TabIndex = 1;
			tabControlIdes.SelectedIndexChanged += tabControlIdes_SelectedIndexChanged;
			// 
			// tabVisualStudio
			// 
			tabVisualStudio.Controls.Add(flowLayoutPanel1);
			tabVisualStudio.Controls.Add(btnVsInstaller);
			tabVisualStudio.Location = new Point(4, 24);
			tabVisualStudio.Name = "tabVisualStudio";
			tabVisualStudio.Padding = new Padding(3);
			tabVisualStudio.Size = new Size(242, 469);
			tabVisualStudio.TabIndex = 0;
			tabVisualStudio.Text = "Visual Studio";
			tabVisualStudio.UseVisualStyleBackColor = true;
			// 
			// flowLayoutPanel1
			// 
			flowLayoutPanel1.AutoScroll = true;
			flowLayoutPanel1.Controls.Add(label2);
			flowLayoutPanel1.Controls.Add(selectVisualStudioVersion);
			flowLayoutPanel1.Controls.Add(btnMainStartVisualStudio1);
			flowLayoutPanel1.Controls.Add(btnMainStartVisualStudio2);
			flowLayoutPanel1.Controls.Add(btnMainStartVisualStudio3);
			flowLayoutPanel1.Controls.Add(btnMainStartVisualStudio4);
			flowLayoutPanel1.Controls.Add(btnMainStartVisualStudio5);
			flowLayoutPanel1.Controls.Add(btnMainOpenActivityLog);
			flowLayoutPanel1.Dock = DockStyle.Fill;
			flowLayoutPanel1.FlowDirection = FlowDirection.TopDown;
			flowLayoutPanel1.Location = new Point(3, 3);
			flowLayoutPanel1.Margin = new Padding(0);
			flowLayoutPanel1.Name = "flowLayoutPanel1";
			flowLayoutPanel1.Size = new Size(236, 415);
			flowLayoutPanel1.TabIndex = 0;
			flowLayoutPanel1.WrapContents = false;
			// 
			// selectVisualStudioVersion
			// 
			this.selectVisualStudioVersion.DrawMode = DrawMode.OwnerDrawFixed;
			this.selectVisualStudioVersion.DropDownHeight = 300;
			this.selectVisualStudioVersion.DropDownStyle = ComboBoxStyle.DropDownList;
			this.selectVisualStudioVersion.IntegralHeight = false;
			this.selectVisualStudioVersion.ItemHeight = 28;
			this.selectVisualStudioVersion.Location = new Point(0, 21);
			this.selectVisualStudioVersion.Margin = new Padding(0, 0, 0, 6);
			this.selectVisualStudioVersion.Name = "selectVisualStudioVersion";
			this.selectVisualStudioVersion.SelectedItem = null;
			this.selectVisualStudioVersion.ShowDefault = false;
			this.selectVisualStudioVersion.Size = new Size(232, 34);
			this.selectVisualStudioVersion.TabIndex = 0;
			this.tooltipForButtons.SetToolTip(this.selectVisualStudioVersion, "Visual Studio versions currently installed");
			this.selectVisualStudioVersion.SelectedIndexChanged += selectVisualStudioVersion_SelectedIndexChanged;
			// 
			// btnMainStartVisualStudio1
			// 
			btnMainStartVisualStudio1.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
			btnMainStartVisualStudio1.Image = (Image)resources.GetObject("btnMainStartVisualStudio1.Image");
			btnMainStartVisualStudio1.ImageAlign = ContentAlignment.MiddleLeft;
			btnMainStartVisualStudio1.Location = new Point(0, 61);
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
			btnMainStartVisualStudio2.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
			btnMainStartVisualStudio2.Image = (Image)resources.GetObject("btnMainStartVisualStudio2.Image");
			btnMainStartVisualStudio2.ImageAlign = ContentAlignment.MiddleLeft;
			btnMainStartVisualStudio2.Location = new Point(0, 125);
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
			btnMainStartVisualStudio3.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
			btnMainStartVisualStudio3.Image = (Image)resources.GetObject("btnMainStartVisualStudio3.Image");
			btnMainStartVisualStudio3.ImageAlign = ContentAlignment.MiddleLeft;
			btnMainStartVisualStudio3.Location = new Point(0, 189);
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
			btnMainStartVisualStudio4.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
			btnMainStartVisualStudio4.Image = (Image)resources.GetObject("btnMainStartVisualStudio4.Image");
			btnMainStartVisualStudio4.ImageAlign = ContentAlignment.MiddleLeft;
			btnMainStartVisualStudio4.Location = new Point(0, 253);
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
			btnMainStartVisualStudio5.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
			btnMainStartVisualStudio5.Image = (Image)resources.GetObject("btnMainStartVisualStudio5.Image");
			btnMainStartVisualStudio5.ImageAlign = ContentAlignment.MiddleLeft;
			btnMainStartVisualStudio5.Location = new Point(0, 317);
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
			// btnMainOpenActivityLog
			// 
			btnMainOpenActivityLog.Font = new Font("Segoe UI", 9F);
			btnMainOpenActivityLog.ImageAlign = ContentAlignment.MiddleLeft;
			btnMainOpenActivityLog.Location = new Point(0, 381);
			btnMainOpenActivityLog.Margin = new Padding(0);
			btnMainOpenActivityLog.Name = "btnMainOpenActivityLog";
			btnMainOpenActivityLog.Padding = new Padding(4);
			btnMainOpenActivityLog.Size = new Size(232, 32);
			btnMainOpenActivityLog.TabIndex = 6;
			btnMainOpenActivityLog.Tag = "Open {0} ActivityLog";
			btnMainOpenActivityLog.Text = "Open ActivityLog";
			btnMainOpenActivityLog.TextImageRelation = TextImageRelation.ImageBeforeText;
			tooltipForButtons.SetToolTip(btnMainOpenActivityLog, "Opens the activity log file of Visual Studio");
			btnMainOpenActivityLog.UseMnemonic = false;
			btnMainOpenActivityLog.UseVisualStyleBackColor = true;
			btnMainOpenActivityLog.Click += btnMainOpenActivityLog_Click;
			// 
			// btnVsInstaller
			// 
			btnVsInstaller.Dock = DockStyle.Bottom;
			btnVsInstaller.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
			btnVsInstaller.Image = Resources.ImportVS1;
			btnVsInstaller.ImageAlign = ContentAlignment.MiddleLeft;
			btnVsInstaller.Location = new Point(3, 418);
			btnVsInstaller.Margin = new Padding(0, 0, 0, 4);
			btnVsInstaller.Name = "btnVsInstaller";
			btnVsInstaller.Padding = new Padding(4, 0, 4, 0);
			btnVsInstaller.Size = new Size(236, 48);
			btnVsInstaller.TabIndex = 5;
			btnVsInstaller.Tag = "";
			btnVsInstaller.Text = "Visual Studio Installer";
			btnVsInstaller.TextAlign = ContentAlignment.MiddleLeft;
			btnVsInstaller.TextImageRelation = TextImageRelation.ImageBeforeText;
			btnVsInstaller.UseVisualStyleBackColor = true;
			btnVsInstaller.Click += btnVsInstaller_Click;
			// 
			// tabVsCode
			// 
			tabVsCode.Controls.Add(flowLayoutPanelVsCode);
			tabVsCode.Controls.Add(btnVsCodeDownload);
			tabVsCode.Location = new Point(4, 24);
			tabVsCode.Name = "tabVsCode";
			tabVsCode.Padding = new Padding(3);
			tabVsCode.Size = new Size(242, 469);
			tabVsCode.TabIndex = 1;
			tabVsCode.Text = "VS Code";
			tabVsCode.UseVisualStyleBackColor = true;
			// 
			// flowLayoutPanelVsCode
			// 
			flowLayoutPanelVsCode.AutoScroll = true;
			flowLayoutPanelVsCode.Controls.Add(lblVsCodeEdition);
			flowLayoutPanelVsCode.Controls.Add(selectVsCodeEdition);
			flowLayoutPanelVsCode.Controls.Add(btnVsCodeStart);
			flowLayoutPanelVsCode.Controls.Add(btnVsCodeStartAdmin);
			flowLayoutPanelVsCode.Controls.Add(btnVsCodeStartNewWindow);
			flowLayoutPanelVsCode.Controls.Add(btnVsCodeStartNoExt);
			flowLayoutPanelVsCode.Controls.Add(btnVsCodeOpenActivityLog);
			flowLayoutPanelVsCode.Dock = DockStyle.Fill;
			flowLayoutPanelVsCode.FlowDirection = FlowDirection.TopDown;
			flowLayoutPanelVsCode.Location = new Point(3, 3);
			flowLayoutPanelVsCode.Margin = new Padding(0);
			flowLayoutPanelVsCode.Name = "flowLayoutPanelVsCode";
			flowLayoutPanelVsCode.Size = new Size(236, 415);
			flowLayoutPanelVsCode.TabIndex = 0;
			flowLayoutPanelVsCode.WrapContents = false;
			// 
			// selectVsCodeEdition
			// 
			this.selectVsCodeEdition.DrawMode = DrawMode.OwnerDrawFixed;
			this.selectVsCodeEdition.DropDownHeight = 300;
			this.selectVsCodeEdition.DropDownStyle = ComboBoxStyle.DropDownList;
			this.selectVsCodeEdition.IntegralHeight = false;
			this.selectVsCodeEdition.ItemHeight = 28;
			this.selectVsCodeEdition.Location = new Point(0, 21);
			this.selectVsCodeEdition.Margin = new Padding(0, 0, 0, 6);
			this.selectVsCodeEdition.Name = "selectVsCodeEdition";
			this.selectVsCodeEdition.ShowDefault = false;
			this.selectVsCodeEdition.Size = new Size(232, 34);
			this.selectVsCodeEdition.TabIndex = 0;
			this.tooltipForButtons.SetToolTip(this.selectVsCodeEdition, "VS Code editions currently installed");
			this.selectVsCodeEdition.SelectedIndexChanged += selectVsCodeEdition_SelectedIndexChanged;
			// 
			// btnVsCodeStart
			// 
			btnVsCodeStart.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
			btnVsCodeStart.ImageAlign = ContentAlignment.MiddleLeft;
			btnVsCodeStart.Location = new Point(0, 61);
			btnVsCodeStart.Margin = new Padding(0);
			btnVsCodeStart.Name = "btnVsCodeStart";
			btnVsCodeStart.Padding = new Padding(4);
			btnVsCodeStart.Size = new Size(232, 64);
			btnVsCodeStart.TabIndex = 1;
			btnVsCodeStart.Tag = "Start {0}";
			btnVsCodeStart.Text = "Start VS Code";
			btnVsCodeStart.TextAlign = ContentAlignment.MiddleLeft;
			btnVsCodeStart.TextImageRelation = TextImageRelation.ImageBeforeText;
			btnVsCodeStart.UseVisualStyleBackColor = true;
			btnVsCodeStart.Click += btnVsCodeStart_Click;
			// 
			// btnVsCodeStartAdmin
			// 
			btnVsCodeStartAdmin.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
			btnVsCodeStartAdmin.ImageAlign = ContentAlignment.MiddleLeft;
			btnVsCodeStartAdmin.Location = new Point(0, 125);
			btnVsCodeStartAdmin.Margin = new Padding(0);
			btnVsCodeStartAdmin.Name = "btnVsCodeStartAdmin";
			btnVsCodeStartAdmin.Padding = new Padding(4);
			btnVsCodeStartAdmin.Size = new Size(232, 64);
			btnVsCodeStartAdmin.TabIndex = 2;
			btnVsCodeStartAdmin.Tag = "Start {0} as Admin";
			btnVsCodeStartAdmin.Text = "Start VS Code \r\nas Admin";
			btnVsCodeStartAdmin.TextAlign = ContentAlignment.MiddleLeft;
			btnVsCodeStartAdmin.TextImageRelation = TextImageRelation.ImageBeforeText;
			btnVsCodeStartAdmin.UseVisualStyleBackColor = true;
			btnVsCodeStartAdmin.Click += btnVsCodeStartAdmin_Click;
			// 
			// btnVsCodeStartNewWindow
			// 
			btnVsCodeStartNewWindow.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
			btnVsCodeStartNewWindow.ImageAlign = ContentAlignment.MiddleLeft;
			btnVsCodeStartNewWindow.Location = new Point(0, 189);
			btnVsCodeStartNewWindow.Margin = new Padding(0);
			btnVsCodeStartNewWindow.Name = "btnVsCodeStartNewWindow";
			btnVsCodeStartNewWindow.Padding = new Padding(4);
			btnVsCodeStartNewWindow.Size = new Size(232, 64);
			btnVsCodeStartNewWindow.TabIndex = 3;
			btnVsCodeStartNewWindow.Tag = "Start {0}\r\nwith New Window";
			btnVsCodeStartNewWindow.Text = "Start VS Code\r\nwith New Window";
			btnVsCodeStartNewWindow.TextAlign = ContentAlignment.MiddleLeft;
			btnVsCodeStartNewWindow.TextImageRelation = TextImageRelation.ImageBeforeText;
			btnVsCodeStartNewWindow.UseVisualStyleBackColor = true;
			btnVsCodeStartNewWindow.Click += btnVsCodeStartNewWindow_Click;
			// 
			// btnVsCodeStartNoExt
			// 
			btnVsCodeStartNoExt.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
			btnVsCodeStartNoExt.ImageAlign = ContentAlignment.MiddleLeft;
			btnVsCodeStartNoExt.Location = new Point(0, 253);
			btnVsCodeStartNoExt.Margin = new Padding(0);
			btnVsCodeStartNoExt.Name = "btnVsCodeStartNoExt";
			btnVsCodeStartNoExt.Padding = new Padding(4);
			btnVsCodeStartNoExt.Size = new Size(232, 64);
			btnVsCodeStartNoExt.TabIndex = 4;
			btnVsCodeStartNoExt.Tag = "Start {0}\r\nwithout Extensions";
			btnVsCodeStartNoExt.Text = "Start VS Code\r\nwithout Extensions";
			btnVsCodeStartNoExt.TextAlign = ContentAlignment.MiddleLeft;
			btnVsCodeStartNoExt.TextImageRelation = TextImageRelation.ImageBeforeText;
			btnVsCodeStartNoExt.UseVisualStyleBackColor = true;
			btnVsCodeStartNoExt.Click += btnVsCodeStartNoExt_Click;
			// 
			// btnVsCodeOpenActivityLog
			// 
			btnVsCodeOpenActivityLog.Font = new Font("Segoe UI", 9F);
			btnVsCodeOpenActivityLog.ImageAlign = ContentAlignment.MiddleLeft;
			btnVsCodeOpenActivityLog.Location = new Point(0, 317);
			btnVsCodeOpenActivityLog.Margin = new Padding(0);
			btnVsCodeOpenActivityLog.Name = "btnVsCodeOpenActivityLog";
			btnVsCodeOpenActivityLog.Padding = new Padding(4);
			btnVsCodeOpenActivityLog.Size = new Size(232, 32);
			btnVsCodeOpenActivityLog.TabIndex = 5;
			btnVsCodeOpenActivityLog.Tag = "Open {0} ActivityLog";
			btnVsCodeOpenActivityLog.Text = "Open ActivityLog";
			btnVsCodeOpenActivityLog.TextImageRelation = TextImageRelation.ImageBeforeText;
			tooltipForButtons.SetToolTip(btnVsCodeOpenActivityLog, "Opens the VS Code logs folder in Explorer");
			btnVsCodeOpenActivityLog.UseMnemonic = false;
			btnVsCodeOpenActivityLog.UseVisualStyleBackColor = true;
			btnVsCodeOpenActivityLog.Click += btnVsCodeOpenActivityLog_Click;
			// 
			// btnVsCodeDownload
			// 
			btnVsCodeDownload.Dock = DockStyle.Bottom;
			btnVsCodeDownload.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
			btnVsCodeDownload.Image = Resources.Download;
			btnVsCodeDownload.ImageAlign = ContentAlignment.MiddleLeft;
			btnVsCodeDownload.Location = new Point(3, 418);
			btnVsCodeDownload.Margin = new Padding(0, 0, 0, 4);
			btnVsCodeDownload.Name = "btnVsCodeDownload";
			btnVsCodeDownload.Padding = new Padding(4, 0, 4, 0);
			btnVsCodeDownload.Size = new Size(236, 48);
			btnVsCodeDownload.TabIndex = 1;
			btnVsCodeDownload.Text = "Download VS Code";
			btnVsCodeDownload.TextAlign = ContentAlignment.MiddleLeft;
			btnVsCodeDownload.TextImageRelation = TextImageRelation.ImageBeforeText;
			btnVsCodeDownload.UseVisualStyleBackColor = true;
			btnVsCodeDownload.Click += btnVsCodeDownload_Click;
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
			ctxMenu.Items.AddRange(new ToolStripItem[] { gitToolStripMenuItem, addToolStripMenuItem, toolStripMenuItem3, runToolStripMenuItem, runAsAdminToolStripMenuItem, renameToolStripMenuItem, toolStripSeparator1, settingsToolStripMenuItem, toolStripSeparator2, removeToolStripMenuItem, toolStripMenuItem4, favoriteToolStripMenuItem, explorerToolStripMenuItem, openInVsCodeToolStripMenuItem });
			ctxMenu.Name = "ctxMenu";
			ctxMenu.Size = new Size(209, 248);
			ctxMenu.Opening += ctxMenu_Opening;
			// 
			// gitToolStripMenuItem
			// 
			gitToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { branchesToolStripMenuItem, toolStripMenuItem1, fetchToolStripMenuItem, pullToolStripMenuItem });
			gitToolStripMenuItem.Name = "gitToolStripMenuItem";
			gitToolStripMenuItem.Size = new Size(208, 22);
			gitToolStripMenuItem.Text = "Git";
			// 
			// branchesToolStripMenuItem
			// 
			branchesToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { xToolStripMenuItem });
			branchesToolStripMenuItem.Name = "branchesToolStripMenuItem";
			branchesToolStripMenuItem.Size = new Size(122, 22);
			branchesToolStripMenuItem.Text = "Branches";
			// 
			// xToolStripMenuItem
			// 
			xToolStripMenuItem.Name = "xToolStripMenuItem";
			xToolStripMenuItem.Size = new Size(80, 22);
			xToolStripMenuItem.Text = "x";
			// 
			// toolStripMenuItem1
			// 
			toolStripMenuItem1.Name = "toolStripMenuItem1";
			toolStripMenuItem1.Size = new Size(119, 6);
			// 
			// fetchToolStripMenuItem
			// 
			fetchToolStripMenuItem.Name = "fetchToolStripMenuItem";
			fetchToolStripMenuItem.Size = new Size(122, 22);
			fetchToolStripMenuItem.Text = "Fetch";
			fetchToolStripMenuItem.Click += fetchToolStripMenuItem_Click;
			// 
			// pullToolStripMenuItem
			// 
			pullToolStripMenuItem.Name = "pullToolStripMenuItem";
			pullToolStripMenuItem.Size = new Size(122, 22);
			pullToolStripMenuItem.Text = "Pull";
			pullToolStripMenuItem.Click += pullToolStripMenuItem_Click;
			// 
			// addToolStripMenuItem
			// 
			addToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { newGroupToolStripMenuItem, fromFolderToolStripMenuItem, solutionProjectToolStripMenuItem });
			addToolStripMenuItem.Name = "addToolStripMenuItem";
			addToolStripMenuItem.Size = new Size(208, 22);
			addToolStripMenuItem.Text = "Add...";
			// 
			// newGroupToolStripMenuItem
			// 
			newGroupToolStripMenuItem.Name = "newGroupToolStripMenuItem";
			newGroupToolStripMenuItem.Size = new Size(169, 22);
			newGroupToolStripMenuItem.Text = "New Group...";
			newGroupToolStripMenuItem.Click += newGroupToolStripMenuItem_Click;
			// 
			// fromFolderToolStripMenuItem
			// 
			fromFolderToolStripMenuItem.Name = "fromFolderToolStripMenuItem";
			fromFolderToolStripMenuItem.Size = new Size(169, 22);
			fromFolderToolStripMenuItem.Text = "From Folder...";
			fromFolderToolStripMenuItem.Click += fromFolderToolStripMenuItem_Click;
			// 
			// solutionProjectToolStripMenuItem
			// 
			solutionProjectToolStripMenuItem.Name = "solutionProjectToolStripMenuItem";
			solutionProjectToolStripMenuItem.Size = new Size(169, 22);
			solutionProjectToolStripMenuItem.Text = "Solution/Project...";
			solutionProjectToolStripMenuItem.Click += solutionProjectToolStripMenuItem_Click;
			// 
			// toolStripMenuItem3
			// 
			toolStripMenuItem3.Name = "toolStripMenuItem3";
			toolStripMenuItem3.Size = new Size(205, 6);
			// 
			// runToolStripMenuItem
			// 
			runToolStripMenuItem.Name = "runToolStripMenuItem";
			runToolStripMenuItem.ShortcutKeys = Keys.F5;
			runToolStripMenuItem.Size = new Size(208, 22);
			runToolStripMenuItem.Text = "Run";
			runToolStripMenuItem.Click += runToolStripMenuItem_Click;
			// 
			// runAsAdminToolStripMenuItem
			// 
			runAsAdminToolStripMenuItem.Name = "runAsAdminToolStripMenuItem";
			runAsAdminToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.F5;
			runAsAdminToolStripMenuItem.Size = new Size(208, 22);
			runAsAdminToolStripMenuItem.Text = "Run as Admin";
			runAsAdminToolStripMenuItem.Click += runAsAdminToolStripMenuItem_Click;
			// 
			// renameToolStripMenuItem
			// 
			renameToolStripMenuItem.Name = "renameToolStripMenuItem";
			renameToolStripMenuItem.Size = new Size(208, 22);
			renameToolStripMenuItem.Text = "Rename...";
			renameToolStripMenuItem.Click += renameToolStripMenuItem_Click;
			// 
			// settingsToolStripMenuItem
			// 
			settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
			settingsToolStripMenuItem.ShortcutKeys = Keys.Alt | Keys.Enter;
			settingsToolStripMenuItem.Size = new Size(208, 22);
			settingsToolStripMenuItem.Text = "Settings...";
			settingsToolStripMenuItem.Click += settingsToolStripMenuItem_Click;
			// 
			// removeToolStripMenuItem
			// 
			removeToolStripMenuItem.Name = "removeToolStripMenuItem";
			removeToolStripMenuItem.ShortcutKeys = Keys.Shift | Keys.Delete;
			removeToolStripMenuItem.Size = new Size(208, 22);
			removeToolStripMenuItem.Text = "Remove...";
			removeToolStripMenuItem.Click += removeToolStripMenuItem_Click;
			// 
			// toolStripMenuItem4
			// 
			toolStripMenuItem4.Name = "toolStripMenuItem4";
			toolStripMenuItem4.Size = new Size(205, 6);
			// 
			// favoriteToolStripMenuItem
			// 
			favoriteToolStripMenuItem.Name = "favoriteToolStripMenuItem";
			favoriteToolStripMenuItem.Size = new Size(208, 22);
			favoriteToolStripMenuItem.Text = "Favorite";
			favoriteToolStripMenuItem.Click += favoriteToolStripMenuItem_Click;
			// 
			// explorerToolStripMenuItem
			// 
			explorerToolStripMenuItem.Name = "explorerToolStripMenuItem";
			explorerToolStripMenuItem.Size = new Size(208, 22);
			explorerToolStripMenuItem.Text = "Open location in Explorer";
			explorerToolStripMenuItem.Click += explorerToolStripMenuItem_Click;
			// 
			// openInVsCodeToolStripMenuItem
			// 
			openInVsCodeToolStripMenuItem.Name = "openInVsCodeToolStripMenuItem";
			openInVsCodeToolStripMenuItem.Size = new Size(208, 22);
			openInVsCodeToolStripMenuItem.Text = "Open in VS Code";
			openInVsCodeToolStripMenuItem.Click += openInVsCodeToolStripMenuItem_Click;
			// 
			// statusStrip1
			// 
			statusStrip1.Items.AddRange(new ToolStripItem[] { mainStatusLabel, toolStripStatusGit });
			statusStrip1.Location = new Point(0, 497);
			statusStrip1.Name = "statusStrip1";
			statusStrip1.Size = new Size(949, 22);
			statusStrip1.TabIndex = 1;
			statusStrip1.Text = "statusStrip1";
			// 
			// mainStatusLabel
			// 
			mainStatusLabel.Name = "mainStatusLabel";
			mainStatusLabel.Size = new Size(912, 17);
			mainStatusLabel.Spring = true;
			mainStatusLabel.Text = "Lets do something incredible today";
			mainStatusLabel.TextAlign = ContentAlignment.MiddleLeft;
			// 
			// toolStripStatusGit
			// 
			toolStripStatusGit.Name = "toolStripStatusGit";
			toolStripStatusGit.Size = new Size(22, 17);
			toolStripStatusGit.Text = "Git";
			// 
			// gitTimer
			// 
			gitTimer.Interval = 1000;
			// 
			// MainDialog
			// 
			AutoScaleDimensions = new SizeF(7F, 15F);
			AutoScaleMode = AutoScaleMode.Font;
			ClientSize = new Size(949, 519);
			Controls.Add(statusStrip1);
			Controls.Add(mainPanel);
			Icon = (Icon)resources.GetObject("$this.Icon");
			Margin = new Padding(4, 3, 4, 3);
			Name = "MainDialog";
			Text = "Visual Studio Launcher";
			FormClosing += MainDialog_FormClosing;
			Load += MainDialog_Load;
			flowLayoutPanel2.ResumeLayout(false);
			flowLayoutPanel2.PerformLayout();
			leftSubPanel.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)olvFiles).EndInit();
			mainPanel.ResumeLayout(false);
			tabControlIdes.ResumeLayout(false);
			tabVisualStudio.ResumeLayout(false);
			flowLayoutPanel1.ResumeLayout(false);
			flowLayoutPanel1.PerformLayout();
			tabVsCode.ResumeLayout(false);
			flowLayoutPanelVsCode.ResumeLayout(false);
			flowLayoutPanelVsCode.PerformLayout();
			ctxMenu.ResumeLayout(false);
			statusStrip1.ResumeLayout(false);
			statusStrip1.PerformLayout();
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
		private TabControl tabControlIdes;
		private TabPage tabVisualStudio;
		private TabPage tabVsCode;
		private FlowLayoutPanel flowLayoutPanel1;
		private FlowLayoutPanel flowLayoutPanelVsCode;
		private IdeCombobox selectVsCodeEdition;
		private Button btnVsCodeStart;
		private Button btnVsCodeStartAdmin;
		private Button btnVsCodeStartNewWindow;
		private Button btnVsCodeStartNoExt;
		private Button btnVsCodeOpenActivityLog;
		private Button btnVsCodeDownload;
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
		private ToolStripMenuItem openInVsCodeToolStripMenuItem;
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

