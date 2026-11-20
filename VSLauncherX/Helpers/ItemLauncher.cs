using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

using VSLauncher.DataModel;

using VSLXshared.Helpers;

namespace VSLauncher.Helpers
{
    /// <summary>
    /// The item launcher.
    /// </summary>
    public class ItemLauncher
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ItemLauncher"/> class for a single item using per-IDE default targets.
        /// </summary>
        /// <param name="item">The item to launch.</param>
        /// <param name="defaultTargets">Optional per-IDE default instance identifiers.</param>
        /// <param name="legacyTarget">Optional legacy Visual Studio executable path for backward compatibility.</param>
        public ItemLauncher(VsItem item, IdeDefaultTarget[]? defaultTargets, string? legacyTarget = null)
        {
            SingleItem     = item ?? throw new ArgumentNullException(nameof(item));
            DefaultTargets = defaultTargets;
            LegacyTarget   = legacyTarget;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemLauncher"/> class for a folder using per-IDE default targets.
        /// </summary>
        /// <param name="folder">The folder to launch.</param>
        /// <param name="defaultTargets">Optional per-IDE default instance identifiers.</param>
        /// <param name="legacyTarget">Optional legacy Visual Studio executable path for backward compatibility.</param>
        public ItemLauncher(VsFolder folder, IdeDefaultTarget[]? defaultTargets, string? legacyTarget = null)
        {
            Solution       = folder ?? throw new ArgumentNullException(nameof(folder));
            DefaultTargets = defaultTargets;
            LegacyTarget   = legacyTarget;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemLauncher"/> class.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="target">The target.</param>
        public ItemLauncher(VsItem item, VisualStudioInstance target)
            : this(item, CreateLegacyDefaultTargets(target), target?.Location)
        {
            Target = target;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemLauncher"/> class.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="target">The target.</param>
        public ItemLauncher(VsFolder item, VisualStudioInstance target)
            : this(item, CreateLegacyDefaultTargets(target), target?.Location)
        {
            Target = target;
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
        public VisualStudioInstance? Target { get; }

        /// <summary>
        /// Gets the per-IDE default target identifiers.
        /// </summary>
        public IdeDefaultTarget[]? DefaultTargets { get; }

        /// <summary>
        /// Gets the optional legacy Visual Studio executable path.
        /// </summary>
        public string? LegacyTarget { get; }

        /// <summary>
        /// Gets the last exception.
        /// </summary>
        public Exception? LastException { get; private set; }

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

                if (Solution is null && SingleItem is null)
                {
                    throw new InvalidOperationException("No item or folder specified to launch.");
                }

                bool bRequireAdmin = bForceAdmin || (Solution is null ? SingleItem!.RunAsAdmin : Solution.RunAsAdmin);
                var psi = new ProcessStartInfo
                {
                    FileName        = GetLauncherPath(),
                    Arguments       = s,
                    ErrorDialog     = false,
                    UseShellExecute = bRequireAdmin,
                    Verb            = bRequireAdmin ? "runas" : string.Empty,
                    WindowStyle     = ProcessWindowStyle.Hidden,
                };

                try
                {
                    using var p = Process.Start(psi);
                }
                catch (Exception ex)
                {
                    LastException = ex;
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
                Solution       = Solution ?? new VsFolder(SingleItem ?? throw new InvalidOperationException("No item specified to launch.")),
                Target         = LegacyTarget ?? Target?.Location,
                DefaultTargets = DefaultTargets
            };

            JsonSerializerSettings settings = FileHelper.CreateSafeJsonSettings(indented: false);
            string s                        = JsonConvert.SerializeObject(li, settings);

            return "b64:" + Convert.ToBase64String(Encoding.UTF8.GetBytes(s));
        }

        /// <summary>
        /// Builds a single-entry default targets array from a legacy <see cref="VisualStudioInstance"/>.
        /// </summary>
        /// <param name="target">The selected Visual Studio instance.</param>
        /// <returns>An array containing the Visual Studio target identifier, or <c>null</c> if unavailable.</returns>
        private static IdeDefaultTarget[]? CreateLegacyDefaultTargets(VisualStudioInstance? target)
        {
            if (target is null || string.IsNullOrWhiteSpace(target.Identifier))
            {
                return null;
            }

            return new[] { new IdeDefaultTarget(IdeType.VisualStudio, target.Identifier) };
        }
    }
}
