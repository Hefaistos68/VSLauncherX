namespace VSLauncher
{
	partial class dlgBeforeAfter
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
			Label label2;
			Label label3;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(dlgBeforeAfter));
			txtTitle = new Label();
			txtRunAfter = new TextBox();
			chkWaitExitBefore = new CheckBox();
			chkWaitExitAfter = new CheckBox();
			btnSelectAfter = new Button();
			btnSelectBefore = new Button();
			txtRunBefore = new TextBox();
			btnOk = new Button();
			btnCancel = new Button();
			label2 = new Label();
			label3 = new Label();
			SuspendLayout();
			// 
			// label2
			// 
			label2.AutoSize = true;
			label2.Location = new Point(18, 113);
			label2.Name = "label2";
			label2.Size = new Size(57, 15);
			label2.TabIndex = 5;
			label2.Text = "Run After";
			// 
			// label3
			// 
			label3.AutoSize = true;
			label3.Location = new Point(18, 48);
			label3.Name = "label3";
			label3.Size = new Size(65, 15);
			label3.TabIndex = 1;
			label3.Text = "Run Before";
			// 
			// txtTitle
			// 
			txtTitle.Font = new Font("Segoe UI", 14.25F, FontStyle.Bold, GraphicsUnit.Point);
			txtTitle.Location = new Point(12, 9);
			txtTitle.Name = "txtTitle";
			txtTitle.Size = new Size(358, 31);
			txtTitle.TabIndex = 0;
			txtTitle.Text = "SomeText.sln";
			// 
			// txtRunAfter
			// 
			txtRunAfter.HideSelection = false;
			txtRunAfter.Location = new Point(18, 131);
			txtRunAfter.MaxLength = 512;
			txtRunAfter.Name = "txtRunAfter";
			txtRunAfter.Size = new Size(321, 23);
			txtRunAfter.TabIndex = 6;
			// 
			// chkWaitExitBefore
			// 
			chkWaitExitBefore.AutoSize = true;
			chkWaitExitBefore.Enabled = false;
			chkWaitExitBefore.Location = new Point(267, 47);
			chkWaitExitBefore.Name = "chkWaitExitBefore";
			chkWaitExitBefore.RightToLeft = RightToLeft.Yes;
			chkWaitExitBefore.Size = new Size(72, 19);
			chkWaitExitBefore.TabIndex = 4;
			chkWaitExitBefore.Text = "Wait exit";
			chkWaitExitBefore.UseVisualStyleBackColor = true;
			// 
			// chkWaitExitAfter
			// 
			chkWaitExitAfter.AutoSize = true;
			chkWaitExitAfter.Enabled = false;
			chkWaitExitAfter.Location = new Point(267, 112);
			chkWaitExitAfter.Name = "chkWaitExitAfter";
			chkWaitExitAfter.RightToLeft = RightToLeft.Yes;
			chkWaitExitAfter.Size = new Size(72, 19);
			chkWaitExitAfter.TabIndex = 8;
			chkWaitExitAfter.Text = "Wait exit";
			chkWaitExitAfter.UseVisualStyleBackColor = true;
			// 
			// btnSelectAfter
			// 
			btnSelectAfter.Location = new Point(345, 130);
			btnSelectAfter.Name = "btnSelectAfter";
			btnSelectAfter.Size = new Size(25, 25);
			btnSelectAfter.TabIndex = 7;
			btnSelectAfter.Text = "...";
			btnSelectAfter.UseVisualStyleBackColor = true;
			btnSelectAfter.Click += btnSelectAfter_Click;
			// 
			// btnSelectBefore
			// 
			btnSelectBefore.Location = new Point(345, 64);
			btnSelectBefore.Name = "btnSelectBefore";
			btnSelectBefore.Size = new Size(25, 25);
			btnSelectBefore.TabIndex = 3;
			btnSelectBefore.Text = "...";
			btnSelectBefore.UseVisualStyleBackColor = true;
			btnSelectBefore.Click += btnSelectBefore_Click;
			// 
			// txtRunBefore
			// 
			txtRunBefore.HideSelection = false;
			txtRunBefore.Location = new Point(18, 66);
			txtRunBefore.MaxLength = 512;
			txtRunBefore.Name = "txtRunBefore";
			txtRunBefore.Size = new Size(321, 23);
			txtRunBefore.TabIndex = 2;
			// 
			// btnOk
			// 
			btnOk.DialogResult = DialogResult.OK;
			btnOk.Image = Resources.Check_24x24;
			btnOk.Location = new Point(270, 187);
			btnOk.Name = "btnOk";
			btnOk.Size = new Size(100, 40);
			btnOk.TabIndex = 10;
			btnOk.Text = "Ok";
			btnOk.TextImageRelation = TextImageRelation.ImageBeforeText;
			btnOk.UseVisualStyleBackColor = true;
			btnOk.Click += btnOk_Click;
			// 
			// btnCancel
			// 
			btnCancel.DialogResult = DialogResult.Cancel;
			btnCancel.Image = Resources.Cancel_24x24;
			btnCancel.Location = new Point(164, 187);
			btnCancel.Name = "btnCancel";
			btnCancel.Size = new Size(100, 40);
			btnCancel.TabIndex = 9;
			btnCancel.Text = " Cancel";
			btnCancel.TextImageRelation = TextImageRelation.ImageBeforeText;
			btnCancel.UseVisualStyleBackColor = true;
			// 
			// dlgBeforeAfter
			// 
			this.AcceptButton = btnOk;
			this.AutoScaleDimensions = new SizeF(7F, 15F);
			this.AutoScaleMode = AutoScaleMode.Font;
			this.CancelButton = btnCancel;
			this.ClientSize = new Size(381, 239);
			this.ControlBox = false;
			this.Controls.Add(txtRunAfter);
			this.Controls.Add(chkWaitExitAfter);
			this.Controls.Add(btnCancel);
			this.Controls.Add(btnSelectAfter);
			this.Controls.Add(chkWaitExitBefore);
			this.Controls.Add(label2);
			this.Controls.Add(btnOk);
			this.Controls.Add(txtTitle);
			this.Controls.Add(btnSelectBefore);
			this.Controls.Add(label3);
			this.Controls.Add(txtRunBefore);
			this.FormBorderStyle = FormBorderStyle.FixedDialog;
			this.Icon = (Icon)resources.GetObject("$this.Icon");
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "dlgBeforeAfter";
			this.SizeGripStyle = SizeGripStyle.Hide;
			this.StartPosition = FormStartPosition.CenterParent;
			this.Text = "Set before and after actions";
			ResumeLayout(false);
			PerformLayout();
		}

		#endregion
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
		private Label txtTitle;
	}
}