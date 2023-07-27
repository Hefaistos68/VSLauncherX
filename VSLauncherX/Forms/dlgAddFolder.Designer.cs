namespace VSLauncher
{
	partial class dlgAddFolder
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
			GroupBox groupBox1;
			Label label2;
			Label label3;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(dlgAddFolder));
			btnSelectAfter = new Button();
			btnSelectBefore = new Button();
			chkRunAsAdmin = new CheckBox();
			txtRunAfter = new TextBox();
			txtRunBefore = new TextBox();
			txtFoldername = new TextBox();
			btnOk = new Button();
			btnCancel = new Button();
			chkWaitExitAfter = new CheckBox();
			chkWaitExitBefore = new CheckBox();
			label1 = new Label();
			groupBox1 = new GroupBox();
			label2 = new Label();
			label3 = new Label();
			groupBox1.SuspendLayout();
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
			// groupBox1
			// 
			groupBox1.Controls.Add(txtRunAfter);
			groupBox1.Controls.Add(chkWaitExitBefore);
			groupBox1.Controls.Add(chkWaitExitAfter);
			groupBox1.Controls.Add(btnSelectAfter);
			groupBox1.Controls.Add(btnSelectBefore);
			groupBox1.Controls.Add(chkRunAsAdmin);
			groupBox1.Controls.Add(label2);
			groupBox1.Controls.Add(txtRunBefore);
			groupBox1.Controls.Add(label3);
			groupBox1.Location = new Point(12, 56);
			groupBox1.Name = "groupBox1";
			groupBox1.Size = new Size(368, 167);
			groupBox1.TabIndex = 2;
			groupBox1.TabStop = false;
			groupBox1.Text = "Options for all items in this folder";
			// 
			// btnSelectAfter
			// 
			btnSelectAfter.Location = new Point(333, 130);
			btnSelectAfter.Name = "btnSelectAfter";
			btnSelectAfter.Size = new Size(25, 25);
			btnSelectAfter.TabIndex = 7;
			btnSelectAfter.Text = "...";
			btnSelectAfter.UseVisualStyleBackColor = true;
			btnSelectAfter.Click += btnSelectAfter_Click;
			// 
			// btnSelectBefore
			// 
			btnSelectBefore.Location = new Point(333, 35);
			btnSelectBefore.Name = "btnSelectBefore";
			btnSelectBefore.Size = new Size(25, 25);
			btnSelectBefore.TabIndex = 2;
			btnSelectBefore.Text = "...";
			btnSelectBefore.UseVisualStyleBackColor = true;
			btnSelectBefore.Click += btnSelectBefore_Click;
			// 
			// chkRunAsAdmin
			// 
			chkRunAsAdmin.AutoSize = true;
			chkRunAsAdmin.Location = new Point(6, 80);
			chkRunAsAdmin.Name = "chkRunAsAdmin";
			chkRunAsAdmin.RightToLeft = RightToLeft.Yes;
			chkRunAsAdmin.Size = new Size(105, 19);
			chkRunAsAdmin.TabIndex = 4;
			chkRunAsAdmin.Text = "Require Admin";
			chkRunAsAdmin.UseVisualStyleBackColor = true;
			// 
			// txtRunAfter
			// 
			txtRunAfter.HideSelection = false;
			txtRunAfter.Location = new Point(6, 131);
			txtRunAfter.Name = "txtRunAfter";
			txtRunAfter.ReadOnly = true;
			txtRunAfter.Size = new Size(321, 23);
			txtRunAfter.TabIndex = 6;
			// 
			// label2
			// 
			label2.AutoSize = true;
			label2.Location = new Point(6, 113);
			label2.Name = "label2";
			label2.Size = new Size(57, 15);
			label2.TabIndex = 5;
			label2.Text = "Run After";
			// 
			// txtRunBefore
			// 
			txtRunBefore.HideSelection = false;
			txtRunBefore.Location = new Point(6, 37);
			txtRunBefore.Name = "txtRunBefore";
			txtRunBefore.ReadOnly = true;
			txtRunBefore.Size = new Size(321, 23);
			txtRunBefore.TabIndex = 1;
			// 
			// label3
			// 
			label3.AutoSize = true;
			label3.Location = new Point(6, 19);
			label3.Name = "label3";
			label3.Size = new Size(65, 15);
			label3.TabIndex = 0;
			label3.Text = "Run Before";
			// 
			// txtFoldername
			// 
			txtFoldername.Location = new Point(12, 27);
			txtFoldername.Name = "txtFoldername";
			txtFoldername.Size = new Size(368, 23);
			txtFoldername.TabIndex = 1;
			txtFoldername.TextChanged += txtFoldername_TextChanged;
			// 
			// btnOk
			// 
			btnOk.DialogResult = DialogResult.OK;
			btnOk.Image = Resources.Check_24x24;
			btnOk.Location = new Point(282, 229);
			btnOk.Name = "btnOk";
			btnOk.Size = new Size(100, 40);
			btnOk.TabIndex = 4;
			btnOk.Text = "&Add";
			btnOk.TextImageRelation = TextImageRelation.ImageBeforeText;
			btnOk.UseVisualStyleBackColor = true;
			btnOk.Click += btnOk_Click;
			// 
			// btnCancel
			// 
			btnCancel.DialogResult = DialogResult.Cancel;
			btnCancel.Image = Resources.Cancel_24x24;
			btnCancel.Location = new Point(176, 229);
			btnCancel.Name = "btnCancel";
			btnCancel.Size = new Size(100, 40);
			btnCancel.TabIndex = 3;
			btnCancel.Text = " Cancel";
			btnCancel.TextImageRelation = TextImageRelation.ImageBeforeText;
			btnCancel.UseVisualStyleBackColor = true;
			// 
			// chkWaitExitAfter
			// 
			chkWaitExitAfter.AutoSize = true;
			chkWaitExitAfter.Enabled = false;
			chkWaitExitAfter.Location = new Point(255, 112);
			chkWaitExitAfter.Name = "chkWaitExitAfter";
			chkWaitExitAfter.RightToLeft = RightToLeft.Yes;
			chkWaitExitAfter.Size = new Size(72, 19);
			chkWaitExitAfter.TabIndex = 8;
			chkWaitExitAfter.Text = "Wait exit";
			chkWaitExitAfter.UseVisualStyleBackColor = true;
			// 
			// chkWaitExitBefore
			// 
			chkWaitExitBefore.AutoSize = true;
			chkWaitExitBefore.Enabled = false;
			chkWaitExitBefore.Location = new Point(255, 18);
			chkWaitExitBefore.Name = "chkWaitExitBefore";
			chkWaitExitBefore.RightToLeft = RightToLeft.Yes;
			chkWaitExitBefore.Size = new Size(72, 19);
			chkWaitExitBefore.TabIndex = 3;
			chkWaitExitBefore.Text = "Wait exit";
			chkWaitExitBefore.UseVisualStyleBackColor = true;
			// 
			// dlgAddFolder
			// 
			this.AcceptButton = btnOk;
			this.AutoScaleDimensions = new SizeF(7F, 15F);
			this.AutoScaleMode = AutoScaleMode.Font;
			this.CancelButton = btnCancel;
			this.ClientSize = new Size(394, 284);
			this.ControlBox = false;
			this.Controls.Add(btnCancel);
			this.Controls.Add(btnOk);
			this.Controls.Add(groupBox1);
			this.Controls.Add(txtFoldername);
			this.Controls.Add(label1);
			this.FormBorderStyle = FormBorderStyle.FixedDialog;
			this.Icon = (Icon)resources.GetObject("$this.Icon");
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "dlgAddFolder";
			this.SizeGripStyle = SizeGripStyle.Hide;
			this.StartPosition = FormStartPosition.CenterParent;
			this.Text = "Add new folder";
			groupBox1.ResumeLayout(false);
			groupBox1.PerformLayout();
			ResumeLayout(false);
			PerformLayout();
		}

		#endregion

		private Label label1;
		private TextBox txtFoldername;
		private GroupBox groupBox1;
		private CheckBox chkRunAsAdmin;
		private TextBox txtRunAfter;
		private Label label2;
		private TextBox txtRunBefore;
		private Label label3;
		private Button btnOk;
		private Button btnCancel;
		private Button btnSelectAfter;
		private Button btnSelectBefore;
		private CheckBox chkWaitExitAfter;
		private CheckBox chkWaitExitBefore;
	}
}