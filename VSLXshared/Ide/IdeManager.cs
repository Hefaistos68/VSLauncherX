using VSLauncher.DataModel;

namespace VSLauncher.Ide
{
	/// <summary>
	/// Coordinates registered <see cref="IIdeProvider"/> implementations, instance resolution precedence, and single-IDE fallback.
	/// </summary>
	public class IdeManager
	{
		private readonly Dictionary<IdeType, IIdeProvider> providers;

		/// <summary>
		/// Initializes a new instance of the <see cref="IdeManager"/> class with the default IDE providers.
		/// </summary>
		public IdeManager() : this(new IIdeProvider[] { new VisualStudioIdeProvider(), new VsCodeIdeProvider() })
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="IdeManager"/> class with the supplied IDE providers.
		/// </summary>
		/// <param name="providers">The providers to register.</param>
		public IdeManager(IEnumerable<IIdeProvider> providers)
		{
			ArgumentNullException.ThrowIfNull(providers);

			this.providers = new Dictionary<IdeType, IIdeProvider>();

			foreach (IIdeProvider provider in providers)
			{
				if (provider is not null)
				{
					this.providers[provider.IdeType] = provider;
				}
			}
		}

		/// <summary>
		/// Gets a value indicating whether any registered IDE provider has an installed instance.
		/// </summary>
		public bool AnyInstalled
		{
			get
			{
				return this.providers.Values.Any(p => p.IsInstalled);
			}
		}

		/// <summary>
		/// Gets the registered provider for the specified IDE type, if available.
		/// </summary>
		/// <param name="ideType">The target IDE family.</param>
		/// <returns>The matching <see cref="IIdeProvider"/>, or <c>null</c> if none is registered.</returns>
		public IIdeProvider? GetProvider(IdeType ideType)
		{
			return this.providers.TryGetValue(ideType, out IIdeProvider? provider) ? provider : null;
		}

		/// <summary>
		/// Gets all discovered instances for the specified IDE type.
		/// </summary>
		/// <param name="ideType">The target IDE family.</param>
		/// <returns>A read-only list of discovered instances, or an empty list if unavailable.</returns>
		public IReadOnlyList<IIdeInstance> GetInstances(IdeType ideType)
		{
			return GetProvider(ideType)?.Instances ?? Array.Empty<IIdeInstance>();
		}

		/// <summary>
		/// Determines whether at least one installation exists for the specified IDE type.
		/// </summary>
		/// <param name="ideType">The IDE family to check.</param>
		/// <returns><c>true</c> if installed; otherwise <c>false</c>.</returns>
		public bool HasInstalled(IdeType ideType)
		{
			return GetProvider(ideType)?.IsInstalled == true;
		}

		/// <summary>
		/// Aggregates recent project groups across all registered IDE providers.
		/// </summary>
		/// <param name="onlyDefaultProfiles">When <c>true</c>, restricts results to default profiles or instances where supported.</param>
		/// <returns>A combined <see cref="VsItemList"/> of grouped recent projects.</returns>
		public VsItemList GetAllRecentProjects(bool onlyDefaultProfiles)
		{
			var combined = new VsItemList(null);

			foreach (IIdeProvider provider in this.providers.Values)
			{
				VsItemList recentItems = provider.GetRecentProjects(onlyDefaultProfiles);

				if (recentItems is not null && recentItems.Count > 0)
				{
					combined.AddRange(recentItems);
				}
			}

			return combined;
		}

		/// <summary>
		/// Resolves the target <see cref="IIdeInstance"/> for an item following the multi-IDE precedence and single-IDE fallback rules.
		/// </summary>
		/// <param name="item">The item to resolve.</param>
		/// <param name="defaultTargetIdentifiers">Optional per-IDE default instance identifiers from sidebar selection.</param>
		/// <returns>The resolved <see cref="IIdeInstance"/>, or <c>null</c> if no compatible IDE is installed.</returns>
		public IIdeInstance? ResolveInstance(VsItem item, IReadOnlyDictionary<IdeType, string>? defaultTargetIdentifiers = null)
		{
			ArgumentNullException.ThrowIfNull(item);

			(IdeType targetIde, string? explicitIdentifier) = DetermineTargetIde(item);

			IIdeProvider? provider = GetProvider(targetIde);

			if (provider is not null && provider.IsInstalled)
			{
				IIdeInstance? resolved = ResolveFromProvider(provider, item, targetIde, explicitIdentifier, defaultTargetIdentifiers);

				if (resolved is not null)
				{
					return resolved;
				}
			}

			if (item.IdeType == IdeType.VisualStudio && !HasInstalled(IdeType.VisualStudio) && HasInstalled(IdeType.VsCode))
			{
				IIdeProvider? vsCodeProvider = GetProvider(IdeType.VsCode);

				if (vsCodeProvider is not null && vsCodeProvider.IsInstalled)
				{
					return ResolveDefaultInstance(vsCodeProvider, IdeType.VsCode, defaultTargetIdentifiers);
				}
			}

			return null;
		}

		/// <summary>
		/// Determines the target IDE type and explicit instance identifier for the supplied item.
		/// </summary>
		/// <param name="item">The item being resolved.</param>
		/// <returns>The target IDE type and optional explicit instance identifier.</returns>
		private (IdeType TargetIde, string? ExplicitIdentifier) DetermineTargetIde(VsItem item)
		{
			if (item is VsFolder folder && folder.IsWorkspaceBacked() && HasInstalled(IdeType.VsCode))
			{
				return (IdeType.VsCode, folder.VsCodeOptions?.Edition);
			}

			return (item.IdeType, item.VsVersion);
		}

		/// <summary>
		/// Resolves an instance within a single provider using explicit ID, solution required version, sidebar default, and provider default.
		/// </summary>
		private static IIdeInstance? ResolveFromProvider(
			IIdeProvider provider,
			VsItem item,
			IdeType targetIde,
			string? explicitIdentifier,
			IReadOnlyDictionary<IdeType, string>? defaultTargetIdentifiers)
		{
			if (!string.IsNullOrWhiteSpace(explicitIdentifier))
			{
				IIdeInstance? byId = provider.FindByIdentifier(explicitIdentifier);

				if (byId is not null)
				{
					return byId;
				}
			}

			if (targetIde == IdeType.VisualStudio && item is VsSolution solution && !string.IsNullOrWhiteSpace(solution.RequiredVersion))
			{
				IIdeInstance? byVersion = provider.FindByVersion(solution.RequiredVersion);

				if (byVersion is not null)
				{
					return byVersion;
				}
			}

			return ResolveDefaultInstance(provider, targetIde, defaultTargetIdentifiers);
		}

		/// <summary>
		/// Resolves the fallback instance from sidebar defaults or the provider's built-in default.
		/// </summary>
		private static IIdeInstance? ResolveDefaultInstance(
			IIdeProvider provider,
			IdeType ideType,
			IReadOnlyDictionary<IdeType, string>? defaultTargetIdentifiers)
		{
			if (defaultTargetIdentifiers is not null &&
				defaultTargetIdentifiers.TryGetValue(ideType, out string? defaultId) &&
				!string.IsNullOrWhiteSpace(defaultId))
			{
				IIdeInstance? byDefaultId = provider.FindByIdentifier(defaultId);

				if (byDefaultId is not null)
				{
					return byDefaultId;
				}
			}

			return provider.DefaultInstance;
		}
	}
}
