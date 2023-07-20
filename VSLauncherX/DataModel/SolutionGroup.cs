using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VSLauncher.DataModel
{

	/// <summary>
	/// The solution group.
	/// </summary>
	public class SolutionGroup : VsItem
    {
		/// <summary>
		/// Initializes a new instance of the <see cref="SolutionGroup"/> class.
		/// </summary>
		public SolutionGroup() : base()
		{
			this.Solutions = new List<VsItem>();
			this.Name = "<empty>";
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="SolutionGroup"/> class.
		/// </summary>
		/// <param name="name">The name.</param>
		public SolutionGroup(string name) : this()
		{
			this.Name = name;
		}

		/// <summary>
		/// Gets or sets the solutions.
		/// </summary>
		public List<VsItem> Solutions { get; set; }

    }
}
