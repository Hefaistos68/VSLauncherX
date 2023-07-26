using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VSLauncher.DataModel
{
	/// <summary>
	/// The visual studio MRU structure as used in %LOCALAPPDATA%\Local\Microsoft\VisualStudio\*\ApplicationPrivateSettings.xml
	/// </summary>
	internal class VisualStudioMru
	{
		public List<MruEntry> MruList { get; set; }
	}

	/// <summary>
	/// The mru entry.
	/// </summary>
	public class MruEntry
	{
		public string Key { get; set; }
		public MruData Value { get; set; }
	}

	/// <summary>
	/// The mru data.
	/// </summary>
	public class MruData
	{
		public Localproperties LocalProperties { get; set; }
		public RemoteInfo? Remote { get; set; }
		public bool IsFavorite { get; set; }
		public DateTime LastAccessed { get; set; }
		public bool IsLocal { get; set; }
		public bool HasRemote { get; set; }
		public bool IsSourceControlled { get; set; }
	}

	/// <summary>
	/// The localproperties.
	/// </summary>
	public class Localproperties
	{
		public string FullPath { get; set; }
		public int Type { get; set; }
		public object? SourceControl { get; set; }
	}

	/// <summary>
	/// The remote info.
	/// </summary>
	public class RemoteInfo
	{
		public string Name { get; set; }
		public string? CodeContainerProvider { get; set; }
		public string? DisplayUrl { get; set; }
		public string? BrowseOnlineUrl { get; set; }
		public DateTime LastAccessed { get; set; }
		public object[]? ExtraProperties { get; set; }
	}

}
