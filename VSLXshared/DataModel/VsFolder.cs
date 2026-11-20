using Newtonsoft.Json;

namespace VSLauncher.DataModel
{
	/// <summary>
	/// The vs folder.
	/// </summary>
	public class VsFolder : VsItem
	{
		private VsItemList? items;

		[JsonIgnore]
		private bool hasDiskWorkspaceCache;

		/// <summary>
		/// Initializes a new instance of the <see cref="VsFolder"/> class.
		/// </summary>
		public VsFolder()
		{
			this.Items = new VsItemList(this);
			this.ItemType = ItemTypeEnum.Folder;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="VsFolder"/> class.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="path">The path.</param>
		public VsFolder(string name, string path) : base(name, path, null)
		{
			this.Items = new VsItemList(this);
			this.ItemType = ItemTypeEnum.Folder;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="VsFolder"/> class.
		/// </summary>
		/// <param name="singleItem">The single item.</param>
		public VsFolder(VsItem singleItem)
		{
			this.Items = new VsItemList(this)
			{
				singleItem
			};
			this.ItemType = ItemTypeEnum.Folder;
		}

		/// <summary>
		/// Gets or sets a value indicating whether checked.
		/// </summary>
		[JsonIgnore]
		public new bool? Checked
		{
			get
			{
				int n1 = 0;
				int n2 = 0;

				if (Items.Count == 0)
				{
					return base.Checked;
				}

				foreach (var i in Items)
				{
					if (i is VsFolder f)
					{
						var c = f.Checked;
						if (c == true)
							n1++;
						else if (c == false)
							n2++;
					}
					else if (i.Checked)
						n1++;
					else
						n2++;
				}

				return n1 == Items.Count ? true : n2 == Items.Count ? false : null;
			}
			set
			{
				base.Checked = value ?? false;
				foreach (var i in Items)
				{
					if (i is VsFolder f)
					{
						f.Checked = value;
					}
					else
					{
						i.Checked = value ?? false;
					}
				}
			}
		}

		/// <summary>
		/// Gets or sets the icon.
		/// </summary>
		[JsonIgnore]
		public Icon? Icon
		{ get; set; }

		/// <summary>
		/// Gets or sets the items.
		/// </summary>
		[JsonProperty("FolderItems")]
		public VsItemList Items
		{
			get
			{
				items ??= new VsItemList(this);

				return items;
			}
			set
			{
				items = value ?? new VsItemList(this);
				items.Reparent(this);
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether this folder is expanded
		/// </summary>
		public bool Expanded { get; set; }

		/// <summary>
		/// Gets or sets the Visual Studio Code launch options associated with this folder.
		/// </summary>
		public VsCodeLaunchOptions? VsCodeOptions { get; set; }

		/// <inheritdoc/>
		public override VsItem Clone()
		{
			return Clone(true);
		}

		/// <summary>
		/// Clones the object
		/// </summary>
		/// <returns>A VsItem.</returns>
		public VsFolder Clone(bool withItems)
		{
			var f = (VsFolder)this.MemberwiseClone();

			f.RunBefore     = this.RunBefore?.Clone();
			f.RunAfter      = this.RunAfter?.Clone();
			f.VsCodeOptions = this.VsCodeOptions?.Clone();
			f.items         = new VsItemList(f);

			if (withItems)
			{
				foreach (var item in this.Items)
				{
					f.items.Add(item.Clone());
				}
			}

			return f;
		}
		/// <summary>
		/// Reutrns the number of projects this item holds.
		/// </summary>
		/// <returns>An int.</returns>
		public int ContainedProjectsCount()
		{
			int n = 0;

			foreach (var i in this.Items)
			{
				if (i is VsFolder f)
				{
					n += f.ContainedProjectsCount();
				}
				else if (i is VsProject or VsCodeProject)
				{
					n++;
				}
			}

			return n;
		}

		/// <summary>
		/// Determines whether this folder is explicitly configured to launch directly in Visual Studio Code.
		/// </summary>
		/// <returns><c>true</c> if this is a path-backed folder with <see cref="IdeType.VsCode"/> or non-null <see cref="VsCodeOptions"/>; otherwise <c>false</c>.</returns>
		public bool IsVsCodeConfigured()
		{
			return this.ItemType == ItemTypeEnum.Folder
				&& !string.IsNullOrWhiteSpace(this.Path)
				&& (this.IdeType == IdeType.VsCode || this.VsCodeOptions is not null);
		}

		/// <summary>
		/// Determines whether this folder should launch directly in Visual Studio Code, either because it is explicitly configured for VS Code or because it is workspace-backed.
		/// </summary>
		/// <param name="allowDiskProbe">If <c>true</c>, probes the filesystem when checking for a <c>.code-workspace</c> file; if <c>false</c>, uses cached/in-memory state only.</param>
		/// <returns><c>true</c> if this folder launches directly in VS Code; otherwise <c>false</c>.</returns>
		public bool IsVsCodeLaunchTarget(bool allowDiskProbe = true)
		{
			return IsVsCodeConfigured() || IsWorkspaceBacked(allowDiskProbe);
		}

		/// <summary>
		/// Determines whether this folder represents a disk-backed folder with a <c>.code-workspace</c> file or workspace child.
		/// </summary>
		/// <param name="allowDiskProbe">If <c>true</c>, probes the filesystem when validating disk state and refreshes the cached disk workspace flag; if <c>false</c>, evaluates only in-memory children and the cached flag without performing disk I/O.</param>
		/// <returns><c>true</c> if this folder is workspace-backed; otherwise <c>false</c>.</returns>
		public bool IsWorkspaceBacked(bool allowDiskProbe = true)
		{
			if (this.ItemType != ItemTypeEnum.Folder || string.IsNullOrWhiteSpace(this.Path))
			{
				if (allowDiskProbe)
				{
					this.hasDiskWorkspaceCache = false;
				}

				return false;
			}

			if (!allowDiskProbe)
			{
				return this.hasDiskWorkspaceCache || HasDirectWorkspaceChild();
			}

			if (!Directory.Exists(this.Path))
			{
				this.hasDiskWorkspaceCache = false;

				return false;
			}

			bool hasDiskWorkspace      = ProbeDiskWorkspaceFile();
			this.hasDiskWorkspaceCache = hasDiskWorkspace;

			return HasDirectWorkspaceChild() || hasDiskWorkspace;
		}

		/// <summary>
		/// Determines whether <see cref="Items"/> directly contains a <see cref="VsCodeProject"/> with <see cref="VsCodeProjectKind.Workspace"/>.
		/// </summary>
		/// <returns><c>true</c> if a direct workspace child exists; otherwise <c>false</c>.</returns>
		private bool HasDirectWorkspaceChild()
		{
			foreach (var item in this.Items)
			{
				if (item is VsCodeProject codeProject && codeProject.ProjectKind == VsCodeProjectKind.Workspace)
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Probes <see cref="VsItem.Path"/> on disk for at least one top-level <c>.code-workspace</c> file.
		/// </summary>
		/// <returns><c>true</c> if a <c>.code-workspace</c> file exists; otherwise <c>false</c>.</returns>
		private bool ProbeDiskWorkspaceFile()
		{
			if (this.ItemType != ItemTypeEnum.Folder || string.IsNullOrWhiteSpace(this.Path) || !Directory.Exists(this.Path))
			{
				return false;
			}

			try
			{
				return Directory.EnumerateFiles(this.Path, "*.code-workspace", SearchOption.TopDirectoryOnly).Any();
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
			{
				return false;
			}
		}

		/// <summary>
		/// Reutrns the number of solutions this item holds.
		/// </summary>
		/// <returns>An int.</returns>
		public int ContainedSolutionsCount()
		{
			int n = 0;

			foreach (var i in this.Items)
			{
				if (i is VsFolder f)
				{
					n += f.ContainedSolutionsCount();
				}
				else if (i is VsSolution)
				{
					n++;
				}
			}

			return n;
		}
		/// <inheritdoc/>
		public override void Refresh()
		{
			this.hasDiskWorkspaceCache = ProbeDiskWorkspaceFile();

			foreach (var i in this.Items)
			{
				i.Refresh();
			}
		}

		/// <summary>
		/// Tos the string.
		/// </summary>
		/// <returns>A string? .</returns>
		public new string? ToString()
		{
			return base.ToString();
		}

		/// <summary>
		/// Finds the parent of the given item, recurses through all subitems
		/// </summary>
		/// <param name="item">The item.</param>
		/// <returns>A VsFolder.</returns>
		public VsFolder? FindParent(object item)
		{
			if (this.Items.Contains(item))
				return this;

			foreach (var i in this.Items)
			{
				if (i is VsFolder f)
				{
					var p = f.FindParent(item);
					if (p != null)
					{
						return p;
					}
				}
			}

			return null;
		}
	}
}