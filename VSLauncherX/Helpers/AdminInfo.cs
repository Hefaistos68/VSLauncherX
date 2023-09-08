using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace VSLauncher.Helpers
{
	internal class AdminInfo
	{
		public static bool IsCurrentUserAdmin()
		{
			return UACHelper.UACHelper.IsAdministrator;
		}

		internal static bool IsElevated()
		{
			return UACHelper.UACHelper.IsElevated;
		}
	}
}
