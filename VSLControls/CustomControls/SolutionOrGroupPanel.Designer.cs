namespace VSLauncher
{
	partial class SolutionOrGroupPanel
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

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
			this.labelDescription = new System.Windows.Forms.Label();
			this.itemDescription = new System.Windows.Forms.RichTextBox();
			this.itemToolstrip = new System.Windows.Forms.ToolStrip();
			this.btnFolderRun = new System.Windows.Forms.ToolStripButton();
			this.btnFolderSettings = new System.Windows.Forms.ToolStripButton();
			this.btnFolderAdd = new System.Windows.Forms.ToolStripButton();
			this.btnFolderRefresh = new System.Windows.Forms.ToolStripButton();
			this.btnFolderAddSolution = new System.Windows.Forms.ToolStripButton();
			this.imgItemSymbol = new System.Windows.Forms.PictureBox();
			this.toolStripButton1 = new System.Windows.Forms.ToolStripButton();
			this.tableLayoutPanel1.SuspendLayout();
			this.itemToolstrip.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.imgItemSymbol)).BeginInit();
			this.SuspendLayout();
			// 
			// tableLayoutPanel1
			// 
			this.tableLayoutPanel1.ColumnCount = 4;
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 5F));
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 26F));
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 190F));
			this.tableLayoutPanel1.Controls.Add(this.labelDescription, 2, 0);
			this.tableLayoutPanel1.Controls.Add(this.itemDescription, 2, 1);
			this.tableLayoutPanel1.Controls.Add(this.imgItemSymbol, 1, 0);
			this.tableLayoutPanel1.Controls.Add(this.itemToolstrip, 3, 0);
			this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
			this.tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(0);
			this.tableLayoutPanel1.Name = "tableLayoutPanel1";
			this.tableLayoutPanel1.RowCount = 2;
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.Size = new System.Drawing.Size(459, 66);
			this.tableLayoutPanel1.TabIndex = 0;
			this.tableLayoutPanel1.Click += new System.EventHandler(this.tableLayoutPanel_Click);
			// 
			// labelDescription
			// 
			this.labelDescription.CausesValidation = false;
			this.labelDescription.Dock = System.Windows.Forms.DockStyle.Fill;
			this.labelDescription.Font = new System.Drawing.Font("Trebuchet MS", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.labelDescription.Location = new System.Drawing.Point(34, 0);
			this.labelDescription.Name = "labelDescription";
			this.labelDescription.Size = new System.Drawing.Size(308, 26);
			this.labelDescription.TabIndex = 1;
			this.labelDescription.Text = "Item Name";
			this.labelDescription.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.labelDescription.Click += new System.EventHandler(this.labelDescription_Click);
			// 
			// itemDescription
			// 
			this.itemDescription.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.itemDescription.CausesValidation = false;
			this.tableLayoutPanel1.SetColumnSpan(this.itemDescription, 2);
			this.itemDescription.Cursor = System.Windows.Forms.Cursors.Default;
			this.itemDescription.DetectUrls = false;
			this.itemDescription.Dock = System.Windows.Forms.DockStyle.Fill;
			this.itemDescription.Font = new System.Drawing.Font("Trebuchet MS", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.itemDescription.Location = new System.Drawing.Point(34, 29);
			this.itemDescription.Name = "itemDescription";
			this.itemDescription.ReadOnly = true;
			this.itemDescription.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.None;
			this.itemDescription.ShortcutsEnabled = false;
			this.itemDescription.Size = new System.Drawing.Size(498, 40);
			this.itemDescription.TabIndex = 2;
			this.itemDescription.TabStop = false;
			this.itemDescription.Text = "item path\\somewhere";
			this.itemDescription.WordWrap = false;
			this.itemDescription.Click += new System.EventHandler(this.itemDescription_Click);
			this.itemDescription.Enter += new System.EventHandler(this.itemDescription_Enter);
			// 
			// itemToolstrip
			// 
			this.itemToolstrip.AllowMerge = false;
			this.itemToolstrip.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.itemToolstrip.AutoSize = false;
			this.itemToolstrip.BackColor = System.Drawing.SystemColors.Control;
			this.itemToolstrip.CanOverflow = false;
			this.itemToolstrip.Dock = System.Windows.Forms.DockStyle.None;
			this.itemToolstrip.GripMargin = new System.Windows.Forms.Padding(0);
			this.itemToolstrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this.itemToolstrip.ImageScalingSize = new System.Drawing.Size(24, 24);
			this.itemToolstrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btnFolderRun,
            this.btnFolderSettings,
            this.btnFolderAdd,
            this.btnFolderRefresh,
            this.btnFolderAddSolution,
            this.toolStripButton1});
			this.itemToolstrip.Location = new System.Drawing.Point(418, 0);
			this.itemToolstrip.Name = "itemToolstrip";
			this.itemToolstrip.Padding = new System.Windows.Forms.Padding(0);
			this.itemToolstrip.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
			this.itemToolstrip.Size = new System.Drawing.Size(117, 26);
			this.itemToolstrip.Stretch = true;
			this.itemToolstrip.TabIndex = 3;
			this.itemToolstrip.Text = "itemButtons";
			// 
			// btnFolderRun
			// 
			this.btnFolderRun.BackColor = System.Drawing.SystemColors.Control;
			this.btnFolderRun.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnFolderRun.Image = global::CustomControls.Properties.Resources.Run;
			this.btnFolderRun.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
			this.btnFolderRun.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnFolderRun.Margin = new System.Windows.Forms.Padding(0);
			this.btnFolderRun.Name = "btnFolderRun";
			this.btnFolderRun.Size = new System.Drawing.Size(23, 26);
			this.btnFolderRun.Text = "&Run";
			// 
			// btnFolderSettings
			// 
			this.btnFolderSettings.BackColor = System.Drawing.SystemColors.Control;
			this.btnFolderSettings.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnFolderSettings.Image = global::CustomControls.Properties.Resources.Settings;
			this.btnFolderSettings.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
			this.btnFolderSettings.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnFolderSettings.Margin = new System.Windows.Forms.Padding(0);
			this.btnFolderSettings.Name = "btnFolderSettings";
			this.btnFolderSettings.Size = new System.Drawing.Size(23, 26);
			this.btnFolderSettings.Text = "&Settings";
			// 
			// btnFolderAdd
			// 
			this.btnFolderAdd.BackColor = System.Drawing.SystemColors.Control;
			this.btnFolderAdd.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnFolderAdd.Image = global::CustomControls.Properties.Resources.AddFolder;
			this.btnFolderAdd.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
			this.btnFolderAdd.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnFolderAdd.Margin = new System.Windows.Forms.Padding(0);
			this.btnFolderAdd.Name = "btnFolderAdd";
			this.btnFolderAdd.Size = new System.Drawing.Size(23, 26);
			this.btnFolderAdd.Text = "&Add Folder";
			// 
			// btnFolderRefresh
			// 
			this.btnFolderRefresh.BackColor = System.Drawing.SystemColors.Control;
			this.btnFolderRefresh.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnFolderRefresh.Image = global::CustomControls.Properties.Resources.Refresh;
			this.btnFolderRefresh.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
			this.btnFolderRefresh.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnFolderRefresh.Margin = new System.Windows.Forms.Padding(0);
			this.btnFolderRefresh.Name = "btnFolderRefresh";
			this.btnFolderRefresh.Size = new System.Drawing.Size(23, 26);
			this.btnFolderRefresh.Text = "Re&fresh";
			// 
			// btnFolderAddSolution
			// 
			this.btnFolderAddSolution.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnFolderAddSolution.Image = global::CustomControls.Properties.Resources.Add;
			this.btnFolderAddSolution.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
			this.btnFolderAddSolution.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnFolderAddSolution.Name = "btnFolderAddSolution";
			this.btnFolderAddSolution.Size = new System.Drawing.Size(23, 23);
			this.btnFolderAddSolution.Text = "btnFolderAddSolution";
			// 
			// imgItemSymbol
			// 
			this.imgItemSymbol.Dock = System.Windows.Forms.DockStyle.Fill;
			this.imgItemSymbol.Image = global::CustomControls.Properties.Resources.FolderClosedBlue;
			this.imgItemSymbol.Location = new System.Drawing.Point(5, 0);
			this.imgItemSymbol.Margin = new System.Windows.Forms.Padding(0);
			this.imgItemSymbol.Name = "imgItemSymbol";
			this.imgItemSymbol.Size = new System.Drawing.Size(26, 26);
			this.imgItemSymbol.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
			this.imgItemSymbol.TabIndex = 4;
			this.imgItemSymbol.TabStop = false;
			// 
			// toolStripButton1
			// 
			this.toolStripButton1.CheckOnClick = true;
			this.toolStripButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.toolStripButton1.Image = global::CustomControls.Properties.Resources.RightArrowAsterisk;
			this.toolStripButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolStripButton1.Name = "toolStripButton1";
			this.toolStripButton1.Size = new System.Drawing.Size(28, 23);
			this.toolStripButton1.Text = "chkAdmin";
			// 
			// SolutionOrGroupPanel
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.tableLayoutPanel1);
			this.Name = "SolutionOrGroupPanel";
			this.Size = new System.Drawing.Size(459, 66);
			this.tableLayoutPanel1.ResumeLayout(false);
			this.itemToolstrip.ResumeLayout(false);
			this.itemToolstrip.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.imgItemSymbol)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
		private System.Windows.Forms.Label labelDescription;
		private System.Windows.Forms.RichTextBox itemDescription;
		private System.Windows.Forms.ToolStrip itemToolstrip;
		private System.Windows.Forms.ToolStripButton btnFolderRun;
		private System.Windows.Forms.ToolStripButton btnFolderSettings;
		private System.Windows.Forms.ToolStripButton btnFolderAdd;
		private System.Windows.Forms.ToolStripButton btnFolderRefresh;
		private System.Windows.Forms.PictureBox imgItemSymbol;
		private System.Windows.Forms.ToolStripButton btnFolderAddSolution;
		private System.Windows.Forms.ToolStripButton toolStripButton1;
	}
}
