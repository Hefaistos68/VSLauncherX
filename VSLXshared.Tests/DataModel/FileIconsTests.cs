using System.Drawing;
using System.Drawing.Imaging;

using NUnit.Framework;

using Shouldly;

using VSLauncher.DataModel;

namespace VSLXshared.Tests.DataModel
{
	/// <summary>
	/// Unit tests for <see cref="FileIcons"/> and <see cref="RegisteredFileType"/>.
	/// </summary>
	[TestFixture]
	public class FileIconsTests
	{
		/// <summary>
		/// Verifies that VS Code workspace and project icon lookups return non-null icons.
		/// </summary>
		[Test]
		public void GetIcon_ForCodeWorkspaceAndVsCodeProject_ReturnsNonNullIcon()
		{
			var icons = new FileIcons(bLarge: true);

			icons.GetIcon("CodeWorkspace").ShouldNotBeNull();
			icons.GetIcon("VsCodeProject").ShouldNotBeNull();
		}

		/// <summary>
		/// Verifies that VS Code icon keys do not fall back to the warning icon.
		/// </summary>
		[Test]
		public void GetIcon_ForCodeWorkspaceAndVsCodeProject_DoesNotReturnWarningIcon()
		{
			var icons = new FileIcons(bLarge: true);

			byte[] warningBytes       = RenderIconPngBytes(icons.GetIcon("Warning"));
			byte[] codeWorkspaceBytes = RenderIconPngBytes(icons.GetIcon("CodeWorkspace"));
			byte[] vsCodeProjectBytes = RenderIconPngBytes(icons.GetIcon("VsCodeProject"));

			codeWorkspaceBytes.ShouldNotBe(warningBytes);
			vsCodeProjectBytes.ShouldNotBe(warningBytes);
		}

		/// <summary>
		/// Verifies that small icon initialization supports VS Code and <c>.slnx</c> solution keys.
		/// </summary>
		[Test]
		public void GetIcon_SmallIcons_ReturnsNonNullForVsCodeAndSolutionX()
		{
			var icons = new FileIcons(bLarge: false);

			icons.GetIcon("CodeWorkspace").ShouldNotBeNull();
			icons.GetIcon("VsCodeProject").ShouldNotBeNull();
			icons.GetIcon("SolutionX").ShouldNotBeNull();
		}

		/// <summary>
		/// Verifies that missing, empty, or null icon names safely fall back to the default icon without throwing.
		/// </summary>
		[Test]
		public void GetIcon_WhenKeyIsMissingOrNull_ReturnsDefaultIconWithoutThrowing()
		{
			var icons = new FileIcons(bLarge: true);

			byte[] defaultBytes = RenderIconPngBytes(icons.GetIcon("None"));

			RenderIconPngBytes(icons.GetIcon("NonExistentIconKey")).ShouldBe(defaultBytes);
			RenderIconPngBytes(icons.GetIcon(string.Empty)).ShouldBe(defaultBytes);
			RenderIconPngBytes(icons.GetIcon(null!)).ShouldBe(defaultBytes);
		}

		/// <summary>
		/// Verifies that <see cref="RegisteredFileType.listExtensionsOfInterest"/> includes <c>.code-workspace</c> and <c>.slnx</c>.
		/// </summary>
		[Test]
		public void ListExtensionsOfInterest_IncludesCodeWorkspaceAndSlnx()
		{
			RegisteredFileType.listExtensionsOfInterest.ShouldContain(".code-workspace");
			RegisteredFileType.listExtensionsOfInterest.ShouldContain(".slnx");
		}

		/// <summary>
		/// Renders an <see cref="Icon"/> to PNG bytes for reliable visual equality comparison.
		/// </summary>
		/// <param name="icon">The icon to render.</param>
		/// <returns>The encoded PNG byte array.</returns>
		private static byte[] RenderIconPngBytes(Icon icon)
		{
			using Bitmap bitmap = icon.ToBitmap();
			using var stream    = new MemoryStream();

			bitmap.Save(stream, ImageFormat.Png);

			return stream.ToArray();
		}
	}
}
