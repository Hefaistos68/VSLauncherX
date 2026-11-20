using VSLauncher.DataModel;

namespace VSLauncher.Helpers
{
	/// <summary>
	/// Provides hierarchical tree search and ancestor expansion lookup across collapsed <see cref="VsFolder"/> hierarchies.
	/// </summary>
	public static class TreeSearchHelper
	{
		/// <summary>
		/// Finds all descendant <see cref="VsFolder"/> instances under <paramref name="root"/> that contain at least one matching descendant,
		/// ordered top-down (outer ancestor before inner ancestor) without duplicates.
		/// </summary>
		/// <param name="root">The invisible root container folder.</param>
		/// <param name="isMatch">Predicate identifying matching items.</param>
		/// <returns>Top-down list of folders to expand so every matching item becomes visible.</returns>
		public static List<VsFolder> FindFoldersToExpand(VsFolder root, Func<VsItem, bool> isMatch)
		{
			ArgumentNullException.ThrowIfNull(root);
			ArgumentNullException.ThrowIfNull(isMatch);

			var foldersToExpand = new List<VsFolder>();

			_ = CollectFoldersToExpand(root, includeSelf: false, isMatch, foldersToExpand);

			return foldersToExpand;
		}

		/// <summary>
		/// Finds the first matching <see cref="VsItem"/> in depth-first tree order, including inside collapsed folders.
		/// </summary>
		/// <param name="root">The invisible root container folder.</param>
		/// <param name="isMatch">Predicate identifying matching items.</param>
		/// <returns>The first matching item, or <c>null</c> if none matches.</returns>
		public static VsItem? FindFirstMatch(VsFolder root, Func<VsItem, bool> isMatch)
		{
			ArgumentNullException.ThrowIfNull(root);
			ArgumentNullException.ThrowIfNull(isMatch);

			var flattened = new List<VsItem>();

			FlattenItems(root, flattened);

			foreach (VsItem item in flattened)
			{
				if (isMatch(item))
				{
					return item;
				}
			}

			return null;
		}

		/// <summary>
		/// Finds the next matching <see cref="VsItem"/> in depth-first tree order with wrap-around, including inside collapsed folders.
		/// </summary>
		/// <param name="root">The invisible root container folder.</param>
		/// <param name="startAfter">Currently focused item, or <c>null</c> to start from the first item.</param>
		/// <param name="includeStartItem">When <c>true</c>, evaluates <paramref name="startAfter"/> first (multi-character prefix refinement); when <c>false</c>, starts after it.</param>
		/// <param name="isMatch">Predicate identifying matching items.</param>
		/// <returns>The next matching item, or <c>null</c> if no item matches.</returns>
		public static VsItem? FindNextMatch(VsFolder root, VsItem? startAfter, bool includeStartItem, Func<VsItem, bool> isMatch)
		{
			ArgumentNullException.ThrowIfNull(root);
			ArgumentNullException.ThrowIfNull(isMatch);

			var flattened = new List<VsItem>();

			FlattenItems(root, flattened);

			if (flattened.Count == 0)
			{
				return null;
			}

			int startIndex = ResolveStartIndex(flattened, startAfter, includeStartItem);

			for (int offset = 0; offset < flattened.Count; offset++)
			{
				int index = (startIndex + offset) % flattened.Count;

				if (isMatch(flattened[index]))
				{
					return flattened[index];
				}
			}

			return null;
		}

		/// <summary>
		/// Gets the ancestor <see cref="VsFolder"/> chain of <paramref name="target"/> below <paramref name="root"/>, ordered top-down.
		/// </summary>
		/// <param name="root">The invisible root container folder.</param>
		/// <param name="target">The item whose ancestors should be found.</param>
		/// <returns>Top-down list of ancestor folders, or empty if <paramref name="target"/> is at root level or not found.</returns>
		public static List<VsFolder> GetAncestorFolders(VsFolder root, VsItem target)
		{
			ArgumentNullException.ThrowIfNull(root);
			ArgumentNullException.ThrowIfNull(target);

			var ancestors = new List<VsFolder>();

			if (!TryCollectAncestors(root, target, includeSelf: false, ancestors))
			{
				return new List<VsFolder>();
			}

			return ancestors;
		}

		/// <summary>
		/// Recursively collects folders that have at least one matching descendant.
		/// </summary>
		/// <param name="folder">Current folder being inspected.</param>
		/// <param name="includeSelf">Whether <paramref name="folder"/> itself may be added to <paramref name="result"/>.</param>
		/// <param name="isMatch">Predicate identifying matching items.</param>
		/// <param name="result">Destination top-down folder list.</param>
		/// <returns><c>true</c> if any descendant under <paramref name="folder"/> matched; otherwise <c>false</c>.</returns>
		private static bool CollectFoldersToExpand(VsFolder folder, bool includeSelf, Func<VsItem, bool> isMatch, List<VsFolder> result)
		{
			int insertIndex = result.Count;
			bool hasMatchingDescendant = false;

			foreach (VsItem child in folder.Items)
			{
				if (isMatch(child))
				{
					hasMatchingDescendant = true;
				}

				if (child is VsFolder subFolder && CollectFoldersToExpand(subFolder, includeSelf: true, isMatch, result))
				{
					hasMatchingDescendant = true;
				}
			}

			if (includeSelf && hasMatchingDescendant)
			{
				result.Insert(insertIndex, folder);
			}

			return hasMatchingDescendant;
		}

		/// <summary>
		/// Flattens all items under <paramref name="folder"/> into depth-first order.
		/// </summary>
		/// <param name="folder">Container folder to traverse.</param>
		/// <param name="target">Destination flattened list.</param>
		private static void FlattenItems(VsFolder folder, List<VsItem> target)
		{
			foreach (VsItem item in folder.Items)
			{
				target.Add(item);

				if (item is VsFolder subFolder)
				{
					FlattenItems(subFolder, target);
				}
			}
		}

		/// <summary>
		/// Resolves the starting index in a flattened item list for wrap-around searching.
		/// </summary>
		/// <param name="flattened">Flattened tree items.</param>
		/// <param name="startAfter">Currently focused item.</param>
		/// <param name="includeStartItem">Whether to start at <paramref name="startAfter"/> or at the item immediately after it.</param>
		/// <returns>Zero-based starting index within <paramref name="flattened"/>.</returns>
		private static int ResolveStartIndex(List<VsItem> flattened, VsItem? startAfter, bool includeStartItem)
		{
			if (startAfter is null)
			{
				return 0;
			}

			int currentIndex = flattened.IndexOf(startAfter);

			if (currentIndex < 0)
			{
				return 0;
			}

			return includeStartItem ? currentIndex : (currentIndex + 1) % flattened.Count;
		}

		/// <summary>
		/// Recursively searches for <paramref name="target"/> and records its top-down ancestor folders.
		/// </summary>
		/// <param name="folder">Current folder to search.</param>
		/// <param name="target">Target item to locate.</param>
		/// <param name="includeSelf">Whether <paramref name="folder"/> should be added to <paramref name="ancestors"/> when found.</param>
		/// <param name="ancestors">Destination top-down ancestor list.</param>
		/// <returns><c>true</c> if <paramref name="target"/> was located beneath <paramref name="folder"/>; otherwise <c>false</c>.</returns>
		private static bool TryCollectAncestors(VsFolder folder, VsItem target, bool includeSelf, List<VsFolder> ancestors)
		{
			foreach (VsItem child in folder.Items)
			{
				if (ReferenceEquals(child, target))
				{
					if (includeSelf)
					{
						ancestors.Add(folder);
					}

					return true;
				}

				if (child is VsFolder subFolder)
				{
					int insertIndex = ancestors.Count;

					if (TryCollectAncestors(subFolder, target, includeSelf: true, ancestors))
					{
						if (includeSelf)
						{
							ancestors.Insert(insertIndex, folder);
						}

						return true;
					}
				}
			}

			return false;
		}
	}
}
