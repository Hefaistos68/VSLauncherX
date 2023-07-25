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
	public class SolutionGroup : VsFolder
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="SolutionGroup"/> class.
		/// </summary>
		public SolutionGroup() : base()
		{
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
	}
}
