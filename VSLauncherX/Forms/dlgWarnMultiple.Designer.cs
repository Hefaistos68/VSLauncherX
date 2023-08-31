namespace VSLauncher
{
	partial class dlgWarnMultiple
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
			Label label2;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(dlgWarnMultiple));
			btnCancel = new Button();
			btnOk = new Button();
			lblItemNumber = new Label();
			chkDontShow = new CheckBox();
			label1 = new Label();
			label2 = new Label();
			SuspendLayout();
			// 
			// label1
			// 
			label1.AutoSize = true;
			label1.Location = new Point(12, 9);
			label1.Name = "label1";
			label1.Size = new Size(133, 15);
			label1.TabIndex = 0;
			label1.Text = "You are about to launch";
			// 
			// label2
			// 
			label2.Location = new Point(12, 50);
			label2.Name = "label2";
			label2.Size = new Size(355, 60);
			label2.TabIndex = 2;
			label2.Text = "Instances of Visual Studio (or other tools). This may consume a lot of time and resources.\r\n\r\nAre you sure to continue?";
			// 
			// btnCancel
			// 
			btnCancel.DialogResult = DialogResult.Cancel;
			btnCancel.Image = Resources.Cancel_24x24;
			btnCancel.Location = new Point(161, 151);
			btnCancel.Name = "btnCancel";
			btnCancel.Size = new Size(100, 40);
			btnCancel.TabIndex = 4;
			btnCancel.Text = " Cancel";
			btnCancel.TextImageRelation = TextImageRelation.ImageBeforeText;
			btnCancel.UseVisualStyleBackColor = true;
			// 
			// btnOk
			// 
			btnOk.DialogResult = DialogResult.OK;
			btnOk.Image = Resources.Check_24x24;
			btnOk.Location = new Point(267, 151);
			btnOk.Name = "btnOk";
			btnOk.Size = new Size(100, 40);
			btnOk.TabIndex = 5;
			btnOk.Text = " Ok";
			btnOk.TextImageRelation = TextImageRelation.ImageBeforeText;
			btnOk.UseVisualStyleBackColor = true;
			btnOk.Click += btnOk_Click;
			// 
			// lblItemNumber
			// 
			lblItemNumber.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
			lblItemNumber.Location = new Point(12, 27);
			lblItemNumber.Name = "lblItemNumber";
			lblItemNumber.Size = new Size(322, 23);
			lblItemNumber.TabIndex = 1;
			lblItemNumber.Text = "0";
			lblItemNumber.TextAlign = ContentAlignment.MiddleCenter;
			// 
			// chkDontShow
			// 
			chkDontShow.AutoSize = true;
			chkDontShow.Location = new Point(12, 163);
			chkDontShow.Name = "chkDontShow";
			chkDontShow.Size = new Size(118, 19);
			chkDontShow.TabIndex = 3;
			chkDontShow.Text = "Don´t show again";
			chkDontShow.UseVisualStyleBackColor = true;
			// 
			// dlgWarnMultiple
			// 
			this.AcceptButton = btnOk;
			this.AutoScaleDimensions = new SizeF(7F, 15F);
			this.AutoScaleMode = AutoScaleMode.Font;
			this.CancelButton = btnCancel;
			this.ClientSize = new Size(379, 203);
			this.ControlBox = false;
			this.Controls.Add(chkDontShow);
			this.Controls.Add(btnCancel);
			this.Controls.Add(btnOk);
			this.Controls.Add(lblItemNumber);
			this.Controls.Add(label2);
			this.Controls.Add(label1);
			this.FormBorderStyle = FormBorderStyle.FixedDialog;
			this.Icon = (Icon)resources.GetObject("$this.Icon");
			this.MaximizeBox = false;
			this.MdiChildrenMinimizedAnchorBottom = false;
			this.MinimizeBox = false;
			this.Name = "dlgWarnMultiple";
			this.SizeGripStyle = SizeGripStyle.Hide;
			this.StartPosition = FormStartPosition.CenterParent;
			this.Text = "Warning";
			ResumeLayout(false);
			PerformLayout();
		}

		#endregion

		private Button btnCancel;
		private Button btnOk;
		private Label lblItemNumber;
		private CheckBox chkDontShow;
	}
}