using System.Drawing;
using System.Windows.Forms;

using BrightIdeasSoftware;
using VSLauncher.Controls;

namespace VSLauncher.Helpers
{
	internal static class ThemeHelper
	{
		private static readonly ThemePalette LightPalette = new ThemePalette
		{
			Window = SystemColors.Control,
			Surface = SystemColors.Control,
			SurfaceAlt = SystemColors.ControlLight,
			SurfaceContrast = SystemColors.ControlDark,
			Text = SystemColors.ControlText,
			SubText = SystemColors.GrayText,
			Border = SystemColors.ActiveBorder,
			Accent = Color.CornflowerBlue,
			AccentDark = Color.SteelBlue,
			Selection = Color.FromArgb(204, 228, 247),
			Button = SystemColors.ControlLight,
			HierarchyLine = Color.DarkBlue
		};

		private static readonly ThemePalette DarkPalette = new ThemePalette
		{
			Window = Color.FromArgb(24, 24, 24),
			Surface = Color.FromArgb(32, 32, 32),
			SurfaceAlt = Color.FromArgb(40, 40, 40),
			SurfaceContrast = Color.FromArgb(0xa0, 0xa0, 0xa0),
			Text = Color.Gainsboro,
			SubText = Color.Silver,
			Border = Color.FromArgb(64, 64, 64),
			Accent = Color.CornflowerBlue,
			AccentDark = Color.SteelBlue,
			Selection = Color.FromArgb(60, 120, 200),
			Button = Color.FromArgb(50, 50, 50),
			HierarchyLine = Color.LightSteelBlue
		};

		internal static ThemePalette GetPalette(bool useDarkMode)
		{
			return useDarkMode ? DarkPalette : LightPalette;
		}

		internal static void ApplyTheme(Control root, ThemePalette palette)
		{
			ApplyControl(root, palette, true);
		}

		internal static void ApplyTheme(ContextMenuStrip menu, ThemePalette palette)
		{
			if (menu == null)
			{
				return;
			}

			menu.Renderer = new ToolStripProfessionalRenderer(new ThemeColorTable(palette));
			menu.BackColor = palette.SurfaceAlt;
			menu.ForeColor = palette.Text;

			foreach (ToolStripItem item in menu.Items)
			{
				item.ForeColor = palette.Text;
			}
		}

		private static void ApplyControl(Control control, ThemePalette palette, bool isRoot)
		{
			if (control is Form)
			{
				control.BackColor = palette.Window;
			}
			else if (control is StatusStrip or ToolStrip)
			{
				control.BackColor = palette.SurfaceAlt;
			}
			else
			{
				control.BackColor = palette.Surface;
			}

			control.ForeColor = palette.Text;

			switch (control)
			{
				case TreeListView treeListView:
					ApplyTreeListView(treeListView, palette);
					break;
				case VisualStudioCombobox vsCombo:
					vsCombo.ApplyTheme(palette);
					break;
				case Label label:
                    bool isHeaderText = label.Parent?.Name == "leftSubPanel" ||  label.Parent?.Name == "flowLayoutPanel2" ||  label.Parent?.Name == "flowLayoutPanel1";
					label.BackColor = isHeaderText ? palette.SurfaceContrast : palette.SurfaceAlt;
					label.ForeColor = isHeaderText ? palette.AccentDark : palette.Text;
					break;

                case TextBoxBase textBox:
                    textBox.BackColor = palette.SurfaceAlt;
					textBox.ForeColor = palette.Text;
					textBox.BorderStyle = BorderStyle.FixedSingle;
					break;

				case Button button:
					bool isHeaderButton = button.Parent?.Name == "flowLayoutPanel2";
					button.FlatStyle = FlatStyle.Flat;
					button.FlatAppearance.BorderColor = palette.Border;
					button.FlatAppearance.MouseOverBackColor = palette.Accent;
					button.FlatAppearance.MouseDownBackColor = palette.AccentDark;
					button.BackColor = isHeaderButton ? palette.SurfaceContrast : palette.Button;
					button.ForeColor = palette.Text;
					button.UseVisualStyleBackColor = false;
					break;
				
				case TableLayoutPanel tableLayoutPanel:
					if (tableLayoutPanel.Name == "leftSubPanel" 
						|| tableLayoutPanel.Name == "flowLayoutPanel2"
						|| tableLayoutPanel.Name == "mainPanel")
					{
						tableLayoutPanel.BackColor = palette.SurfaceContrast;
					}

					break;

				case FlowLayoutPanel flowLayoutPanel:
					if (flowLayoutPanel.Name == "flowLayoutPanel2" || flowLayoutPanel.Name == "flowLayoutPanel1")
					{
                        flowLayoutPanel.BackColor = palette.SurfaceContrast;
					}

					break;
			}

			foreach (Control child in control.Controls)
			{
				ApplyControl(child, palette, false);
			}
		}

		private static void ApplyTreeListView(TreeListView treeListView, ThemePalette palette)
		{
			treeListView.BackColor = palette.Surface;
			treeListView.ForeColor = palette.Text;
			treeListView.AlternateRowBackColor = palette.SurfaceAlt;
			treeListView.HeaderUsesThemes = false;
			treeListView.HeaderFormatStyle = new HeaderFormatStyle
			{
				Normal = new HeaderStateStyle
				{
					BackColor = palette.SurfaceAlt,
					ForeColor = palette.Text
				}
			};
			treeListView.SelectedBackColor = palette.Selection;
			treeListView.UnfocusedSelectedBackColor= palette.Selection;

            Pen linePen = treeListView.TreeColumnRenderer.LinePen;
            if (linePen != null)
            {
                linePen.Color = palette.HierarchyLine;
            }
        }

		internal readonly struct ThemePalette
		{
			internal Color Window { get; init; }
			internal Color Surface { get; init; }
			internal Color SurfaceAlt { get; init; }
			internal Color SurfaceContrast { get; init; }
			internal Color Text { get; init; }
			internal Color SubText { get; init; }
			internal Color Border { get; init; }
			internal Color Accent { get; init; }
			internal Color AccentDark { get; init; }
			internal Color Selection { get; init; }
			internal Color Button { get; init; }
			internal Color HierarchyLine { get; init; }
		}

		private sealed class ThemeColorTable : ProfessionalColorTable
		{
			private readonly ThemePalette palette;

			internal ThemeColorTable(ThemePalette palette)
			{
				this.palette = palette;
			}

			public override Color MenuItemSelected => this.palette.Selection;
			public override Color MenuItemBorder => this.palette.Border;
			public override Color ToolStripDropDownBackground => this.palette.SurfaceAlt;
			public override Color ImageMarginGradientBegin => this.palette.SurfaceAlt;
			public override Color ImageMarginGradientMiddle => this.palette.SurfaceAlt;
			public override Color ImageMarginGradientEnd => this.palette.SurfaceAlt;
		}
	}
}