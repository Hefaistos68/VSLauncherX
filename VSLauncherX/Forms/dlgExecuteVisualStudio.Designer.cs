namespace VSLauncher
{
	partial class dlgExecuteVisualStudio
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
			Label label3;
			Label label4;
			btnCancel = new Button();
			btnOk = new Button();
			txtCommand = new TextBox();
			this.visualStudioCombobox1 = new VisualStudioCombobox();
			cbxInstance = new ComboBox();
			chkAdmin = new CheckBox();
			chkSplash = new CheckBox();
			btnSelectFolder = new Button();
			txtFoldername = new TextBox();
			label1 = new Label();
			label2 = new Label();
			label3 = new Label();
			label4 = new Label();
			SuspendLayout();
			// 
			// label1
			// 
			label1.AutoSize = true;
			label1.Location = new Point(12, 138);
			label1.Name = "label1";
			label1.Size = new Size(64, 15);
			label1.TabIndex = 4;
			label1.Text = "Command";
			// 
			// label2
			// 
			label2.AutoSize = true;
			label2.Location = new Point(12, 9);
			label2.Name = "label2";
			label2.Size = new Size(48, 15);
			label2.TabIndex = 0;
			label2.Text = "Version:";
			// 
			// label3
			// 
			label3.AutoSize = true;
			label3.Location = new Point(12, 78);
			label3.Name = "label3";
			label3.Size = new Size(54, 15);
			label3.TabIndex = 2;
			label3.Text = "Instance:";
			// 
			// label4
			// 
			label4.AutoSize = true;
			label4.Location = new Point(12, 201);
			label4.Name = "label4";
			label4.Size = new Size(108, 15);
			label4.TabIndex = 6;
			label4.Text = "Project or Solution:";
			// 
			// btnCancel
			// 
			btnCancel.DialogResult = DialogResult.Cancel;
			btnCancel.Image = Resources.Cancel_24x24;
			btnCancel.Location = new Point(193, 348);
			btnCancel.Name = "btnCancel";
			btnCancel.Size = new Size(100, 40);
			btnCancel.TabIndex = 11;
			btnCancel.Text = " Cancel";
			btnCancel.TextImageRelation = TextImageRelation.ImageBeforeText;
			btnCancel.UseVisualStyleBackColor = true;
			// 
			// btnOk
			// 
			btnOk.DialogResult = DialogResult.OK;
			btnOk.Image = Resources.Check_24x24;
			btnOk.Location = new Point(299, 348);
			btnOk.Name = "btnOk";
			btnOk.Size = new Size(100, 40);
			btnOk.TabIndex = 12;
			btnOk.Text = "Run";
			btnOk.TextImageRelation = TextImageRelation.ImageBeforeText;
			btnOk.UseVisualStyleBackColor = true;
			btnOk.Click += btnOk_Click;
			// 
			// txtCommand
			// 
			txtCommand.Location = new Point(12, 156);
			txtCommand.Name = "txtCommand";
			txtCommand.Size = new Size(387, 23);
			txtCommand.TabIndex = 5;
			txtCommand.TextChanged += txtInstanceName_TextChanged;
			// 
			// visualStudioCombobox1
			// 
			this.visualStudioCombobox1.DrawMode = DrawMode.OwnerDrawFixed;
			this.visualStudioCombobox1.DropDownStyle = ComboBoxStyle.DropDownList;
			this.visualStudioCombobox1.FormattingEnabled = true;
			this.visualStudioCombobox1.IntegralHeight = false;
			this.visualStudioCombobox1.ItemHeight = 26;
			this.visualStudioCombobox1.Location = new Point(12, 27);
			this.visualStudioCombobox1.Name = "visualStudioCombobox1";
			this.visualStudioCombobox1.Size = new Size(387, 32);
			this.visualStudioCombobox1.TabIndex = 1;
			this.visualStudioCombobox1.SelectedIndexChanged += visualStudioCombobox1_SelectedIndexChanged;
			// 
			// cbxInstance
			// 
			cbxInstance.FormattingEnabled = true;
			cbxInstance.Location = new Point(12, 96);
			cbxInstance.Name = "cbxInstance";
			cbxInstance.Size = new Size(387, 23);
			cbxInstance.TabIndex = 3;
			cbxInstance.SelectedIndexChanged += cbxInstance_SelectedIndexChanged;
			cbxInstance.TextUpdate += cbxInstance_TextUpdate;
			// 
			// chkAdmin
			// 
			chkAdmin.AutoSize = true;
			chkAdmin.Location = new Point(12, 274);
			chkAdmin.Name = "chkAdmin";
			chkAdmin.Size = new Size(100, 19);
			chkAdmin.TabIndex = 9;
			chkAdmin.Text = "Run as Admin";
			chkAdmin.UseVisualStyleBackColor = true;
			// 
			// chkSplash
			// 
			chkSplash.AutoSize = true;
			chkSplash.Location = new Point(12, 299);
			chkSplash.Name = "chkSplash";
			chkSplash.Size = new Size(210, 19);
			chkSplash.TabIndex = 10;
			chkSplash.Text = "Show Visual Studio Splash Window";
			chkSplash.UseVisualStyleBackColor = true;
			// 
			// btnSelectFolder
			// 
			btnSelectFolder.Location = new Point(374, 220);
			btnSelectFolder.Name = "btnSelectFolder";
			btnSelectFolder.Size = new Size(25, 25);
			btnSelectFolder.TabIndex = 8;
			btnSelectFolder.Text = "...";
			btnSelectFolder.UseVisualStyleBackColor = true;
			btnSelectFolder.Click += btnSelectFolder_Click;
			// 
			// txtFoldername
			// 
			txtFoldername.Location = new Point(12, 221);
			txtFoldername.Name = "txtFoldername";
			txtFoldername.Size = new Size(356, 23);
			txtFoldername.TabIndex = 7;
			// 
			// dlgExecuteVisualStudio
			// 
			this.AcceptButton = btnOk;
			this.AutoScaleDimensions = new SizeF(7F, 15F);
			this.AutoScaleMode = AutoScaleMode.Font;
			this.CancelButton = btnCancel;
			this.ClientSize = new Size(411, 400);
			this.ControlBox = false;
			this.Controls.Add(btnSelectFolder);
			this.Controls.Add(txtFoldername);
			this.Controls.Add(label4);
			this.Controls.Add(chkSplash);
			this.Controls.Add(chkAdmin);
			this.Controls.Add(cbxInstance);
			this.Controls.Add(this.visualStudioCombobox1);
			this.Controls.Add(btnCancel);
			this.Controls.Add(btnOk);
			this.Controls.Add(txtCommand);
			this.Controls.Add(label2);
			this.Controls.Add(label3);
			this.Controls.Add(label1);
			this.FormBorderStyle = FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MdiChildrenMinimizedAnchorBottom = false;
			this.MinimizeBox = false;
			this.Name = "dlgExecuteVisualStudio";
			this.ShowIcon = false;
			this.SizeGripStyle = SizeGripStyle.Hide;
			this.StartPosition = FormStartPosition.CenterParent;
			this.Text = "Execute Visual Studio";
			ResumeLayout(false);
			PerformLayout();
		}

		#endregion

		private Button btnCancel;
		private Button btnOk;
		private TextBox txtCommand;
		private VisualStudioCombobox visualStudioCombobox1;
		private ComboBox cbxInstance;
		private CheckBox chkAdmin;
		private CheckBox chkSplash;
		private Button btnSelectFolder;
		private TextBox txtFoldername;
	}
}