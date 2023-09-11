using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;
using VSLauncher.Helpers.Tokens;
using VSLauncherX.Helpers;

namespace VSLauncher.Helpers
{
    internal class AdminInfo
	{
		private static WindowsIdentity _currentUserIdentity;

		private static WindowsIdentity CachedOwner => _currentUserIdentity ?? (_currentUserIdentity = Owner);

		public static WindowsIdentity Owner => WindowsIdentity.GetCurrent();

		/// <summary>
		/// Is the current user admin.
		/// </summary>
		/// <returns>A bool.</returns>
		public static bool IsCurrentUserAdmin()
		{
			return SecurityHelper.IsAdministrator() | SecurityHelper.IsMemberOfLocalAdmins();
		}

		/// <summary>
		/// Is the app elevated.
		/// </summary>
		/// <returns>A bool.</returns>
		internal static bool IsElevated()
		{
			IntegrityLevel currentIntegrity = (IntegrityLevel)SecurityHelper.GetCurrentIntegrityLevel();

			try
			{
				TokenProvider tp = TokenProvider.CreateFromCurrentProcessToken();
				var et = tp.GetTokenElevationType();

				return et == TokenProvider.TokenElevationType.Full;
			}
			catch
			{
				return currentIntegrity > IntegrityLevel.Medium;								 
			}
		}
	}
}
