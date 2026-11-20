using System.Diagnostics;

using VSLauncher.DataModel;

namespace VSLauncher.Ide
{
	/// <summary>
	/// Provides Visual Studio discovery, MRU loading, and <c>devenv.exe</c> start-info construction.
	/// </summary>
	public class VisualStudioIdeProvider : IIdeProvider
	{
		private const string VisualStudioDownloadUrl = "https://visualstudio.microsoft.com/downloads/";

		private readonly IReadOnlyList<VisualStudioInstance> instances;
		private readonly Func<bool, VsItemList> recentProjectsLoader;

		/// <summary>
		/// Initializes a new instance of the <see cref="VisualStudioIdeProvider"/> class using local Visual Studio discovery.
		/// </summary>
		public VisualStudioIdeProvider() : this(new VisualStudioInstanceManager())
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="VisualStudioIdeProvider"/> class wrapping an existing <see cref="VisualStudioInstanceManager"/>.
		/// </summary>
		/// <param name="instanceManager">The Visual Studio instance manager.</param>
		public VisualStudioIdeProvider(VisualStudioInstanceManager instanceManager)
		{
			ArgumentNullException.ThrowIfNull(instanceManager);

			this.instances            = instanceManager.All.ToList();
			this.recentProjectsLoader = instanceManager.GetRecentProjects;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="VisualStudioIdeProvider"/> class with explicit instances and an optional MRU loader.
		/// </summary>
		/// <param name="instances">The Visual Studio instances exposed by this provider.</param>
		/// <param name="recentProjectsLoader">Optional MRU loader callback.</param>
		public VisualStudioIdeProvider(IEnumerable<VisualStudioInstance> instances, Func<bool, VsItemList>? recentProjectsLoader = null)
		{
			ArgumentNullException.ThrowIfNull(instances);

			this.instances            = instances.Where(i => i is not null).ToList();
			this.recentProjectsLoader = recentProjectsLoader ?? (_ => new VsItemList(null));
		}

		/// <inheritdoc/>
		public IdeType IdeType
		{
			get
			{
				return IdeType.VisualStudio;
			}
		}

		/// <inheritdoc/>
		public string DisplayName
		{
			get
			{
				return "Visual Studio";
			}
		}

		/// <inheritdoc/>
		public string DownloadUrl
		{
			get
			{
				return VisualStudioDownloadUrl;
			}
		}

		/// <inheritdoc/>
		public IReadOnlyList<IIdeInstance> Instances
		{
			get
			{
				return this.instances;
			}
		}

		/// <inheritdoc/>
		public bool IsInstalled
		{
			get
			{
				return this.instances.Count > 0;
			}
		}

		/// <inheritdoc/>
		public IIdeInstance? DefaultInstance
		{
			get
			{
				return SelectHighestVersionInstance();
			}
		}

		/// <inheritdoc/>
		public IIdeInstance? FindByIdentifier(string? identifier)
		{
			if (string.IsNullOrWhiteSpace(identifier))
			{
				return null;
			}

			return this.instances.FirstOrDefault(x => x.Identifier.Equals(identifier, StringComparison.OrdinalIgnoreCase));
		}

		/// <inheritdoc/>
		public IIdeInstance? FindByVersion(string? version)
		{
			if (string.IsNullOrWhiteSpace(version))
			{
				return null;
			}

			return this.instances.FirstOrDefault(x => x.Version.StartsWith(version, StringComparison.OrdinalIgnoreCase));
		}

		/// <inheritdoc/>
		public VsItemList GetRecentProjects(bool onlyDefaultProfiles)
		{
			return this.recentProjectsLoader(onlyDefaultProfiles);
		}

		/// <inheritdoc/>
		public ProcessStartInfo? BuildStartInfo(VsItem item, IIdeInstance instance, bool forceAdmin = false, bool waitForExit = false)
		{
			ArgumentNullException.ThrowIfNull(item);
			ArgumentNullException.ThrowIfNull(instance);

			if (item.ItemType != ItemTypeEnum.Solution && item.ItemType != ItemTypeEnum.Project)
			{
				return null;
			}

			if (string.IsNullOrWhiteSpace(item.Path) || string.IsNullOrWhiteSpace(instance.Location))
			{
				return null;
			}

			bool runAsAdmin = forceAdmin || item.RunAsAdmin;

			return new ProcessStartInfo
			{
				FileName        = instance.Location,
				Arguments       = BuildArguments(item),
				Verb            = runAsAdmin ? "runas" : string.Empty,
				UseShellExecute = runAsAdmin,
				ErrorDialog     = runAsAdmin
			};
		}

		/// <summary>
		/// Selects the Visual Studio instance with the highest version, preferring the later entry on ties.
		/// </summary>
		/// <returns>The highest-version instance, or <c>null</c> if none are registered.</returns>
		private VisualStudioInstance? SelectHighestVersionInstance()
		{
			if (this.instances.Count == 0)
			{
				return null;
			}

			VisualStudioInstance best = this.instances[0];

			for (int i = 1; i < this.instances.Count; i++)
			{
				VisualStudioInstance candidate = this.instances[i];

				if (CompareVersionStrings(candidate.Version, best.Version) >= 0)
				{
					best = candidate;
				}
			}

			return best;
		}

		/// <summary>
		/// Compares two version strings numerically when parseable, falling back to ordinal case-insensitive comparison.
		/// </summary>
		/// <param name="left">The first version string.</param>
		/// <param name="right">The second version string.</param>
		/// <returns>A signed integer indicating relative order.</returns>
		private static int CompareVersionStrings(string left, string right)
		{
			bool parsedLeft  = Version.TryParse(left, out Version? vLeft);
			bool parsedRight = Version.TryParse(right, out Version? vRight);

			if (parsedLeft && parsedRight && vLeft is not null && vRight is not null)
			{
				return vLeft.CompareTo(vRight);
			}

			return string.Compare(left, right, StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Builds the <c>devenv.exe</c> command-line argument string for a launchable Visual Studio item.
		/// </summary>
		/// <param name="item">The item to launch.</param>
		/// <returns>The formatted argument string.</returns>
		private static string BuildArguments(VsItem item)
		{
			var args = new List<string> { $"\"{item.Path}\"" };

			if (!string.IsNullOrWhiteSpace(item.Commands))
			{
				args.Add(item.Commands.Trim());
			}

			if (!item.ShowSplash)
			{
				args.Add("/NoSplash");
			}

			if (!string.IsNullOrWhiteSpace(item.Instance))
			{
				string trimmedInstance = item.Instance.Trim();

				if (!string.Equals(trimmedInstance, "<default>", StringComparison.OrdinalIgnoreCase))
				{
					args.Add(trimmedInstance.Contains(' ', StringComparison.Ordinal)
						? $"/RootSuffix \"{trimmedInstance}\""
						: $"/RootSuffix {trimmedInstance}");
				}
			}

			return string.Join(' ', args);
		}
	}
}
