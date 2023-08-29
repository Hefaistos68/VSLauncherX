using System.Collections;

using VSLauncher.DataModel;

namespace VSLauncher.Helpers
{
    /// <summary>
    /// The import helper.
    /// </summary>
    public static class ImportHelper
    {
        /// <summary>
        /// Gets the item from extension.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="path">The path.</param>
        /// <returns>A VsItem.</returns>
        public static VsItem GetItemFromExtension(string name, string path)
        {
            var ext = Path.GetExtension(path)?.ToLower();

            if (ext == ".sln")
            {
                return new VsSolution(name, path);
            }
            else if (ext == ".csproj")
            {
                return new VsProject(name, path, ProjectTypeEnum.CSProject);
            }
            else if (ext == ".vbproj")
            {
                return new VsProject(name, path, ProjectTypeEnum.VBProject);
            }
            else if (ext == ".vcxproj")
            {
                return new VsProject(name, path, ProjectTypeEnum.CPPProject);
            }
            else if (ext == ".vcproj")
            {
                return new VsProject(name, path, ProjectTypeEnum.CPPProject);
            }
            else if (ext == ".fsproj")
            {
                return new VsProject(name, path, ProjectTypeEnum.FSProject);
            }
            else if (ext == ".esproj")
            {
                return new VsProject(name, path, ProjectTypeEnum.JSProject);
            }
            else if (ext == ".tsproj")
            {
                return new VsProject(name, path, ProjectTypeEnum.TSProject);
            }

            return new VsItem(name, path, null);
        }

		/// <summary>
		/// Filters the items.
		/// </summary>
		/// <param name="origin">The origin.</param>
		/// <param name="checkedItems">The checked items.</param>
		/// <returns>A VsItemList.</returns>
		public static VsItemList FilterCheckedItems(VsItemList origin, IList checkedItems)
		{
			VsItemList list = new VsItemList(null);

			bool bAdded = false;
			foreach (var i in origin)
			{
				if (checkedItems.Contains(i))
				{
					list.Add(i);
					bAdded = true;
				}

				// when adding a folder, it is in the checked list only when all its subitems are checked too
				// thus we can skip iterating through the subitems as they are already included
				if (i is VsFolder f && !bAdded)
				{
					if (f.Items.Count > 0)
					{
						var fi = ImportHelper.FilterCheckedItems(f.Items, checkedItems);

						if (fi.Count > 0)
						{
							VsFolder fn = f.Clone(false);
							fi.Reparent(fn);
							fn.Items = fi;
							list.Changed = true;

							if (!bAdded)
							{
								// may be because the parent item is not in the checkedItems list
								list.Add(fn);
							}
						}
					}
				}

				bAdded = false;
			}

			return list;
		}
	}
}
