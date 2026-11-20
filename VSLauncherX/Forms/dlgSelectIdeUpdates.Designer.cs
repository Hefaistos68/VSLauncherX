#nullable enable

namespace VSLauncher.Forms
{
	partial class dlgSelectIdeUpdates
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer? components;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (this.lvUpdates is not null)
				{
					this.lvUpdates.SmallImageList = null;
				}

				this.ideImageList?.Dispose();
				this.components?.Dispose();
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
			this.components    = new System.ComponentModel.Container();
			this.lblPrompt     = new Label();
			this.lvUpdates     = new ListView();
			this.colIde        = new ColumnHeader();
			this.colInstalled  = new ColumnHeader();
			this.colAvailable  = new ColumnHeader();
			this.ideImageList  = new ImageList(this.components);
			this.btnSelectAll  = new Button();
			this.btnSelectNone = new Button();
			this.btnCancel     = new Button();
			this.btnUpdate     = new Button();
			SuspendLayout();
			//
			// lblPrompt
			//
			this.lblPrompt.AutoSize = true;
			this.lblPrompt.Location = new Point(12, 12);
			this.lblPrompt.Name     = "lblPrompt";
			this.lblPrompt.Size     = new Size(216, 15);
			this.lblPrompt.TabIndex = 0;
			this.lblPrompt.Text     = "Select the IDEs to download and update:";
			//
			// lvUpdates
			//
			this.lvUpdates.Anchor                        = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
			this.lvUpdates.CheckBoxes                    = true;
			this.lvUpdates.Columns.AddRange(new ColumnHeader[] { this.colIde, this.colInstalled, this.colAvailable });
			this.lvUpdates.FullRowSelect                 = true;
			this.lvUpdates.HeaderStyle                   = ColumnHeaderStyle.Nonclickable;
			this.lvUpdates.HideSelection                 = false;
			this.lvUpdates.Location                      = new Point(12, 34);
			this.lvUpdates.MultiSelect                   = false;
			this.lvUpdates.Name                          = "lvUpdates";
			this.lvUpdates.ShowItemToolTips              = true;
			this.lvUpdates.Size                          = new Size(452, 198);
			this.lvUpdates.SmallImageList                = this.ideImageList;
			this.lvUpdates.TabIndex                      = 1;
			this.lvUpdates.UseCompatibleStateImageBehavior = false;
			this.lvUpdates.View                          = View.Details;
			this.lvUpdates.ItemChecked                  += lvUpdates_ItemChecked;
			//
			// colIde
			//
			this.colIde.Text  = "IDE";
			this.colIde.Width = 210;
			//
			// colInstalled
			//
			this.colInstalled.Text  = "Installed";
			this.colInstalled.Width = 100;
			//
			// colAvailable
			//
			this.colAvailable.Text  = "Available";
			this.colAvailable.Width = 100;
			//
			// ideImageList
			//
			this.ideImageList.ColorDepth       = ColorDepth.Depth32Bit;
			this.ideImageList.ImageSize        = new Size(16, 16);
			this.ideImageList.TransparentColor = Color.Transparent;
			//
			// btnSelectAll
			//
			this.btnSelectAll.Anchor                  = AnchorStyles.Bottom | AnchorStyles.Left;
			this.btnSelectAll.Location                = new Point(12, 244);
			this.btnSelectAll.Name                    = "btnSelectAll";
			this.btnSelectAll.Size                    = new Size(88, 38);
			this.btnSelectAll.TabIndex                = 2;
			this.btnSelectAll.Text                    = "Select All";
			this.btnSelectAll.UseVisualStyleBackColor = true;
			this.btnSelectAll.Click                  += btnSelectAll_Click;
			//
			// btnSelectNone
			//
			this.btnSelectNone.Anchor                  = AnchorStyles.Bottom | AnchorStyles.Left;
			this.btnSelectNone.Location                = new Point(106, 244);
			this.btnSelectNone.Name                    = "btnSelectNone";
			this.btnSelectNone.Size                    = new Size(94, 38);
			this.btnSelectNone.TabIndex                = 3;
			this.btnSelectNone.Text                    = "Select None";
			this.btnSelectNone.UseVisualStyleBackColor = true;
			this.btnSelectNone.Click                  += btnSelectNone_Click;
			//
			// btnCancel
			//
			this.btnCancel.Anchor                  = AnchorStyles.Bottom | AnchorStyles.Right;
			this.btnCancel.DialogResult            = DialogResult.Cancel;
			this.btnCancel.Image                   = Resources.Cancel_24x24;
			this.btnCancel.Location                = new Point(224, 244);
			this.btnCancel.Name                    = "btnCancel";
			this.btnCancel.Size                    = new Size(96, 38);
			this.btnCancel.TabIndex                = 4;
			this.btnCancel.Text                    = " Cancel";
			this.btnCancel.TextImageRelation       = TextImageRelation.ImageBeforeText;
			this.btnCancel.UseVisualStyleBackColor = true;
			//
			// btnUpdate
			//
			this.btnUpdate.Anchor                  = AnchorStyles.Bottom | AnchorStyles.Right;
			this.btnUpdate.DialogResult            = DialogResult.OK;
			this.btnUpdate.Image                   = Resources.Check_24x24;
			this.btnUpdate.Location                = new Point(326, 244);
			this.btnUpdate.Name                    = "btnUpdate";
			this.btnUpdate.Size                    = new Size(138, 38);
			this.btnUpdate.TabIndex                = 5;
			this.btnUpdate.Text                    = " Update Selected";
			this.btnUpdate.TextImageRelation       = TextImageRelation.ImageBeforeText;
			this.btnUpdate.UseVisualStyleBackColor = true;
			this.btnUpdate.Click                  += btnUpdate_Click;
			//
			// dlgSelectIdeUpdates
			//
			this.AcceptButton        = this.btnUpdate;
			this.AutoScaleDimensions = new SizeF(7F, 15F);
			this.AutoScaleMode       = AutoScaleMode.Font;
			this.CancelButton        = this.btnCancel;
			this.ClientSize          = new Size(476, 294);
			this.Controls.Add(this.lblPrompt);
			this.Controls.Add(this.lvUpdates);
			this.Controls.Add(this.btnSelectAll);
			this.Controls.Add(this.btnSelectNone);
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.btnUpdate);
			this.FormBorderStyle    = FormBorderStyle.FixedDialog;
			this.MaximizeBox        = false;
			this.MinimizeBox        = false;
			this.Name               = "dlgSelectIdeUpdates";
			this.ShowIcon           = false;
			this.ShowInTaskbar      = false;
			this.SizeGripStyle      = SizeGripStyle.Hide;
			this.StartPosition      = FormStartPosition.CenterParent;
			this.Text               = "Select IDE Updates";
			ResumeLayout(false);
			PerformLayout();
		}

		#endregion

		private Label lblPrompt = null!;
		private ListView lvUpdates = null!;
		private ColumnHeader colIde = null!;
		private ColumnHeader colInstalled = null!;
		private ColumnHeader colAvailable = null!;
		private ImageList ideImageList = null!;
		private Button btnSelectAll = null!;
		private Button btnSelectNone = null!;
		private Button btnCancel = null!;
		private Button btnUpdate = null!;
	}
}

#nullable restore
