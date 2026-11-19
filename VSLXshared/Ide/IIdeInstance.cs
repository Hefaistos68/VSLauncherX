using System.Drawing;

using VSLauncher.DataModel;

namespace VSLauncher.Ide
{
	/// <summary>
	/// Represents an installed IDE edition or instance.
	/// </summary>
	public interface IIdeInstance
	{
		/// <summary>
		/// Gets the IDE family represented by this instance.
		/// </summary>
		IdeType IdeType { get; }

		/// <summary>
		/// Gets the full display name of this IDE instance.
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Gets the compact display name suitable for quick-launch buttons and badges.
		/// </summary>
		string ShortName { get; }

		/// <summary>
		/// Gets the product version string.
		/// </summary>
		string Version { get; }

		/// <summary>
		/// Gets the full path to the IDE executable.
		/// </summary>
		string Location { get; }

		/// <summary>
		/// Gets the stable unique identifier for this IDE instance.
		/// </summary>
		string Identifier { get; }

		/// <summary>
		/// Gets the icon associated with this IDE executable.
		/// </summary>
		Icon AppIcon { get; }

		/// <summary>
		/// Gets the available launch profiles or instance suffixes, starting with <c>&lt;default&gt;</c>.
		/// </summary>
		/// <returns>A list of profile or instance names.</returns>
		List<string> GetProfiles();
	}
}
