using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

using VSLauncher.DataModel;

namespace VSLauncher
{
	/// <summary>
	/// The item launcher.
	/// </summary>
	public class ItemLauncher
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ItemLauncher"/> class.
		/// </summary>
		/// <param name="item">The item.</param>
		/// <param name="target">The target.</param>
		public ItemLauncher(VsItem item, VisualStudioInstance target)
		{
			this.Target = target;
			this.SingleItem = item;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ItemLauncher"/> class.
		/// </summary>
		/// <param name="item">The item.</param>
		/// <param name="target">The target.</param>
		public ItemLauncher(VsFolder item, VisualStudioInstance target)
		{
			this.Solution = item;
			this.Target = target;
		}

		/// <summary>
		/// Gets the launch item.
		/// </summary>
		public VsFolder? Solution { get; }

		/// <summary>
		/// Gets the single item.
		/// </summary>
		public VsItem? SingleItem { get; }

		/// <summary>
		/// Gets the target.
		/// </summary>
		public VisualStudioInstance Target { get; }

		/// <summary>
		/// Gets the last exception.
		/// </summary>
		public Exception LastException { get; private set; }

		/// <summary>
		/// Launches the.
		/// </summary>
		/// <param name="bForceAdmin">If true, b force admin.</param>
		public Task Launch(bool bForceAdmin = false)
		{
			// Execute the item, run the before and after items if set, elevate to admin if required
			return Task.Run(() =>
			{
				string s = CreateLaunchInfoString();

				Debug.WriteLine(s);
				if(this.Solution is null && this.SingleItem is null)
				{
					throw new NullReferenceException();
				}

				bool bRequireAdmin = bForceAdmin | (this.Solution is null ? this.SingleItem!.RunAsAdmin : this.Solution.RunAsAdmin);
				var psi = new System.Diagnostics.ProcessStartInfo
				{
					FileName = this.GetLauncherPath(),
					Arguments = s,
					ErrorDialog = false,
					UseShellExecute = bRequireAdmin,
					Verb = bRequireAdmin ? "runas" : "run",
					WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
				};

				try
				{
					var p = System.Diagnostics.Process.Start(psi);
				}
				catch (Exception ex)
				{
					this.LastException = ex;
					Debug.WriteLine(ex.ToString());
				}
			});
		}

		/// <summary>
		/// Gets the launcher path.
		/// </summary>
		/// <returns>A string.</returns>
		public string GetLauncherPath()
		{
			string env = Environment.CurrentDirectory;

			// get working directory of current assembly
			string current = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? env;

			return Path.Combine(current, "BackgroundLaunch.exe");
		}

		/// <summary>
		/// Creates the launch info string.
		/// </summary>
		/// <returns>A string.</returns>
		public string CreateLaunchInfoString()
		{
			var li = new LaunchInfo() 
				{ 
					Solution = this.Solution ?? new VsFolder(this.SingleItem ?? throw new NullReferenceException()), 
					Target = this.Target.Location 
				};

			JsonSerializerSettings settings = new JsonSerializerSettings()
			{
				Formatting = Formatting.None,
			};

			string s = JsonConvert.SerializeObject(li, settings);

			s = s.Replace('\"', '«').Replace(' ', '»');

			return s;
		}
	}
}
