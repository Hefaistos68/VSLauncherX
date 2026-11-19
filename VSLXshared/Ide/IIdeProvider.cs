using System.Diagnostics;

using VSLauncher.DataModel;

namespace VSLauncher.Ide
{
	/// <summary>
	/// Defines discovery, recent project loading, and launch preparation for an IDE family.
	/// </summary>
	public interface IIdeProvider
	{
		/// <summary>
		/// Gets the IDE family managed by this provider.
		/// </summary>
		IdeType IdeType { get; }

		/// <summary>
		/// Gets the human-readable IDE family name.
		/// </summary>
		string DisplayName { get; }

		/// <summary>
		/// Gets the official download URL for installing this IDE.
		/// </summary>
		string DownloadUrl { get; }

		/// <summary>
		/// Gets all discovered installations for this IDE.
		/// </summary>
		IReadOnlyList<IIdeInstance> Instances { get; }

		/// <summary>
		/// Gets a value indicating whether at least one installation of this IDE is available.
		/// </summary>
		bool IsInstalled { get; }

		/// <summary>
		/// Gets the default installation used when no explicit instance is specified, or <c>null</c> if none are installed.
		/// </summary>
		IIdeInstance? DefaultInstance { get; }

		/// <summary>
		/// Finds an installed instance by its stable identifier.
		/// </summary>
		/// <param name="identifier">The instance identifier.</param>
		/// <returns>The matching <see cref="IIdeInstance"/>, or <c>null</c> if not found.</returns>
		IIdeInstance? FindByIdentifier(string? identifier);

		/// <summary>
		/// Finds an installed instance matching a version string or prefix.
		/// </summary>
		/// <param name="version">The version string or prefix.</param>
		/// <returns>The matching <see cref="IIdeInstance"/>, or <c>null</c> if not found.</returns>
		IIdeInstance? FindByVersion(string? version);

		/// <summary>
		/// Loads recent projects grouped by instance or profile.
		/// </summary>
		/// <param name="onlyDefaultProfiles">When <c>true</c>, restricts results to default profiles or instances where applicable.</param>
		/// <returns>A <see cref="VsItemList"/> containing grouped recent items.</returns>
		VsItemList GetRecentProjects(bool onlyDefaultProfiles);

		/// <summary>
		/// Builds a <see cref="ProcessStartInfo"/> for launching an item with a resolved IDE instance.
		/// </summary>
		/// <param name="item">The item to launch.</param>
		/// <param name="instance">The target IDE instance.</param>
		/// <param name="forceAdmin">Whether to force UAC elevation.</param>
		/// <param name="waitForExit">Whether the launched process should block until closed.</param>
		/// <returns>A configured <see cref="ProcessStartInfo"/>, or <c>null</c> if the item is not directly launchable.</returns>
		ProcessStartInfo? BuildStartInfo(VsItem item, IIdeInstance instance, bool forceAdmin = false, bool waitForExit = false);
	}
}
