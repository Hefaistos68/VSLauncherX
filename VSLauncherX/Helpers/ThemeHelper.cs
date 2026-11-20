using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using BrightIdeasSoftware;
using VSLauncher.Controls;

namespace VSLauncher.Helpers
{
	/// <summary>
	/// Applies consistent light and dark theme palettes across forms, controls, and context menus.
	/// </summary>
	internal static class ThemeHelper
	{
		private const int DwmwaUseImmersiveDarkModeBefore20H1 = 19;
		private const int DwmwaUseImmersiveDarkMode           = 20;
		private const uint SwpNoSize                          = 0x0001;
		private const uint SwpNoMove                          = 0x0002;
		private const uint SwpNoZOrder                        = 0x0004;
		private const uint SwpNoActivate                      = 0x0010;
		private const uint SwpFrameChanged                    = 0x0020;

		[DllImport("dwmapi.dll", PreserveSig = true)]
		private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

		[DllImport("uxtheme.dll", EntryPoint = "#133", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool AllowDarkModeForWindow(IntPtr hWnd, [MarshalAs(UnmanagedType.Bool)] bool allow);

		[DllImport("uxtheme.dll", EntryPoint = "#135", SetLastError = true, CharSet = CharSet.Unicode)]
		private static extern int SetPreferredAppMode(int preferredAppMode);

		[DllImport("uxtheme.dll", EntryPoint = "#136", SetLastError = true, CharSet = CharSet.Unicode)]
		private static extern void FlushMenuThemes();

		[DllImport("uxtheme.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
		private static extern int SetWindowTheme(IntPtr hWnd, string? pszSubAppName, string? pszSubIdList);

		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

		private static readonly ThemePalette LightPalette = new ThemePalette
		{
			IsDark          = false,
			Window          = SystemColors.Control,
			Surface         = SystemColors.Control,
			SurfaceAlt      = SystemColors.ControlLight,
			SurfaceContrast = SystemColors.ControlDark,
			Text            = SystemColors.ControlText,
			SubText         = SystemColors.GrayText,
			DisabledText    = SystemColors.GrayText,
			Border          = SystemColors.ActiveBorder,
			Accent          = Color.CornflowerBlue,
			AccentDark      = Color.SteelBlue,
			Selection       = Color.FromArgb(204, 228, 247),
			SelectionText   = SystemColors.ControlText,
			Button          = SystemColors.ControlLight
		};

		private static readonly ThemePalette DarkPalette = new ThemePalette
		{
			IsDark          = true,
			Window          = Color.FromArgb(24, 24, 24),
			Surface         = Color.FromArgb(32, 32, 32),
			SurfaceAlt      = Color.FromArgb(40, 40, 40),
			SurfaceContrast = Color.FromArgb(0xa0, 0xa0, 0xa0),
			Text            = Color.Gainsboro,
			SubText         = Color.Silver,
			DisabledText    = Color.FromArgb(130, 130, 130),
			Border          = Color.FromArgb(64, 64, 64),
			Accent          = Color.CornflowerBlue,
			AccentDark      = Color.SteelBlue,
			Selection       = Color.FromArgb(60, 120, 200),
			SelectionText   = Color.White,
			Button          = Color.FromArgb(50, 50, 50)
		};

		private static readonly SolidBrush LightTreeFillBrush = new(LightPalette.Accent);
		private static readonly Pen LightTreeFramePen         = new(LightPalette.Border);
		private static readonly SolidBrush DarkTreeFillBrush  = new(DarkPalette.Accent);
		private static readonly Pen DarkTreeFramePen          = new(DarkPalette.Border);
		private static readonly Pen LightTreeLinePen          = CreateDottedPen(Color.FromArgb(120, 140, 180));
		private static readonly Pen DarkTreeLinePen           = CreateDottedPen(Color.FromArgb(165, 180, 200));
		private static readonly Color DarkFolderIconColor     = Color.FromArgb(235, 195, 95);
		private static readonly Color DarkGitCleanColor       = Color.FromArgb(72, 218, 120);
		private static readonly Color DarkGitDirtyColor       = Color.FromArgb(255, 108, 74);
		private static readonly ConditionalWeakTable<Control, NativeScrollbarThemeState> NativeScrollbarStates = new();

		private static Bitmap? darkFolderIcon32;
		private static Bitmap? darkFolderIcon16;
		private static Bitmap? darkGitCleanIcon;
		private static Bitmap? darkGitDirtyIcon;

		/// <summary>
		/// Creates a 1px dotted pen with the specified color.
		/// </summary>
		/// <param name="color">The pen color.</param>
		/// <returns>A configured dotted <see cref="Pen"/>.</returns>
		private static Pen CreateDottedPen(Color color)
		{
			return new Pen(color, 1.0F)
			{
				DashStyle = DashStyle.Dot
			};
		}

		/// <summary>
		/// Gets a folder icon appropriate for the current light or dark theme.
		/// </summary>
		/// <param name="large">True for 32px list icons, false for 16px import icons.</param>
		/// <returns>An <see cref="Icon"/> or <see cref="Bitmap"/> for rendering.</returns>
		internal static object GetFolderIcon(bool large)
		{
			if (!Properties.Settings.Default.DarkMode)
			{
				return large ? Program.VisualStudioFileIcons32.GetIcon("Folder") : Program.VisualStudioFileIcons16.GetIcon("Folder");
			}

			if (large)
			{
				darkFolderIcon32 ??= CreateTintedIconBitmap(Program.VisualStudioFileIcons32.GetIcon("Folder"), DarkFolderIconColor, 32);

				return darkFolderIcon32;
			}

			darkFolderIcon16 ??= CreateTintedIconBitmap(Program.VisualStudioFileIcons16.GetIcon("Folder"), DarkFolderIconColor, 16);

			return darkFolderIcon16;
		}

		/// <summary>
		/// Gets a Git status icon appropriate for the current light or dark theme.
		/// </summary>
		/// <param name="isDirty">True for dirty status, false for clean status.</param>
		/// <returns>A themed status <see cref="Bitmap"/>.</returns>
		internal static Bitmap GetGitStatusIcon(bool isDirty)
		{
			if (!Properties.Settings.Default.DarkMode)
			{
				return isDirty ? Resources.GitDirty2 : Resources.GitClean2;
			}

			if (isDirty)
			{
				darkGitDirtyIcon ??= CreateDarkGitStatusBitmap(Resources.GitDirty2, DarkGitDirtyColor);

				return darkGitDirtyIcon;
			}

			darkGitCleanIcon ??= CreateDarkGitStatusBitmap(Resources.GitClean2, DarkGitCleanColor);

			return darkGitCleanIcon;
		}

		/// <summary>
		/// Converts a monochrome icon into a high-contrast tinted bitmap while preserving alpha.
		/// </summary>
		/// <param name="sourceIcon">The source monochrome icon.</param>
		/// <param name="tintColor">The target foreground color.</param>
		/// <param name="targetSize">Target width and height in pixels.</param>
		/// <returns>A tinted <see cref="Bitmap"/>.</returns>
		private static Bitmap CreateTintedIconBitmap(Icon sourceIcon, Color tintColor, int targetSize)
		{
			using Bitmap raw = sourceIcon.ToBitmap();
			using Bitmap resized = (raw.Width == targetSize && raw.Height == targetSize)
				? (Bitmap)raw.Clone()
				: new Bitmap(raw, new Size(targetSize, targetSize));

			var result = new Bitmap(resized.Width, resized.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

			for (int y = 0; y < resized.Height; y++)
			{
				for (int x = 0; x < resized.Width; x++)
				{
					Color pixel = resized.GetPixel(x, y);

					if (pixel.A > 0)
					{
						result.SetPixel(x, y, Color.FromArgb(pixel.A, tintColor.R, tintColor.G, tintColor.B));
					}
				}
			}

			return result;
		}

		/// <summary>
		/// Builds a high-contrast dark-mode Git status diamond with a vivid fill and crisp white interior glyph.
		/// </summary>
		/// <param name="source">The original light-mode status bitmap.</param>
		/// <param name="fillColor">The vivid dark-mode diamond color.</param>
		/// <returns>A high-contrast <see cref="Bitmap"/>.</returns>
		private static Bitmap CreateDarkGitStatusBitmap(Bitmap source, Color fillColor)
		{
			var result = new Bitmap(source.Width, source.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			float cx = (source.Width - 1) / 2.0F;
			float cy = (source.Height - 1) / 2.0F;
			float innerRadius = Math.Min(cx, cy) - 1.0F;

			for (int y = 0; y < source.Height; y++)
			{
				for (int x = 0; x < source.Width; x++)
				{
					Color src = source.GetPixel(x, y);
					float manhattan = Math.Abs(x - cx) + Math.Abs(y - cy);

					if (manhattan <= innerRadius)
					{
						float alpha = src.A / 255.0F;
						int r = (int)Math.Round((fillColor.R * alpha) + (255.0F * (1.0F - alpha)));
						int g = (int)Math.Round((fillColor.G * alpha) + (255.0F * (1.0F - alpha)));
						int b = (int)Math.Round((fillColor.B * alpha) + (255.0F * (1.0F - alpha)));

						result.SetPixel(x, y, Color.FromArgb(255, r, g, b));
					}
					else if (src.A > 0)
					{
						result.SetPixel(x, y, Color.FromArgb(src.A, fillColor.R, fillColor.G, fillColor.B));
					}
				}
			}

			return result;
		}

		/// <summary>
		/// Gets the palette for the requested theme mode.
		/// </summary>
		/// <param name="useDarkMode">Whether dark mode is enabled.</param>
		/// <returns>The matching <see cref="ThemePalette"/>.</returns>
		internal static ThemePalette GetPalette(bool useDarkMode)
		{
			return useDarkMode ? DarkPalette : LightPalette;
		}

		/// <summary>
		/// Applies the currently configured application theme to a form and its child controls.
		/// </summary>
		/// <param name="form">The form to theme.</param>
		internal static void ApplyCurrentTheme(Form form)
		{
			ApplyTheme(form, GetPalette(Properties.Settings.Default.DarkMode));
		}

		/// <summary>
		/// Applies a theme palette to a root control and all of its descendants.
		/// </summary>
		/// <param name="root">The root control or form.</param>
		/// <param name="palette">The theme palette to apply.</param>
		internal static void ApplyTheme(Control root, ThemePalette palette)
		{
			if (root is Form form)
			{
				ApplyAppDarkModePolicy(palette.IsDark);
				ToolStripManager.Renderer = new ThemeToolStripRenderer(palette);
				ApplyTitleBarTheme(form, palette.IsDark);
			}

			ApplyControl(root, palette);
		}

		/// <summary>
		/// Applies a theme palette recursively to a context menu or dropdown and all nested submenus.
		/// </summary>
		/// <param name="menu">The menu strip or dropdown to theme.</param>
		/// <param name="palette">The theme palette to apply.</param>
		internal static void ApplyTheme(ToolStripDropDown? menu, ThemePalette palette)
		{
			if (menu is null)
			{
				return;
			}

			var renderer = new ThemeToolStripRenderer(palette);
			ApplyToolStripDropDown(menu, palette, renderer);
		}

		/// <summary>
		/// Recursively styles a dropdown menu and all nested submenu dropdowns and items.
		/// </summary>
		/// <param name="dropDown">The dropdown to style.</param>
		/// <param name="palette">The palette to apply.</param>
		/// <param name="renderer">Shared themed renderer instance.</param>
		private static void ApplyToolStripDropDown(ToolStripDropDown dropDown, ThemePalette palette, ToolStripRenderer renderer)
		{
			dropDown.Renderer  = renderer;
			dropDown.BackColor = palette.SurfaceAlt;
			dropDown.ForeColor = palette.Text;

			foreach (ToolStripItem item in dropDown.Items)
			{
				item.BackColor = palette.SurfaceAlt;
				item.ForeColor = palette.Text;

				if (item is ToolStripDropDownItem dropDownItem && dropDownItem.HasDropDownItems)
				{
					ApplyToolStripDropDown(dropDownItem.DropDown, palette, renderer);
				}
			}
		}

		/// <summary>
		/// Enables or disables the Windows immersive dark mode window title bar for a form.
		/// </summary>
		/// <param name="form">The target form.</param>
		/// <param name="useDarkMode">Whether dark mode is enabled.</param>
		private static void ApplyTitleBarTheme(Form form, bool useDarkMode)
		{
			if (form.IsHandleCreated)
			{
				SetImmersiveDarkMode(form.Handle, useDarkMode);

				return;
			}

			void OnHandleCreated(object? sender, EventArgs e)
			{
				form.HandleCreated -= OnHandleCreated;
				SetImmersiveDarkMode(form.Handle, Properties.Settings.Default.DarkMode);
			}

			form.HandleCreated += OnHandleCreated;
		}

		/// <summary>
		/// Updates the process-wide Windows dark mode preference used for native scrollbars and menus.
		/// </summary>
		/// <param name="useDarkMode">Whether dark mode is enabled.</param>
		private static void ApplyAppDarkModePolicy(bool useDarkMode)
		{
			try
			{
				_ = SetPreferredAppMode(useDarkMode ? 2 : 0);
				FlushMenuThemes();
			}
			catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
			{
			}
		}

		/// <summary>
		/// Sets the DWM immersive dark mode attribute on a native window handle.
		/// </summary>
		/// <param name="handle">The window handle.</param>
		/// <param name="useDarkMode">Whether to enable dark title bar rendering.</param>
		private static void SetImmersiveDarkMode(IntPtr handle, bool useDarkMode)
		{
			if (handle == IntPtr.Zero)
			{
				return;
			}

			try
			{
				int darkFlag = useDarkMode ? 1 : 0;

				if (DwmSetWindowAttribute(handle, DwmwaUseImmersiveDarkMode, ref darkFlag, sizeof(int)) != 0)
				{
					_ = DwmSetWindowAttribute(handle, DwmwaUseImmersiveDarkModeBefore20H1, ref darkFlag, sizeof(int));
				}

				_ = AllowDarkModeForWindow(handle, useDarkMode);
				ApplyAppDarkModePolicy(useDarkMode);
			}
			catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
			{
			}
		}

		private static void ApplyControl(Control control, ThemePalette palette)
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

			ApplySpecificControlTheme(control, palette);

			foreach (Control child in control.Controls)
			{
				ApplyControl(child, palette);
			}
		}

		/// <summary>
		/// Applies type-specific theme styling to an individual control.
		/// </summary>
		/// <param name="control">The control to style.</param>
		/// <param name="palette">The active theme palette.</param>
		private static void ApplySpecificControlTheme(Control control, ThemePalette palette)
		{
			switch (control)
			{
				case TreeListView treeListView:
					ApplyTreeListView(treeListView, palette);
					break;

				case IdeCombobox ideCombo:
					ideCombo.ApplyTheme(palette);
					ApplyNativeScrollbarTheme(ideCombo, palette.IsDark, lightThemeName: null);
					break;

				case ThemedTabControl themedTabControl:
					themedTabControl.ApplyTheme(palette);
					break;

				case TabPage tabPage:
					ApplyTabPageTheme(tabPage, palette);
					break;

				case ComboBox comboBox:
					comboBox.BackColor = palette.SurfaceAlt;
					comboBox.ForeColor = palette.Text;
					comboBox.FlatStyle = palette.IsDark ? FlatStyle.Flat : FlatStyle.Standard;
					ApplyNativeScrollbarTheme(comboBox, palette.IsDark, lightThemeName: null);
					break;

				case LinkLabel linkLabel:
					linkLabel.BackColor        = linkLabel.Parent is Form ? palette.Window : palette.Surface;
					linkLabel.ForeColor        = palette.Text;
					linkLabel.LinkColor        = palette.IsDark ? palette.Accent : SystemColors.HotTrack;
					linkLabel.ActiveLinkColor  = palette.SelectionText;
					linkLabel.VisitedLinkColor = palette.AccentDark;
					break;

				case Label label:
					ApplyLabelTheme(label, palette);
					break;

				case CheckBox checkBox:
					checkBox.BackColor               = checkBox.Parent is Form ? palette.Window : palette.Surface;
					checkBox.ForeColor               = palette.Text;
					checkBox.UseVisualStyleBackColor = !palette.IsDark;
					break;

				case PictureBox pictureBox:
					pictureBox.BackColor = pictureBox.Parent is Form ? palette.Window : palette.Surface;
					break;

				case TextBoxBase textBox:
					textBox.BackColor   = palette.SurfaceAlt;
					textBox.ForeColor   = palette.Text;
					textBox.BorderStyle = BorderStyle.FixedSingle;
					break;

				case Button button:
					bool isHeaderButton = button.Parent?.Name == "flowLayoutPanel2";
					button.FlatStyle                        = FlatStyle.Flat;
					button.FlatAppearance.BorderColor       = palette.Border;
					button.FlatAppearance.MouseOverBackColor = palette.Accent;
					button.FlatAppearance.MouseDownBackColor = palette.AccentDark;
					button.BackColor                        = isHeaderButton ? palette.SurfaceContrast : palette.Button;
					button.ForeColor                        = palette.Text;
					button.UseVisualStyleBackColor          = false;
					break;

				case ToolStrip toolStrip:
					toolStrip.Renderer = new ThemeToolStripRenderer(palette);

					foreach (ToolStripItem item in toolStrip.Items)
					{
						item.BackColor = palette.SurfaceAlt;
						item.ForeColor = palette.Text;
					}

					break;

				case TableLayoutPanel tableLayoutPanel:
					if (tableLayoutPanel.Name == "leftSubPanel" || tableLayoutPanel.Name == "flowLayoutPanel2")
					{
						tableLayoutPanel.BackColor = palette.SurfaceContrast;
					}

					break;

				case FlowLayoutPanel flowLayoutPanel:
					if (flowLayoutPanel.Name == "flowLayoutPanel2")
					{
						flowLayoutPanel.BackColor = palette.SurfaceContrast;
					}

					ApplyNativeScrollbarTheme(flowLayoutPanel, palette.IsDark, lightThemeName: null);
					break;

				case GroupBox groupBox:
					groupBox.BackColor = groupBox.Parent is Form ? palette.Window : palette.Surface;
					groupBox.ForeColor = palette.Text;
					break;

				case ListView listView:
					listView.BackColor   = palette.SurfaceAlt;
					listView.ForeColor   = palette.Text;
					listView.BorderStyle = palette.IsDark ? BorderStyle.None : BorderStyle.Fixed3D;
					ApplyNativeScrollbarTheme(listView, palette.IsDark, lightThemeName: "Explorer");
					break;
			}
		}

		/// <summary>
		/// Applies Windows dark or light native scrollbar theming to <paramref name="control"/> immediately and on every subsequent handle creation.
		/// </summary>
		/// <param name="control">The control whose native scrollbars should match the active theme.</param>
		/// <param name="useDarkMode">Whether dark mode is enabled.</param>
		/// <param name="lightThemeName">The native visual style name to restore in light mode, or <c>null</c> for default.</param>
		private static void ApplyNativeScrollbarTheme(Control control, bool useDarkMode, string? lightThemeName)
		{
			NativeScrollbarThemeState state = NativeScrollbarStates.GetOrCreateValue(control);

			state.UseDarkMode    = useDarkMode;
			state.LightThemeName = lightThemeName;

			if (!state.Hooked)
			{
				state.Hooked           = true;
				control.HandleCreated += OnThemedScrollbarHandleCreated;
			}

			if (control.IsHandleCreated)
			{
				ApplyNativeScrollbarThemeToHandle(control.Handle, state);
			}
		}

		/// <summary>
		/// Re-applies native dark or light scrollbar styling whenever a tracked control creates or recreates its Win32 handle.
		/// </summary>
		/// <param name="sender">The source control.</param>
		/// <param name="e">The event arguments.</param>
		private static void OnThemedScrollbarHandleCreated(object? sender, EventArgs e)
		{
			if (sender is not Control control
				|| !control.IsHandleCreated
				|| !NativeScrollbarStates.TryGetValue(control, out NativeScrollbarThemeState? state))
			{
				return;
			}

			ApplyNativeScrollbarThemeToHandle(control.Handle, state);

			// ObjectListView.OnHandleCreated invokes OnControlCreated right after base.OnHandleCreated,
			// which resets SetWindowTheme via UseExplorerTheme; re-apply after that message completes.
			if (control is ObjectListView)
			{
				control.BeginInvoke(() =>
				{
					if (!control.IsDisposed && control.IsHandleCreated)
					{
						ApplyNativeScrollbarThemeToHandle(control.Handle, state);
					}
				});
			}
		}

		/// <summary>
		/// Applies <c>AllowDarkModeForWindow</c> and <c>SetWindowTheme</c> to a live window handle and refreshes its non-client frame.
		/// </summary>
		/// <param name="handle">The Win32 window handle.</param>
		/// <param name="state">The desired scrollbar theme state.</param>
		private static void ApplyNativeScrollbarThemeToHandle(IntPtr handle, NativeScrollbarThemeState state)
		{
			if (handle == IntPtr.Zero)
			{
				return;
			}

			try
			{
				string? themeName = state.UseDarkMode ? "DarkMode_Explorer" : state.LightThemeName;

				_ = AllowDarkModeForWindow(handle, state.UseDarkMode);
				_ = SetWindowTheme(handle, themeName, null);
				_ = SetWindowPos(handle, IntPtr.Zero, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpNoZOrder | SwpNoActivate | SwpFrameChanged);
			}
			catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
			{
			}
		}

		/// <summary>
		/// Applies light or dark theme styling to a <see cref="TabPage"/>.
		/// </summary>
		/// <param name="tabPage">The tab page to style.</param>
		/// <param name="palette">The active theme palette.</param>
		private static void ApplyTabPageTheme(TabPage tabPage, ThemePalette palette)
		{
			tabPage.BackColor               = palette.Surface;
			tabPage.ForeColor               = palette.Text;
			tabPage.UseVisualStyleBackColor = !palette.IsDark;
		}

		/// <summary>
		/// Tracks the native scrollbar dark-mode state for a Win32-backed control across handle recreations.
		/// </summary>
		private sealed class NativeScrollbarThemeState
		{
			internal bool UseDarkMode { get; set; }

			internal string? LightThemeName { get; set; }

			internal bool Hooked { get; set; }
		}

		/// <summary>
		/// Applies background color styling to a <see cref="Label"/> based on its container.
		/// </summary>
		/// <param name="label">The label to style.</param>
		/// <param name="palette">The active theme palette.</param>
		private static void ApplyLabelTheme(Label label, ThemePalette palette)
		{
			bool isHeaderText = label.Parent?.Name == "leftSubPanel" || label.Parent?.Name == "flowLayoutPanel2";

			if (isHeaderText)
			{
				label.BackColor = palette.SurfaceContrast;
			}
			else if (label.Parent is TextBoxBase)
			{
				label.BackColor = palette.SurfaceAlt;
			}
			else if (label.Parent is Form)
			{
				label.BackColor = palette.Window;
			}
			else
			{
				label.BackColor = palette.Surface;
			}
		}

		private static void ApplyTreeListView(TreeListView treeListView, ThemePalette palette)
		{
			treeListView.BorderStyle                = palette.IsDark ? BorderStyle.None : BorderStyle.Fixed3D;
			treeListView.BackColor                  = palette.Surface;
			treeListView.ForeColor                  = palette.Text;
			treeListView.AlternateRowBackColor      = palette.SurfaceAlt;
			treeListView.HeaderUsesThemes           = false;
			treeListView.HeaderFormatStyle          = new HeaderFormatStyle
			{
				Normal = new HeaderStateStyle
				{
					BackColor = palette.SurfaceAlt,
					ForeColor = palette.Text
				},
				Hot = new HeaderStateStyle
				{
					BackColor = palette.Button,
					ForeColor = palette.Text
				},
				Pressed = new HeaderStateStyle
				{
					BackColor = palette.Selection,
					ForeColor = palette.SelectionText
				}
			};
			treeListView.SelectedBackColor          = palette.Selection;
			treeListView.UnfocusedSelectedBackColor = palette.Selection;
			treeListView.SelectedForeColor          = palette.SelectionText;
			treeListView.UnfocusedSelectedForeColor = palette.SelectionText;

			TreeListView.TreeRenderer renderer = EnsureThemedTreeRenderer(treeListView);
			renderer.FillBrush = palette.IsDark ? DarkTreeFillBrush : LightTreeFillBrush;
			renderer.FramePen  = palette.IsDark ? DarkTreeFramePen : LightTreeFramePen;
			renderer.LinePen   = palette.IsDark ? DarkTreeLinePen : LightTreeLinePen;

			if (renderer is ThemedTreeRenderer themedRenderer)
			{
				themedRenderer.UseDarkGlyphs = palette.IsDark;
				themedRenderer.GlyphColor    = palette.Text;
				themedRenderer.GlyphHotColor = palette.SelectionText;
			}

			if (renderer.SubRenderer is DescribedTaskRenderer describedTaskRenderer)
			{
				describedTaskRenderer.TitleColor       = palette.Text;
				describedTaskRenderer.DescriptionColor = palette.SubText;
			}

			ApplyNativeScrollbarTheme(treeListView, palette.IsDark, lightThemeName: null);
			treeListView.Invalidate();
		}

		/// <summary>
		/// Ensures the <see cref="TreeListView"/> uses a <see cref="ThemedTreeRenderer"/> while preserving renderer configuration.
		/// </summary>
		/// <param name="treeListView">The tree list view.</param>
		/// <returns>The active <see cref="TreeListView.TreeRenderer"/>.</returns>
		private static TreeListView.TreeRenderer EnsureThemedTreeRenderer(TreeListView treeListView)
		{
			if (treeListView.TreeColumnRenderer is ThemedTreeRenderer existing)
			{
				return existing;
			}

			var current = treeListView.TreeColumnRenderer;
			var themed = new ThemedTreeRenderer();

			if (current != null)
			{
				themed.IsShowLines     = current.IsShowLines;
				themed.IsShowGlyphs    = current.IsShowGlyphs;
				themed.UseTriangles    = current.UseTriangles;
				themed.CornerRoundness = current.CornerRoundness;
				themed.SubRenderer     = current.SubRenderer;
			}

			treeListView.TreeColumnRenderer = themed;

			return themed;
		}

		/// <summary>
		/// TreeRenderer subclass that renders high-contrast expansion glyphs in dark mode.
		/// </summary>
		private sealed class ThemedTreeRenderer : TreeListView.TreeRenderer
		{
			[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
			internal bool UseDarkGlyphs { get; set; }

			[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
			internal Color GlyphColor { get; set; } = Color.Gainsboro;

			[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
			internal Color GlyphHotColor { get; set; } = Color.White;

			/// <inheritdoc/>
			protected override void DrawExpansionGlyph(Graphics g, Rectangle r, bool isExpanded)
			{
				if (!this.UseDarkGlyphs)
				{
					base.DrawExpansionGlyph(g, r, isExpanded);

					return;
				}

				if (this.UseTriangles && this.IsShowLines)
				{
					using var bgBrush = new SolidBrush(GetBackgroundColor());
					Rectangle clearRect = new Rectangle(r.X + 2, r.Y + 2, r.Width - 2, r.Height - 4);

					g.FillRectangle(bgBrush, clearRect);
				}

				int cx = r.X + (r.Width / 2);
				int cy = r.Y + (r.Height / 2);
				Color color = this.IsExpansionHot ? this.GlyphHotColor : this.GlyphColor;

				SmoothingMode previousMode = g.SmoothingMode;
				g.SmoothingMode = SmoothingMode.AntiAlias;

				if (isExpanded)
				{
					Point[] expandedTriangle =
					{
						new Point(cx - 4, cy - 2),
						new Point(cx + 4, cy - 2),
						new Point(cx, cy + 3)
					};
					using var fillBrush = new SolidBrush(color);

					g.FillPolygon(fillBrush, expandedTriangle);
				}
				else
				{
					Point[] collapsedTriangle =
					{
						new Point(cx - 3, cy - 4),
						new Point(cx + 3, cy),
						new Point(cx - 3, cy + 4)
					};
					using var pen = new Pen(color, 1.4F);

					g.DrawPolygon(pen, collapsedTriangle);
				}

				g.SmoothingMode = previousMode;
			}
		}

		internal readonly struct ThemePalette
		{
			internal bool IsDark { get; init; }
			internal Color Window { get; init; }
			internal Color Surface { get; init; }
			internal Color SurfaceAlt { get; init; }
			internal Color SurfaceContrast { get; init; }
			internal Color Text { get; init; }
			internal Color SubText { get; init; }
			internal Color DisabledText { get; init; }
			internal Color Border { get; init; }
			internal Color Accent { get; init; }
			internal Color AccentDark { get; init; }
			internal Color Selection { get; init; }
			internal Color SelectionText { get; init; }
			internal Color Button { get; init; }
			internal Color HierarchyLine { get; init; }
		}

		/// <summary>
		/// Custom professional renderer ensuring consistent menu item text, selection, submenu, arrow, separator, and checkmark colors.
		/// </summary>
		private sealed class ThemeToolStripRenderer : ToolStripProfessionalRenderer
		{
			private readonly ThemePalette palette;

			internal ThemeToolStripRenderer(ThemePalette palette) : base(new ThemeColorTable(palette))
			{
				this.palette  = palette;
				RoundedEdges  = false;
			}

			/// <inheritdoc/>
			protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
			{
				Rectangle rect = new Rectangle(Point.Empty, e.Item.Size);

				if (e.Item.Enabled && (e.Item.Selected || e.Item.Pressed))
				{
					Rectangle highlightRect = new Rectangle(2, 0, Math.Max(0, rect.Width - 4), rect.Height);

					using var brush = new SolidBrush(this.palette.Selection);
					using var pen = new Pen(this.palette.Border);
					e.Graphics.FillRectangle(brush, highlightRect);
					e.Graphics.DrawRectangle(pen, highlightRect.X, highlightRect.Y, Math.Max(0, highlightRect.Width - 1), Math.Max(0, highlightRect.Height - 1));

					return;
				}

				using var bgBrush = new SolidBrush(this.palette.SurfaceAlt);
				e.Graphics.FillRectangle(bgBrush, rect);
			}

			/// <inheritdoc/>
			protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
			{
				Color textColor;

				if (!e.Item.Enabled)
				{
					textColor = this.palette.DisabledText;
				}
				else if (e.Item.Selected || e.Item.Pressed)
				{
					textColor = this.palette.SelectionText;
				}
				else
				{
					textColor = this.palette.Text;
				}

				e.TextColor = textColor;
				TextRenderer.DrawText(e.Graphics, e.Text, e.TextFont, e.TextRectangle, textColor, e.TextFormat);
			}

			/// <inheritdoc/>
			protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
			{
				if (e.Item != null)
				{
					e.ArrowColor = !e.Item.Enabled
						? this.palette.DisabledText
						: (e.Item.Selected || e.Item.Pressed ? this.palette.SelectionText : this.palette.Text);
				}

				base.OnRenderArrow(e);
			}

			/// <inheritdoc/>
			protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
			{
				if (e.Vertical)
				{
					base.OnRenderSeparator(e);

					return;
				}

				int y = e.Item.ContentRectangle.Height / 2;
				int left = e.ToolStrip is ToolStripDropDownMenu ? 26 : 0;

				using var pen = new Pen(this.palette.Border);
				e.Graphics.DrawLine(pen, left, y, e.Item.Width - 4, y);
			}

			/// <inheritdoc/>
			protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
			{
				if (!this.palette.IsDark)
				{
					base.OnRenderItemCheck(e);

					return;
				}

				Rectangle box = new Rectangle(e.ImageRectangle.Left - 2, e.ImageRectangle.Top - 2, e.ImageRectangle.Width + 4, e.ImageRectangle.Height + 4);
				Color fill = e.Item.Selected ? this.palette.AccentDark : this.palette.Button;

				using var fillBrush = new SolidBrush(fill);
				using var borderPen = new Pen(this.palette.Border);
				e.Graphics.FillRectangle(fillBrush, box);
				e.Graphics.DrawRectangle(borderPen, box);

				SmoothingMode previousMode = e.Graphics.SmoothingMode;
				e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

				using var checkPen = new Pen(this.palette.SelectionText, 1.8F);
				Point p1 = new Point(box.Left + 4, box.Top + (box.Height / 2));
				Point p2 = new Point(box.Left + (box.Width / 2) - 1, box.Bottom - 5);
				Point p3 = new Point(box.Right - 4, box.Top + 5);
				e.Graphics.DrawLines(checkPen, new[] { p1, p2, p3 });

				e.Graphics.SmoothingMode = previousMode;
			}

			/// <inheritdoc/>
			protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
			{
				if (!this.palette.IsDark)
				{
					base.OnRenderToolStripBorder(e);

					return;
				}

				if (e.ToolStrip is StatusStrip)
				{
					using var topBorderPen = new Pen(this.palette.Border);

					e.Graphics.DrawLine(topBorderPen, 0, 0, e.ToolStrip.Width, 0);

					return;
				}

				if (e.ToolStrip is ToolStripDropDown)
				{
					using var borderPen = new Pen(this.palette.Border);

					e.Graphics.DrawRectangle(borderPen, 0, 0, Math.Max(0, e.ToolStrip.Width - 1), Math.Max(0, e.ToolStrip.Height - 1));

					return;
				}

				base.OnRenderToolStripBorder(e);
			}

			/// <inheritdoc/>
			protected override void OnRenderStatusStripSizingGrip(ToolStripRenderEventArgs e)
			{
				if (!this.palette.IsDark)
				{
					base.OnRenderStatusStripSizingGrip(e);

					return;
				}

				using var dotBrush = new SolidBrush(this.palette.Border);

				int right  = e.ToolStrip.Width - 4;
				int bottom = e.ToolStrip.Height - 4;

				for (int row = 0; row < 3; row++)
				{
					for (int col = 0; col <= row; col++)
					{
						e.Graphics.FillRectangle(dotBrush, right - (col * 4), bottom - ((2 - row) * 4), 2, 2);
					}
				}
			}
		}

		private sealed class ThemeColorTable : ProfessionalColorTable
		{
			private readonly ThemePalette palette;

			internal ThemeColorTable(ThemePalette palette)
			{
				this.palette = palette;
			}

			public override Color MenuItemSelected => this.palette.Selection;
			public override Color MenuItemSelectedGradientBegin => this.palette.Selection;
			public override Color MenuItemSelectedGradientEnd => this.palette.Selection;
			public override Color MenuItemPressedGradientBegin => this.palette.Selection;
			public override Color MenuItemPressedGradientMiddle => this.palette.Selection;
			public override Color MenuItemPressedGradientEnd => this.palette.Selection;
			public override Color MenuItemBorder => this.palette.Border;
			public override Color MenuBorder => this.palette.Border;
			public override Color ToolStripBorder => this.palette.Border;
			public override Color ToolStripDropDownBackground => this.palette.SurfaceAlt;
			public override Color ImageMarginGradientBegin => this.palette.SurfaceAlt;
			public override Color ImageMarginGradientMiddle => this.palette.SurfaceAlt;
			public override Color ImageMarginGradientEnd => this.palette.SurfaceAlt;
			public override Color SeparatorDark => this.palette.Border;
			public override Color SeparatorLight => this.palette.SurfaceAlt;
			public override Color CheckBackground => this.palette.Button;
			public override Color CheckSelectedBackground => this.palette.AccentDark;
			public override Color CheckPressedBackground => this.palette.AccentDark;
			public override Color StatusStripBorder => this.palette.Border;
			public override Color StatusStripGradientBegin => this.palette.SurfaceAlt;
			public override Color StatusStripGradientEnd => this.palette.SurfaceAlt;
			public override Color GripDark => this.palette.Border;
			public override Color GripLight => this.palette.SurfaceAlt;
		}
	}
}