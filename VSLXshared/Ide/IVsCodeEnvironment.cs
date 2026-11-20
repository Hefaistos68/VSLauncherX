namespace VSLauncher.Ide
{
	/// <summary>
	/// Represents an installed application entry read from the Windows Uninstall registry hive.
	/// </summary>
	/// <param name="DisplayName">The registered display name of the application.</param>
	/// <param name="InstallLocation">The registered installation directory path.</param>
	public sealed record VsCodeUninstallEntry(string DisplayName, string InstallLocation);

	/// <summary>
	/// Abstracts host filesystem, environment, version, and registry lookups required for VS Code discovery and launch preparation.
	/// </summary>
	public interface IVsCodeEnvironment
	{
		/// <summary>
		/// Gets the path to the specified Windows special folder.
		/// </summary>
		/// <param name="folder">The special folder identifier.</param>
		/// <returns>The resolved directory path.</returns>
		string GetFolderPath(Environment.SpecialFolder folder);

		/// <summary>
		/// Gets the value of an environment variable.
		/// </summary>
		/// <param name="variableName">The environment variable name.</param>
		/// <returns>The variable value, or <c>null</c> if unset.</returns>
		string? GetEnvironmentVariable(string variableName);

		/// <summary>
		/// Gets the root temporary directory path for transient copy-on-lock operations.
		/// </summary>
		/// <returns>The temporary directory path.</returns>
		string GetTempPath();

		/// <summary>
		/// Determines whether the specified file exists.
		/// </summary>
		/// <param name="path">The file path to test.</param>
		/// <returns><c>true</c> if the file exists; otherwise <c>false</c>.</returns>
		bool FileExists(string? path);

		/// <summary>
		/// Determines whether the specified directory exists.
		/// </summary>
		/// <param name="path">The directory path to test.</param>
		/// <returns><c>true</c> if the directory exists; otherwise <c>false</c>.</returns>
		bool DirectoryExists(string? path);

		/// <summary>
		/// Reads the entire contents of a UTF-8 text file.
		/// </summary>
		/// <param name="path">The file path to read.</param>
		/// <returns>The file contents.</returns>
		string ReadAllText(string path);

		/// <summary>
		/// Reads the product version string from an executable file.
		/// </summary>
		/// <param name="path">The executable file path.</param>
		/// <returns>The product version string, or <c>null</c> if unavailable.</returns>
		string? GetFileVersion(string path);

		/// <summary>
		/// Enumerates Windows Uninstall registry entries containing a display name and installation location.
		/// </summary>
		/// <returns>A read-only list of uninstall entries.</returns>
		IReadOnlyList<VsCodeUninstallEntry> GetUninstallEntries();

		/// <summary>
		/// Enumerates immediate subdirectory paths within the specified directory.
		/// </summary>
		/// <param name="path">The parent directory path.</param>
		/// <returns>A read-only list of subdirectory paths, or an empty list when unavailable.</returns>
		IReadOnlyList<string> GetDirectories(string? path);

		/// <summary>
		/// Enumerates file paths matching a search pattern within the specified directory.
		/// </summary>
		/// <param name="path">The directory path to search.</param>
		/// <param name="searchPattern">The search pattern (for example, <c>*.exe</c>).</param>
		/// <returns>A read-only list of matching file paths, or an empty list when unavailable.</returns>
		IReadOnlyList<string> GetFiles(string? path, string searchPattern);
	}
}
