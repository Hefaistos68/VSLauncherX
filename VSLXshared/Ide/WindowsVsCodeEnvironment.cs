using System.Diagnostics;
using System.Security;

using Microsoft.Win32;

namespace VSLauncher.Ide
{
	/// <summary>
	/// Default Windows implementation of <see cref="IVsCodeEnvironment"/> backed by the local filesystem, environment variables, and registry.
	/// </summary>
	public class WindowsVsCodeEnvironment : IVsCodeEnvironment
	{
		private const string UninstallSubKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";

		/// <summary>
		/// Gets the shared singleton instance of <see cref="WindowsVsCodeEnvironment"/>.
		/// </summary>
		public static WindowsVsCodeEnvironment Instance { get; } = new WindowsVsCodeEnvironment();

		/// <inheritdoc/>
		public string GetFolderPath(Environment.SpecialFolder folder)
		{
			return Environment.GetFolderPath(folder);
		}

		/// <inheritdoc/>
		public string? GetEnvironmentVariable(string variableName)
		{
			return Environment.GetEnvironmentVariable(variableName);
		}

		/// <inheritdoc/>
		public string GetTempPath()
		{
			return Path.GetTempPath();
		}

		/// <inheritdoc/>
		public bool FileExists(string? path)
		{
			return !string.IsNullOrWhiteSpace(path) && File.Exists(path);
		}

		/// <inheritdoc/>
		public bool DirectoryExists(string? path)
		{
			return !string.IsNullOrWhiteSpace(path) && Directory.Exists(path);
		}

		/// <inheritdoc/>
		public string ReadAllText(string path)
		{
			return File.ReadAllText(path);
		}

		/// <inheritdoc/>
		public string? GetFileVersion(string path)
		{
			if (!FileExists(path))
			{
				return null;
			}

			try
			{
				string? productVersion = FileVersionInfo.GetVersionInfo(path).ProductVersion;

				return string.IsNullOrWhiteSpace(productVersion) ? null : productVersion.Trim();
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or FileNotFoundException)
			{
				return null;
			}
		}

		/// <inheritdoc/>
		public IReadOnlyList<VsCodeUninstallEntry> GetUninstallEntries()
		{
			var entries = new List<VsCodeUninstallEntry>();

			ReadUninstallEntriesFromHive(RegistryHive.CurrentUser, RegistryView.Default, entries);
			ReadUninstallEntriesFromHive(RegistryHive.LocalMachine, RegistryView.Registry64, entries);
			ReadUninstallEntriesFromHive(RegistryHive.LocalMachine, RegistryView.Registry32, entries);

			return entries;
		}

		/// <inheritdoc/>
		public IReadOnlyList<string> GetDirectories(string? path)
		{
			if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
			{
				return Array.Empty<string>();
			}

			try
			{
				return Directory.GetDirectories(path);
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException or SecurityException)
			{
				Debug.WriteLine(ex);

				return Array.Empty<string>();
			}
		}

		/// <inheritdoc/>
		public IReadOnlyList<string> GetFiles(string? path, string searchPattern)
		{
			if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(searchPattern) || !Directory.Exists(path))
			{
				return Array.Empty<string>();
			}

			try
			{
				return Directory.GetFiles(path, searchPattern);
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException or SecurityException)
			{
				Debug.WriteLine(ex);

				return Array.Empty<string>();
			}
		}

		/// <summary>
		/// Reads Uninstall registry entries from a specific hive and registry view.
		/// </summary>
		/// <param name="hive">The registry hive to open.</param>
		/// <param name="view">The 32-bit or 64-bit registry view.</param>
		/// <param name="entries">The destination list receiving discovered entries.</param>
		private static void ReadUninstallEntriesFromHive(RegistryHive hive, RegistryView view, List<VsCodeUninstallEntry> entries)
		{
			try
			{
				using RegistryKey  baseKey      = RegistryKey.OpenBaseKey(hive, view);
				using RegistryKey? uninstallKey = baseKey.OpenSubKey(UninstallSubKey);

				if (uninstallKey is null)
				{
					return;
				}

				foreach (string subKeyName in uninstallKey.GetSubKeyNames())
				{
					TryAddUninstallSubKey(uninstallKey, subKeyName, entries);
				}
			}
			catch (Exception ex) when (ex is SecurityException or UnauthorizedAccessException or IOException)
			{
				Debug.WriteLine(ex);
			}
		}

		/// <summary>
		/// Attempts to read a single Uninstall subkey and append its entry when both display name and install location are populated.
		/// </summary>
		/// <param name="uninstallKey">The parent Uninstall registry key.</param>
		/// <param name="subKeyName">The subkey name to open.</param>
		/// <param name="entries">The destination list receiving the entry.</param>
		private static void TryAddUninstallSubKey(RegistryKey uninstallKey, string subKeyName, List<VsCodeUninstallEntry> entries)
		{
			try
			{
				using RegistryKey? subKey = uninstallKey.OpenSubKey(subKeyName);

				if (subKey is null)
				{
					return;
				}

				string? displayName     = subKey.GetValue("DisplayName") as string;
				string? installLocation = subKey.GetValue("InstallLocation") as string;

				if (string.IsNullOrWhiteSpace(displayName) || string.IsNullOrWhiteSpace(installLocation))
				{
					return;
				}

				string trimmedLocation = installLocation.Trim().Trim('"');

				if (!string.IsNullOrWhiteSpace(trimmedLocation))
				{
					entries.Add(new VsCodeUninstallEntry(displayName.Trim(), trimmedLocation));
				}
			}
			catch (Exception ex) when (ex is SecurityException or UnauthorizedAccessException or IOException)
			{
				Debug.WriteLine(ex);
			}
		}
	}
}
