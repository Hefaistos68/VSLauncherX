using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using VSLauncher.DataModel;

namespace VSLXshared.Helpers
{
	/// <summary>
	/// File dialog filters and safe JSON serialization helpers.
	/// </summary>
	public static class FileHelper
	{
		private static readonly ISerializationBinder SafeSerializationBinder = new VsSerializationBinder();

		public static readonly string ExecutablesFilterString = "Executable files (*.exe)|*.exe|" +
																"Batch files (*.bat)|*.bat|" +
																"Command files (*.cmd)|*.cmd|" +
																"PowerShell files (*.ps1)|*.ps1|" +
																"All files (*.*)|*.*";

		public static readonly string SolutionFilterString    = "Solutions (*.sln, *.slnx)|*.sln;*.slnx|" +
																"C# Projects (*.csproj)|*.csproj|" +
																"VB Projects (*.vbproj)|*.vbproj|" +
																"F# Projects (*.fsproj)|*.fsproj|" +
																"TS/JS Projects (*.esproj, *.tsproj)|*.esproj;*.tsproj|" +
																"Cxx Projects (*.vcxproj)|*.vcxproj|" +
																"All files (*.*)|*.*";

		/// <summary>
		/// Creates JSON serializer settings using polymorphic type handling restricted to known data model types.
		/// </summary>
		/// <param name="indented">Whether output JSON should be indented.</param>
		/// <returns>Configured <see cref="JsonSerializerSettings"/>.</returns>
		public static JsonSerializerSettings CreateSafeJsonSettings(bool indented = false)
		{
			return new JsonSerializerSettings
			{
				Formatting          = indented ? Formatting.Indented : Formatting.None,
				TypeNameHandling    = TypeNameHandling.Auto,
				SerializationBinder = SafeSerializationBinder
			};
		}

		/// <summary>
		/// Restricts Newtonsoft.Json type resolution to known VSLauncher data model types.
		/// </summary>
		private sealed class VsSerializationBinder : ISerializationBinder
		{
			private static readonly HashSet<string> AllowedAssemblies = new(StringComparer.OrdinalIgnoreCase)
			{
				"VSLXshared",
				"VSLauncherX"
			};

			private static readonly Dictionary<string, Type> AllowedTypes = new(StringComparer.Ordinal)
			{
				[typeof(LaunchInfo).FullName!]       = typeof(LaunchInfo),
				[typeof(VsFolder).FullName!]         = typeof(VsFolder),
				[typeof(VsSolution).FullName!]       = typeof(VsSolution),
				[typeof(VsProject).FullName!]        = typeof(VsProject),
				[typeof(VsItem).FullName!]           = typeof(VsItem),
				[typeof(VsOptions).FullName!]        = typeof(VsOptions),
				[typeof(VsItemList).FullName!]       = typeof(VsItemList),
				[typeof(ItemTypeEnum).FullName!]     = typeof(ItemTypeEnum),
				[typeof(ProjectTypeEnum).FullName!]  = typeof(ProjectTypeEnum),
				[typeof(SolutionTypeEnum).FullName!] = typeof(SolutionTypeEnum),
				[typeof(OptionsEnum).FullName!]      = typeof(OptionsEnum)
			};

			/// <inheritdoc/>
			public Type BindToType(string? assemblyName, string typeName)
			{
				if (!string.IsNullOrEmpty(assemblyName))
				{
					string simpleAssemblyName = assemblyName.Split(',')[0].Trim();

					if (!AllowedAssemblies.Contains(simpleAssemblyName))
					{
						throw new JsonSerializationException($"Disallowed assembly '{assemblyName}' for type '{typeName}'.");
					}
				}

				if (AllowedTypes.TryGetValue(typeName, out Type? resolvedType))
				{
					return resolvedType;
				}

				throw new JsonSerializationException($"Disallowed type '{typeName}'.");
			}

			/// <inheritdoc/>
			public void BindToName(Type serializedType, out string? assemblyName, out string? typeName)
			{
				assemblyName = serializedType.Assembly.GetName().Name;
				typeName     = serializedType.FullName;
			}
		}
	}
}
