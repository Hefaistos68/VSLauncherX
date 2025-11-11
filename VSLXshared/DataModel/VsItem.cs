using Newtonsoft.Json;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace VSLauncher.DataModel
{
	/// <summary>
	/// The item type, required for serialization mostly
	/// </summary>
	public enum ItemTypeEnum
	{
		Solution,
		Project,
		Other,
		Folder,
		VisualStudio
	}

	/// <summary>
	/// The vs item.
	/// </summary>
	public class VsItem : VsOptions, INotifyPropertyChanged
	{
		private string?        name;
		private string?        path;
		private string?        commands;
		private string?        instance;
		private string?        vsVersion;
		private ItemTypeEnum   itemType;
		private bool           isFavorite;
		private string?        status; // git status marker (*,!,?)
		private string         branchName = string.Empty;
		private bool           expanded;
		private VsItemList     items      = new VsItemList(null);
		private bool           checkedFlag;
		private bool           warning;

		/// <summary>
		/// Raised when a property value changes.
		/// </summary>
		public event PropertyChangedEventHandler? PropertyChanged;

		/// <summary>
		/// Helper to raise PropertyChanged.
		/// Passing string.Empty notifies bindings with empty path (entire object).
		/// </summary>
		/// <param name="propertyName">Name of property.</param>
		protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="VsItem"/> class.
		/// </summary>
		public VsItem() : base()
		{
			this.ItemType   = ItemTypeEnum.Other;
			this.branchName = string.Empty;
		}

		/// <summary>
		/// Clones the object
		/// </summary>
		/// <returns>A VsItem.</returns>
		public VsItem Clone()
		{
			return (VsItem)this.MemberwiseClone();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="VsItem"/> class.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="path">The path.</param>
		public VsItem(string name, string path, DateTime? modified) : this()
		{
			this.Name         = name ?? throw new ArgumentNullException(nameof(name));
			this.Path         = path ?? throw new ArgumentNullException(nameof(path));
			this.LastModified = modified ?? DateTime.Now;
			this.BranchName   = string.Empty;
		}

		/// <summary>
		/// Gets or sets the name.
		/// </summary>
		public string? Name
		{
			get => this.name;
			set
			{
				if (this.name == value)
					return;
				this.name = value;
				OnPropertyChanged();
			}
		}

		/// <summary>
		/// Gets or sets the path.
		/// </summary>
		public string? Path
		{
			get => this.path;
			set
			{
				if (this.path == value)
					return;
				this.path = value;
				OnPropertyChanged();
			}
		}

		/// <summary>
		/// Gets or sets the commands.
		/// </summary>
		public string? Commands
		{
			get => this.commands;
			set
			{
				if (this.commands == value)
					return;
				this.commands = value;
				OnPropertyChanged();
			}
		}

		/// <summary>
		/// Gets or sets the instance.
		/// </summary>
		public string? Instance
		{
			get => this.instance;
			set
			{
				if (this.instance == value)
					return;
				this.instance = value;
				OnPropertyChanged();
			}
		}

		/// <summary>
		/// Gets or sets the Visual Studio Version to use.
		/// </summary>
		public string? VsVersion
		{
			get => this.vsVersion;
			set
			{
				if (this.vsVersion == value)
					return;
				this.vsVersion = value;
				OnPropertyChanged();
			}
		}

		/// <summary>
		/// Gets the last modified.
		/// </summary>
		[JsonIgnore]
		public DateTime LastModified { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether changed.
		/// </summary>
		[JsonIgnore]
		public bool Changed { get; set; }

		/// <summary>
		/// Gets or sets the item type.
		/// </summary>
		public ItemTypeEnum ItemType
		{
			get => this.itemType;
			set
			{
				if (this.itemType == value)
					return;
				this.itemType = value;
				OnPropertyChanged();
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether is favorite.
		/// </summary>
		public bool IsFavorite
		{
			get => this.isFavorite;
			set
			{
				if (this.isFavorite == value)
					return;
				this.isFavorite = value;
				OnPropertyChanged();
			}
		}

		/// <summary>
		/// Gets or sets the GIT status marker (*, ! or ?).
		/// Raising PropertyChanged with empty path forces re-evaluation of bindings with no Path (converter-only binding).
		/// </summary>
		[JsonIgnore]
		public string? Status
		{
			get => this.status;
			set
			{
				if (this.status == value)
					return;
				this.status = value;
				OnPropertyChanged(string.Empty); // trigger converter bound to whole item
			}
		}

		/// <summary>
		/// Gets or sets the branch name.
		/// </summary>
		public string BranchName
		{
			get => this.branchName;
			set
			{
				if (this.branchName == value)
					return;
				this.branchName = value;
				OnPropertyChanged(string.Empty); // update branch converter
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether this item is expanded in UI.
		/// </summary>
		[JsonIgnore]
		public virtual bool Expanded
		{
			get => this.expanded;
			set
			{
				if (this.expanded == value)
					return;
				this.expanded = value;
				OnPropertyChanged();
			}
		}

		/// <summary>
		/// Child items (folders override, others keep empty list).
		/// </summary>
		[JsonIgnore]
		public virtual VsItemList Items
		{
			get => this.items;
			set
			{
				if (ReferenceEquals(this.items, value))
					return;
				this.items = value;
				OnPropertyChanged();
			}
		}

		/// <summary>
		/// Indicates if item is checked (used by import dialogs and options flags).
		/// </summary>
		[JsonIgnore]
		public bool Checked
		{
			get => this.checkedFlag;
			set
			{
				if (this.checkedFlag == value)
					return;
				this.checkedFlag = value;
				OnPropertyChanged();
			}
		}

		/// <summary>
		/// True if underlying file/folder is missing (warning state).
		/// </summary>
		[JsonIgnore]
		public bool Warning
		{
			get => this.warning;
			set
			{
				if (this.warning == value)
					return;
				this.warning = value;
				OnPropertyChanged();
			}
		}

		/// <summary>
		/// Refreshes the item.
		/// </summary>
		public virtual void Refresh()
		{
		}
	}
}
