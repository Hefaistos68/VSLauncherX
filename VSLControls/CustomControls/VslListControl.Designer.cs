using System.Windows.Forms;

namespace CustomControls
{
	/// <summary>
	/// The vsl list control.
	/// </summary>
	partial class VslListControl
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
			components = new System.ComponentModel.Container();
			this.flowpanelListBox = new System.Windows.Forms.FlowLayoutPanel();
			base.SuspendLayout();
			this.flowpanelListBox.AutoScroll = true;
			this.flowpanelListBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.flowpanelListBox.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
			this.flowpanelListBox.Location = new System.Drawing.Point(0, 0);
			this.flowpanelListBox.Margin = new System.Windows.Forms.Padding(0);
			this.flowpanelListBox.Name = "flpListBox";
			this.flowpanelListBox.Size = new System.Drawing.Size(148, 148);
			this.flowpanelListBox.TabIndex = 0;
			this.flowpanelListBox.WrapContents = false;
			base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
			base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.SystemColors.Window;
			base.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			base.Controls.Add(this.flowpanelListBox);
			base.Margin = new System.Windows.Forms.Padding(0);
			base.Name = "VslListControl";
			base.Size = new System.Drawing.Size(148, 148);
			base.ResumeLayout(false);
		}

		private FlowLayoutPanel flowpanelListBox;

		#endregion
	}
}
