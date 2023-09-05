using System.Text;

namespace VSLauncher.DataModel
{
	/// <summary>
	/// The vs options.
	/// </summary>
	public class VsOptions
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="VsOptions"/> class.
		/// </summary>
		public VsOptions()
		{
			this.RunBefore = null;
			this.RunAfter = null;
			this.RunAsAdmin = false;
		}

		/// <summary>
		/// Gets or sets the run before.
		/// </summary>
		public VsItem? RunBefore { get; set; }

		/// <summary>
		/// Gets or sets the run after.
		/// </summary>
		public VsItem? RunAfter { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether run as admin.
		/// </summary>
		public bool RunAsAdmin { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether show splash.
		/// </summary>
		public bool ShowSplash { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether wait for completion.
		/// </summary>
		public bool WaitForCompletion { get; set; }

		/// <summary>
		/// Gets or sets the preferred monitor.
		/// </summary>
		public int? PreferredMonitor { get; set; }

		/// <inheritdoc/>
		public override string? ToString()
		{
			StringBuilder sb = new();
			if(this.RunBefore != null)
			{
				sb.Append("Run Before: ");
				sb.Append(this.RunBefore.Name);
				sb.Append("\r\n");
			}
			if(this.RunAsAdmin)
			{
				sb.Append("Run as Admin");
				sb.Append("\r\n");
			}
			if(this.RunAfter != null)
			{
				sb.Append("Run After: ");
				sb.Append(this.RunAfter.Name);
				sb.Append("\r\n");
			}

			if (sb.Length == 0)
				return "None";

			return sb.ToString();
		}
	}
}
