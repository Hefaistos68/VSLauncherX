namespace VSLauncher.Ide
{
	/// <summary>
	/// Represents an installed VS Code-based IDE that has a newer release available to install.
	/// </summary>
	public sealed record VsCodeUpdateCandidate
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="VsCodeUpdateCandidate"/> record.
		/// </summary>
		/// <param name="instance">The installed IDE instance.</param>
		/// <param name="updateInfo">The update metadata describing the available release.</param>
		public VsCodeUpdateCandidate(VsCodeInstance instance, VsCodeUpdateInfo updateInfo)
		{
			ArgumentNullException.ThrowIfNull(instance);
			ArgumentNullException.ThrowIfNull(updateInfo);

			this.Instance   = instance;
			this.UpdateInfo = updateInfo;
		}

		/// <summary>
		/// Gets the installed IDE instance.
		/// </summary>
		public VsCodeInstance Instance { get; }

		/// <summary>
		/// Gets the update metadata describing the available release.
		/// </summary>
		public VsCodeUpdateInfo UpdateInfo { get; }
	}

	/// <summary>
	/// Aggregates update statuses across all installed VS Code-based IDEs into UI-ready button state, candidate lists, and tooltips.
	/// </summary>
	public sealed class VsCodeUpdateSummary
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="VsCodeUpdateSummary"/> class.
		/// </summary>
		private VsCodeUpdateSummary(
			IReadOnlyList<VsCodeUpdateCandidate> availableUpdates,
			string buttonText,
			bool buttonEnabled,
			bool showDownloadIcon,
			string tooltipText)
		{
			this.AvailableUpdates = availableUpdates;
			this.ButtonText       = buttonText;
			this.ButtonEnabled    = buttonEnabled;
			this.ShowDownloadIcon = showDownloadIcon;
			this.TooltipText      = tooltipText;
		}

		/// <summary>
		/// Gets the ordered list of installed IDEs that currently have an available update.
		/// </summary>
		public IReadOnlyList<VsCodeUpdateCandidate> AvailableUpdates { get; }

		/// <summary>
		/// Gets the label text to display on the sidebar installer/update button.
		/// </summary>
		public string ButtonText { get; }

		/// <summary>
		/// Gets a value indicating whether the sidebar installer/update button should be enabled.
		/// </summary>
		public bool ButtonEnabled { get; }

		/// <summary>
		/// Gets a value indicating whether the sidebar button should display the download icon instead of the installer icon.
		/// </summary>
		public bool ShowDownloadIcon { get; }

		/// <summary>
		/// Gets the tooltip summary text for the sidebar installer/update button.
		/// </summary>
		public string TooltipText { get; }

		/// <summary>
		/// Builds an aggregated <see cref="VsCodeUpdateSummary"/> from the installed IDE instances, update results, and installation state.
		/// </summary>
		/// <param name="instances">All currently discovered VS Code-based IDE instances.</param>
		/// <param name="updateStates">The map of update check results keyed by <see cref="VsCodeInstance.Identifier"/>.</param>
		/// <param name="selectedInstance">The currently selected sidebar IDE instance, if any.</param>
		/// <param name="installInProgress"><c>true</c> while an update download or installation batch is actively running.</param>
		/// <param name="activeInstallLabel">Optional active progress label displayed while <paramref name="installInProgress"/> is <c>true</c>.</param>
		/// <returns>An immutable <see cref="VsCodeUpdateSummary"/> describing the resulting UI state.</returns>
		public static VsCodeUpdateSummary Build(
			IReadOnlyList<VsCodeInstance> instances,
			IReadOnlyDictionary<string, VsCodeUpdateInfo> updateStates,
			VsCodeInstance? selectedInstance,
			bool installInProgress,
			string? activeInstallLabel = null)
		{
			ArgumentNullException.ThrowIfNull(instances);
			ArgumentNullException.ThrowIfNull(updateStates);

			if (instances.Count == 0)
			{
				return new VsCodeUpdateSummary(
					Array.Empty<VsCodeUpdateCandidate>(),
					buttonText: "Download VS Code",
					buttonEnabled: true,
					showDownloadIcon: true,
					tooltipText: "Opens the VS Code download page");
			}

			IReadOnlyList<VsCodeUpdateCandidate> availableUpdates = CollectAvailableUpdates(instances, updateStates);

			if (installInProgress)
			{
				return BuildInProgressSummary(instances, availableUpdates, activeInstallLabel);
			}

			if (availableUpdates.Count == 1)
			{
				VsCodeUpdateCandidate single = availableUpdates[0];
				string displayLabel          = FormatInstanceLabel(single.Instance, instances);

				return new VsCodeUpdateSummary(
					availableUpdates,
					buttonText: $"Update {single.Instance.ShortName}",
					buttonEnabled: true,
					showDownloadIcon: false,
					tooltipText: BuildSingleUpdateTooltip(displayLabel, single.UpdateInfo));
			}

			if (availableUpdates.Count > 1)
			{
				return new VsCodeUpdateSummary(
					availableUpdates,
					buttonText: $"Update IDEs ({availableUpdates.Count})",
					buttonEnabled: true,
					showDownloadIcon: false,
					tooltipText: BuildMultiUpdateTooltip(availableUpdates, instances));
			}

			return new VsCodeUpdateSummary(
				availableUpdates,
				buttonText: instances.Count == 1 ? "VS Code Installer" : "IDE Updates",
				buttonEnabled: false,
				showDownloadIcon: false,
				tooltipText: BuildNoUpdateTooltip(instances, updateStates, selectedInstance));
		}

		/// <summary>
		/// Collects ordered <see cref="VsCodeUpdateCandidate"/> entries for all installed IDEs with an available update.
		/// </summary>
		private static IReadOnlyList<VsCodeUpdateCandidate> CollectAvailableUpdates(
			IReadOnlyList<VsCodeInstance> instances,
			IReadOnlyDictionary<string, VsCodeUpdateInfo> updateStates)
		{
			var candidates = new List<VsCodeUpdateCandidate>();

			foreach (VsCodeInstance instance in instances)
			{
				if (instance is null)
				{
					continue;
				}

				if (updateStates.TryGetValue(instance.Identifier, out VsCodeUpdateInfo? info) && info.IsUpdateAvailable)
				{
					candidates.Add(new VsCodeUpdateCandidate(instance, info));
				}
			}

			return candidates;
		}

		/// <summary>
		/// Builds the summary state displayed while an installer download or batch execution is active.
		/// </summary>
		private static VsCodeUpdateSummary BuildInProgressSummary(
			IReadOnlyList<VsCodeInstance> instances,
			IReadOnlyList<VsCodeUpdateCandidate> availableUpdates,
			string? activeInstallLabel)
		{
			string trimmedLabel = activeInstallLabel?.Trim() ?? string.Empty;
			string buttonText   = !string.IsNullOrEmpty(trimmedLabel)
				? trimmedLabel
				: (instances.Count == 1 ? $"Updating {instances[0].ShortName}..." : "Updating IDEs...");
			string tooltipText  = !string.IsNullOrEmpty(trimmedLabel)
				? trimmedLabel
				: "Downloading and installing IDE updates...";

			return new VsCodeUpdateSummary(
				availableUpdates,
				buttonText,
				buttonEnabled: false,
				showDownloadIcon: false,
				tooltipText);
		}

		/// <summary>
		/// Builds the tooltip text when exactly one IDE has an update available.
		/// </summary>
		private static string BuildSingleUpdateTooltip(string displayLabel, VsCodeUpdateInfo info)
		{
			bool hasCurrent = !string.IsNullOrWhiteSpace(info.CurrentVersion);
			bool hasLatest  = !string.IsNullOrWhiteSpace(info.LatestVersion);

			if (hasCurrent && hasLatest && !info.CurrentVersion.Equals(info.LatestVersion, StringComparison.OrdinalIgnoreCase))
			{
				return $"Update {displayLabel} from {info.CurrentVersion} to {info.LatestVersion}";
			}

			if (hasLatest)
			{
				return $"Update {displayLabel} to {info.LatestVersion}";
			}

			return $"Update {displayLabel}";
		}

		/// <summary>
		/// Builds a multi-line tooltip listing all IDEs with available updates and their version transitions.
		/// </summary>
		private static string BuildMultiUpdateTooltip(
			IReadOnlyList<VsCodeUpdateCandidate> availableUpdates,
			IReadOnlyList<VsCodeInstance> allInstances)
		{
			var lines = new List<string>(availableUpdates.Count + 1)
			{
				"Updates available:"
			};

			foreach (VsCodeUpdateCandidate candidate in availableUpdates)
			{
				string label      = FormatInstanceLabel(candidate.Instance, allInstances);
				string current    = candidate.UpdateInfo.CurrentVersion;
				string latest     = candidate.UpdateInfo.LatestVersion;
				bool hasCurrent   = !string.IsNullOrWhiteSpace(current);
				bool hasLatest    = !string.IsNullOrWhiteSpace(latest);

				if (hasCurrent && hasLatest && !current.Equals(latest, StringComparison.OrdinalIgnoreCase))
				{
					lines.Add($"- {label}: {current} -> {latest}");
				}
				else if (hasLatest)
				{
					lines.Add($"- {label}: {latest}");
				}
				else
				{
					lines.Add($"- {label}: Update available");
				}
			}

			return string.Join(Environment.NewLine, lines);
		}

		/// <summary>
		/// Builds the tooltip text when no installed IDEs have an available update.
		/// </summary>
		private static string BuildNoUpdateTooltip(
			IReadOnlyList<VsCodeInstance> instances,
			IReadOnlyDictionary<string, VsCodeUpdateInfo> updateStates,
			VsCodeInstance? selectedInstance)
		{
			if (instances.Count == 1)
			{
				VsCodeInstance single = selectedInstance ?? instances[0];

				_ = updateStates.TryGetValue(single.Identifier, out VsCodeUpdateInfo? info);

				return FormatSingleIdeNoUpdateTooltip(single, info);
			}

			var lines = new List<string>(instances.Count);

			foreach (VsCodeInstance instance in instances)
			{
				if (instance is null)
				{
					continue;
				}

				_ = updateStates.TryGetValue(instance.Identifier, out VsCodeUpdateInfo? info);

				lines.Add(FormatMultiIdeStatusLine(instance, instances, info));
			}

			return lines.Count > 0 ? string.Join(Environment.NewLine, lines) : "Checks daily for IDE updates";
		}

		/// <summary>
		/// Formats the tooltip for a single installed IDE when no update is available.
		/// </summary>
		private static string FormatSingleIdeNoUpdateTooltip(VsCodeInstance instance, VsCodeUpdateInfo? info)
		{
			string version   = ResolveVersionForDisplay(instance, info);
			string verSuffix = string.IsNullOrWhiteSpace(version) ? string.Empty : $" ({version})";

			if (IsUnsupported(instance, info))
			{
				return $"{instance.ShortName}{verSuffix}: Manual updates only";
			}

			if (info is null)
			{
				return "Checks daily for VS Code updates";
			}

			if (!info.CheckSucceeded)
			{
				return $"{instance.ShortName}{verSuffix}: Update check failed";
			}

			return $"{instance.ShortName}{verSuffix} is up to date";
		}

		/// <summary>
		/// Formats a single line summarizing an IDE's status in a multi-IDE installation.
		/// </summary>
		private static string FormatMultiIdeStatusLine(
			VsCodeInstance instance,
			IReadOnlyList<VsCodeInstance> allInstances,
			VsCodeUpdateInfo? info)
		{
			string label     = FormatInstanceLabel(instance, allInstances);
			string version   = ResolveVersionForDisplay(instance, info);
			string verSuffix = string.IsNullOrWhiteSpace(version) ? string.Empty : $" ({version})";

			if (IsUnsupported(instance, info))
			{
				return $"{label}{verSuffix}: Manual updates only";
			}

			if (info is null)
			{
				return $"{label}{verSuffix}: Checks daily for updates";
			}

			if (!info.CheckSucceeded)
			{
				return $"{label}{verSuffix}: Update check failed";
			}

			return $"{label}{verSuffix}: Up to date";
		}

		/// <summary>
		/// Determines whether an IDE is unsupported for automatic update checking.
		/// </summary>
		private static bool IsUnsupported(VsCodeInstance instance, VsCodeUpdateInfo? info)
		{
			return instance.UpdateStrategy == VsCodeUpdateStrategyKind.Unsupported
				|| info?.UpdateCheckStatus == VsCodeUpdateCheckStatus.Unsupported;
		}

		/// <summary>
		/// Chooses the best available display version string from check state or instance metadata.
		/// </summary>
		private static string ResolveVersionForDisplay(VsCodeInstance instance, VsCodeUpdateInfo? info)
		{
			if (info is not null && !string.IsNullOrWhiteSpace(info.CurrentVersion))
			{
				return info.CurrentVersion;
			}

			return instance.Version;
		}

		/// <summary>
		/// Returns a disambiguated display label when multiple installed instances share the same <see cref="VsCodeInstance.ShortName"/>.
		/// </summary>
		private static string FormatInstanceLabel(VsCodeInstance instance, IReadOnlyList<VsCodeInstance> allInstances)
		{
			int duplicateCount = 0;

			foreach (VsCodeInstance candidate in allInstances)
			{
				if (candidate is not null && candidate.ShortName.Equals(instance.ShortName, StringComparison.OrdinalIgnoreCase))
				{
					duplicateCount++;
				}
			}

			return duplicateCount > 1 && !string.IsNullOrWhiteSpace(instance.Name)
				? instance.Name
				: instance.ShortName;
		}
	}
}
