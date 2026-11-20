using NUnit.Framework;

using Shouldly;

using VSLauncher.DataModel;
using VSLauncher.Helpers;

namespace VSLXshared.Tests.Helpers
{
	/// <summary>
	/// Unit tests for hierarchical tree search and ancestor expansion lookup in <see cref="TreeSearchHelper"/>.
	/// </summary>
	[TestFixture]
	public class TreeSearchHelperTests
	{
		/// <summary>
		/// Verifies that <see cref="TreeSearchHelper.FindFoldersToExpand"/> returns top-down ancestors for deeply nested matches inside collapsed folders.
		/// </summary>
		[Test]
		public void FindFoldersToExpand_WhenMatchIsInsideNestedCollapsedFolders_ReturnsTopDownAncestorsOnly()
		{
			VsFolder root      = new VsFolder();
			VsFolder outer     = new VsFolder("GroupA", string.Empty) { Expanded = false };
			VsFolder inner     = new VsFolder("SubGroupB", string.Empty) { Expanded = false };
			VsFolder unrelated = new VsFolder("OtherGroup", string.Empty) { Expanded = false };
			VsSolution target  = new VsSolution { Name = "MyApp" };
			VsSolution other   = new VsSolution { Name = "Billing" };

			inner.Items.Add(target);
			outer.Items.Add(inner);
			unrelated.Items.Add(other);
			root.Items.Add(outer);
			root.Items.Add(unrelated);

			List<VsFolder> toExpand = TreeSearchHelper.FindFoldersToExpand(
				root,
				item => item.Name?.Contains("MyApp", StringComparison.OrdinalIgnoreCase) == true);

			toExpand.ShouldBe(new[] { outer, inner });
		}

		/// <summary>
		/// Verifies that a folder matching by its own name without matching descendants is not needlessly expanded, while its parent is expanded.
		/// </summary>
		[Test]
		public void FindFoldersToExpand_WhenNestedFolderItselfMatches_ExpandsParentButNotMatchedFolder()
		{
			VsFolder root   = new VsFolder();
			VsFolder parent = new VsFolder("Workspace", string.Empty) { Expanded = false };
			VsFolder child  = new VsFolder("Frontend", string.Empty) { Expanded = false };
			VsProject proj  = new VsProject { Name = "Other" };

			child.Items.Add(proj);
			parent.Items.Add(child);
			root.Items.Add(parent);

			List<VsFolder> toExpand = TreeSearchHelper.FindFoldersToExpand(
				root,
				item => item.Name?.Contains("Front", StringComparison.OrdinalIgnoreCase) == true);

			toExpand.ShouldBe(new[] { parent });
		}

		/// <summary>
		/// Verifies that shared ancestors are deduplicated when multiple children inside the same collapsed folder match.
		/// </summary>
		[Test]
		public void FindFoldersToExpand_WhenMultipleSiblingsMatch_ReturnsEachAncestorOnce()
		{
			VsFolder root      = new VsFolder();
			VsFolder group     = new VsFolder("Services", string.Empty) { Expanded = false };
			VsSolution first   = new VsSolution { Name = "AuthApi" };
			VsSolution second  = new VsSolution { Name = "BillingApi" };

			group.Items.Add(first);
			group.Items.Add(second);
			root.Items.Add(group);

			List<VsFolder> toExpand = TreeSearchHelper.FindFoldersToExpand(
				root,
				item => item.Name?.Contains("Api", StringComparison.OrdinalIgnoreCase) == true);

			toExpand.ShouldBe(new[] { group });
		}

		/// <summary>
		/// Verifies that <see cref="TreeSearchHelper.FindFirstMatch"/> finds the first depth-first match inside collapsed folders.
		/// </summary>
		[Test]
		public void FindFirstMatch_WhenMatchIsInsideCollapsedFolder_ReturnsFirstDepthFirstMatch()
		{
			VsFolder root      = new VsFolder();
			VsFolder group     = new VsFolder("GroupA", string.Empty) { Expanded = false };
			VsSolution nested  = new VsSolution { Name = "Orders" };
			VsSolution topItem = new VsSolution { Name = "OrdersLegacy" };

			group.Items.Add(nested);
			root.Items.Add(group);
			root.Items.Add(topItem);

			VsItem? match = TreeSearchHelper.FindFirstMatch(
				root,
				item => item.Name?.StartsWith("Ord", StringComparison.OrdinalIgnoreCase) == true);

			match.ShouldBeSameAs(nested);
		}

		/// <summary>
		/// Verifies that <see cref="TreeSearchHelper.FindNextMatch"/> finds items inside collapsed folders, cycles across single-character presses, and wraps around.
		/// </summary>
		[Test]
		public void FindNextMatch_WhenCyclingAcrossCollapsedAndVisibleItems_WrapsInTreeOrder()
		{
			VsFolder root     = new VsFolder();
			VsFolder group    = new VsFolder("Apps", string.Empty) { Expanded = false };
			VsSolution alpha  = new VsSolution { Name = "Alpha" };
			VsSolution beta   = new VsSolution { Name = "Beta" };
			VsSolution atlas  = new VsSolution { Name = "Atlas" };

			group.Items.Add(alpha);
			group.Items.Add(beta);
			root.Items.Add(group);
			root.Items.Add(atlas);

			bool MatchA(VsItem item) => item.Name?.StartsWith("A", StringComparison.OrdinalIgnoreCase) == true;

			VsItem? first  = TreeSearchHelper.FindNextMatch(root, startAfter: group, includeStartItem: false, MatchA);
			VsItem? second = TreeSearchHelper.FindNextMatch(root, startAfter: first, includeStartItem: false, MatchA);
			VsItem? third  = TreeSearchHelper.FindNextMatch(root, startAfter: second, includeStartItem: false, MatchA);

			first.ShouldBeSameAs(alpha);
			second.ShouldBeSameAs(atlas);
			third.ShouldBeSameAs(group);
		}

		/// <summary>
		/// Verifies that multi-character incremental typing keeps the currently focused item when it still matches, and advances when it no longer matches.
		/// </summary>
		[Test]
		public void FindNextMatch_WhenIncludeStartItemTrue_KeepsCurrentMatchUntilPrefixDiverges()
		{
			VsFolder root    = new VsFolder();
			VsFolder group   = new VsFolder("Work", string.Empty) { Expanded = false };
			VsSolution app1  = new VsSolution { Name = "AppOne" };
			VsSolution app2  = new VsSolution { Name = "AppTwo" };

			group.Items.Add(app1);
			group.Items.Add(app2);
			root.Items.Add(group);

			VsItem? same = TreeSearchHelper.FindNextMatch(
				root,
				startAfter: app1,
				includeStartItem: true,
				item => item.Name?.StartsWith("AppO", StringComparison.OrdinalIgnoreCase) == true);

			VsItem? next = TreeSearchHelper.FindNextMatch(
				root,
				startAfter: app1,
				includeStartItem: true,
				item => item.Name?.StartsWith("AppT", StringComparison.OrdinalIgnoreCase) == true);

			same.ShouldBeSameAs(app1);
			next.ShouldBeSameAs(app2);
		}

		/// <summary>
		/// Verifies that <see cref="TreeSearchHelper.GetAncestorFolders"/> returns top-down ancestors excluding the root container.
		/// </summary>
		[Test]
		public void GetAncestorFolders_ReturnsTopDownAncestorsForNestedItemAndEmptyForTopLevelOrMissing()
		{
			VsFolder root    = new VsFolder();
			VsFolder outer   = new VsFolder("Outer", string.Empty);
			VsFolder inner   = new VsFolder("Inner", string.Empty);
			VsSolution leaf  = new VsSolution { Name = "Leaf" };
			VsSolution top   = new VsSolution { Name = "Top" };
			VsSolution stray = new VsSolution { Name = "Stray" };

			inner.Items.Add(leaf);
			outer.Items.Add(inner);
			root.Items.Add(outer);
			root.Items.Add(top);

			TreeSearchHelper.GetAncestorFolders(root, leaf).ShouldBe(new[] { outer, inner });
			TreeSearchHelper.GetAncestorFolders(root, top).ShouldBeEmpty();
			TreeSearchHelper.GetAncestorFolders(root, stray).ShouldBeEmpty();
		}
	}
}
