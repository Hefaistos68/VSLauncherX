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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(dlgAddFolder));
			txtFoldername = new TextBox();
			btnOk = new Button();
			btnCancel = new Button();
			label2 = new Label();
			pictureBox1 = new PictureBox();
			label1 = new Label();
			((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
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
			btnOk.Location = new Point(280, 109);
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
			btnCancel.Location = new Point(174, 109);
			btnCancel.Name = "btnCancel";
			btnCancel.Size = new Size(100, 40);
			btnCancel.TabIndex = 3;
			btnCancel.Text = " Cancel";
			btnCancel.TextImageRelation = TextImageRelation.ImageBeforeText;
			btnCancel.UseVisualStyleBackColor = true;
			// 
			// label2
			// 
			label2.Location = new Point(50, 56);
			label2.Name = "label2";
			label2.Size = new Size(321, 50);
			label2.TabIndex = 5;
			label2.Text = "Use this groups 'Settings' to set actions and properties for all contained items";
			// 
			// pictureBox1
			// 
			pictureBox1.Image = Resources.Information;
			pictureBox1.Location = new Point(12, 56);
			pictureBox1.Name = "pictureBox1";
			pictureBox1.Size = new Size(32, 32);
			pictureBox1.SizeMode = PictureBoxSizeMode.CenterImage;
			pictureBox1.TabIndex = 6;
			pictureBox1.TabStop = false;
			// 
			// dlgAddFolder
			// 
			this.AcceptButton = btnOk;
			this.AutoScaleDimensions = new SizeF(7F, 15F);
			this.AutoScaleMode = AutoScaleMode.Font;
			this.CancelButton = btnCancel;
			this.ClientSize = new Size(394, 164);
			this.ControlBox = false;
			this.Controls.Add(pictureBox1);
			this.Controls.Add(label2);
			this.Controls.Add(btnCancel);
			this.Controls.Add(btnOk);
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
			((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
			ResumeLayout(false);
			PerformLayout();
		}

		#endregion

		private Label label1;
		private TextBox txtFoldername;
		private Button btnOk;
		private Button btnCancel;
		private Label label2;
		private PictureBox pictureBox1;
	}
}