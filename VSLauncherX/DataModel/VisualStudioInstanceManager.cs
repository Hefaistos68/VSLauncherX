using System.Management;
using System.Diagnostics;

namespace VSLauncher.DataModel
{
    /// <summary>
    /// The visual studio instance manager.
    /// </summary>
    public class VisualStudioInstanceManager
    {
        private List<VisualStudioInstance> allInstances;

        /// <summary>
        /// Initializes a new instance of the <see cref="VisualStudioInstanceManager"/> class.
        /// </summary>
        public VisualStudioInstanceManager()
        {
            allInstances = ReadAllInstances();
        }

        /// <summary>
        /// Gets the count.
        /// </summary>
        public int Count
        {
            get
            {
                return allInstances.Count;
            }
        }

        /// <summary>
        /// Access an instance by index
        /// </summary>
        public VisualStudioInstance this[int index]
        {
            get
            {
                return allInstances[index];
            }
        }

        /// <summary>
        /// Access an instance by its version number
        /// </summary>
        public VisualStudioInstance this[string version]
        {
            get
            {
                return allInstances.Where(x => x.Version == version).Single();
            }
        }

        public List<VisualStudioInstance> All
        {
            get { return allInstances; }
        }

        /// <summary>
        /// Reads the all installed Visual Studio instances from WMI
        /// </summary>
        /// <returns>A list of VisualStudioInstances.</returns>
        public static List<VisualStudioInstance> ReadAllInstances()
        {
            var list = new List<VisualStudioInstance>();
            // read all data from WMI using CimInstance MSFT_VSInstance
            // https://docs.microsoft.com/en-us/windows/win32/wmisdk/msft-vsinstance

            ManagementObjectSearcher searcher = new ManagementObjectSearcher
            {
                Query = new SelectQuery("MSFT_VSInstance ", "", new[] { "Name", "Version", "ProductLocation", "IdentifyingNumber" })
            };
            ManagementObjectCollection collection = searcher.Get();
            ManagementObjectCollection.ManagementObjectEnumerator em = collection.GetEnumerator();

            while (em.MoveNext())
            {
                ManagementBaseObject baseObj = em.Current;
                if (baseObj.Properties["Version"].Value != null)
                {
                    try
                    {
                        string name = baseObj.Properties["Name"].Value.ToString();
                        string version = baseObj.Properties["Version"].Value.ToString();
                        string location = baseObj.Properties["ProductLocation"].Value.ToString();
                        string identifier = baseObj.Properties["IdentifyingNumber"].Value.ToString();
                        string year = version.StartsWith("15") ? "2017" :
                                      version.StartsWith("16") ? "2019" :
                                      version.StartsWith("17") ? "2022" : "newer";

                        list.Add(new VisualStudioInstance(name, version, location, identifier, year));
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.ToString());
                    }
                }
            }

            em?.Dispose();
            collection?.Dispose();
            searcher?.Dispose();

            return list;
        }
    }
}
