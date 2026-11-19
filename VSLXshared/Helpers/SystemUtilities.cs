using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Security.Principal;
using System.Security.AccessControl;

namespace VSLauncher.Helpers
{
    /// <summary>
    /// Utility for accessing window IShell* interfaces in order to use them to launch a process unelevated
    /// </summary>
    public class SystemUtility
    {
        /// <summary>
        /// Are the admin.
        /// </summary>
        /// <returns>A bool.</returns>
        public static bool IsAdmin()
        {
            using WindowsIdentity id = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(id);

            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        /// <summary>
        /// We are elevated and should launch the process unelevated. We can't create the
        /// process directly without it becoming elevated. So to workaround this, we have
        /// explorer do the process creation (explorer is typically running unelevated).
        /// </summary>
        public static void ExecuteProcessUnElevated(string process, string args, string currentDirectory = "")
        {
            var shellWindows = (IShellWindows)new CShellWindows();

            // Get the desktop window
            object loc = CSIDL_Desktop;
            object unused = new object();
            int hwnd;
            var serviceProvider = (IServiceProvider)shellWindows.FindWindowSW(ref loc, ref unused, SWC_DESKTOP, out hwnd, SWFO_NEEDDISPATCH);

            // Get the shell browser
            var serviceGuid = SID_STopLevelBrowser;
            var interfaceGuid = typeof(IShellBrowser).GUID;
            var shellBrowser = (IShellBrowser)serviceProvider.QueryService(ref serviceGuid, ref interfaceGuid);

            // Get the shell dispatch
            var dispatch = typeof(IDispatch).GUID;
            var folderView = (IShellFolderViewDual)shellBrowser.QueryActiveShellView().GetItemObject(SVGIO_BACKGROUND, ref dispatch);
            var shellDispatch = (IShellDispatch2)folderView.Application;

            // Use the dispatch (which is unelevated) to launch the process for us
            shellDispatch.ShellExecute(process, args, currentDirectory, string.Empty, SW_SHOWNORMAL);
        }

        /// <summary>
        /// Interop definitions
        /// </summary>
        private const int CSIDL_Desktop = 0;
        private const int SWC_DESKTOP = 8;
        private const int SWFO_NEEDDISPATCH = 1;
        private const int SW_SHOWNORMAL = 1;
        private const int SVGIO_BACKGROUND = 0;
        private readonly static Guid SID_STopLevelBrowser = new Guid("4C96BE40-915C-11CF-99D3-00AA004AE837");

		/// <summary>
		/// The c shell windows.
		/// </summary>
		[ComImport]
        [Guid("9BA05972-F6A8-11CF-A442-00A0C90A8F39")]
        [ClassInterface(ClassInterfaceType.None)]
        private class CShellWindows
        {
        }

        [ComImport]
        [Guid("85CB6900-4D95-11CF-960C-0080C7F4EE85")]
        [InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
        private interface IShellWindows
        {
			/// <summary>
			/// Finds the window s w.
			/// </summary>
			/// <param name="pvarloc">The pvarloc.</param>
			/// <param name="pvarlocRoot">The pvarloc root.</param>
			/// <param name="swClass">The sw class.</param>
			/// <param name="pHWND">The p h w n d.</param>
			/// <param name="swfwOptions">The swfw options.</param>
			/// <returns>An object.</returns>
			[return: MarshalAs(UnmanagedType.IDispatch)]
            object FindWindowSW([MarshalAs(UnmanagedType.Struct)] ref object pvarloc, [MarshalAs(UnmanagedType.Struct)] ref object pvarlocRoot, int swClass, out int pHWND, int swfwOptions);
        }

        [ComImport]
        [Guid("6d5140c1-7436-11ce-8034-00aa006009fa")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IServiceProvider
        {
			/// <summary>
			/// Queries the service.
			/// </summary>
			/// <param name="guidService">The guid service.</param>
			/// <param name="riid">The riid.</param>
			/// <returns>An object.</returns>
			[return: MarshalAs(UnmanagedType.Interface)]
            object QueryService(ref Guid guidService, ref Guid riid);
        }

        [ComImport]
        [Guid("000214E2-0000-0000-C000-000000000046")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellBrowser
        {
			/// <summary>
			/// VS the table gap01.
			/// </summary>
			void VTableGap01(); // GetWindow
			/// <summary>
			/// VS the table gap02.
			/// </summary>
			void VTableGap02(); // ContextSensitiveHelp
			/// <summary>
			/// VS the table gap03.
			/// </summary>
			void VTableGap03(); // InsertMenusSB
			/// <summary>
			/// VS the table gap04.
			/// </summary>
			void VTableGap04(); // SetMenuSB
			/// <summary>
			/// VS the table gap05.
			/// </summary>
			void VTableGap05(); // RemoveMenusSB
			/// <summary>
			/// VS the table gap06.
			/// </summary>
			void VTableGap06(); // SetStatusTextSB
			/// <summary>
			/// VS the table gap07.
			/// </summary>
			void VTableGap07(); // EnableModelessSB
			/// <summary>
			/// VS the table gap08.
			/// </summary>
			void VTableGap08(); // TranslateAcceleratorSB
			/// <summary>
			/// VS the table gap09.
			/// </summary>
			void VTableGap09(); // BrowseObject
			/// <summary>
			/// VS the table gap10.
			/// </summary>
			void VTableGap10(); // GetViewStateStream
			/// <summary>
			/// VS the table gap11.
			/// </summary>
			void VTableGap11(); // GetControlWindow
			/// <summary>
			/// VS the table gap12.
			/// </summary>
			void VTableGap12(); // SendControlMsg
			/// <summary>
			/// Queries the active shell view.
			/// </summary>
			/// <returns>An IShellView.</returns>
			IShellView QueryActiveShellView();
        }

        [ComImport]
        [Guid("000214E3-0000-0000-C000-000000000046")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellView
        {
			/// <summary>
			/// VS the table gap01.
			/// </summary>
			void VTableGap01(); // GetWindow
			/// <summary>
			/// VS the table gap02.
			/// </summary>
			void VTableGap02(); // ContextSensitiveHelp
			/// <summary>
			/// VS the table gap03.
			/// </summary>
			void VTableGap03(); // TranslateAcceleratorA
			/// <summary>
			/// VS the table gap04.
			/// </summary>
			void VTableGap04(); // EnableModeless
			/// <summary>
			/// VS the table gap05.
			/// </summary>
			void VTableGap05(); // UIActivate
			/// <summary>
			/// VS the table gap06.
			/// </summary>
			void VTableGap06(); // Refresh
			/// <summary>
			/// VS the table gap07.
			/// </summary>
			void VTableGap07(); // CreateViewWindow
			/// <summary>
			/// VS the table gap08.
			/// </summary>
			void VTableGap08(); // DestroyViewWindow
			/// <summary>
			/// VS the table gap09.
			/// </summary>
			void VTableGap09(); // GetCurrentInfo
			/// <summary>
			/// VS the table gap10.
			/// </summary>
			void VTableGap10(); // AddPropertySheetPages
			/// <summary>
			/// VS the table gap11.
			/// </summary>
			void VTableGap11(); // SaveViewState
			/// <summary>
			/// VS the table gap12.
			/// </summary>
			void VTableGap12(); // SelectItem

			/// <summary>
			/// Gets the item object.
			/// </summary>
			/// <param name="aspectOfView">The aspect of view.</param>
			/// <param name="riid">The riid.</param>
			/// <returns>An object.</returns>
			[return: MarshalAs(UnmanagedType.Interface)]
            object GetItemObject(uint aspectOfView, ref Guid riid);
        }

        [ComImport]
        [Guid("00020400-0000-0000-C000-000000000046")]
        [InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
        private interface IDispatch
        {
        }

        [ComImport]
        [Guid("E7A1AF80-4D96-11CF-960C-0080C7F4EE85")]
        [InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
        private interface IShellFolderViewDual
        {
			/// <summary>
			/// Gets the application.
			/// </summary>
			object Application { [return: MarshalAs(UnmanagedType.IDispatch)] get; }
        }

        [ComImport]
        [Guid("A4C6892C-3BA9-11D2-9DEA-00C04FB16162")]
        [InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
        public interface IShellDispatch2
        {
			/// <summary>
			/// Shells the execute.
			/// </summary>
			/// <param name="File">The file.</param>
			/// <param name="vArgs">The v args.</param>
			/// <param name="vDir">The v dir.</param>
			/// <param name="vOperation">The v operation.</param>
			/// <param name="vShow">The v show.</param>
			void ShellExecute([MarshalAs(UnmanagedType.BStr)] string File, [MarshalAs(UnmanagedType.Struct)] object vArgs, [MarshalAs(UnmanagedType.Struct)] object vDir, [MarshalAs(UnmanagedType.Struct)] object vOperation, [MarshalAs(UnmanagedType.Struct)] object vShow);
        }
    }

	/// <summary>
	/// The unelevated process starter.
	/// </summary>
	public static class UnelevatedProcessStarter
    {
		/// <summary>
		/// Starts the.
		/// </summary>
		/// <param name="cmdArgs">The cmd args.</param>
		/// <returns>An int.</returns>
		public static int Start(string cmdArgs)
        {
            // 1. Get the shell
            var shell = NativeMethods.GetShellWindow();

            if (shell == IntPtr.Zero)
            {
                throw new Exception("Could not find shell window");
            }

            IntPtr hShellProcess = IntPtr.Zero;
            IntPtr hShellToken = IntPtr.Zero;
            IntPtr hToken = IntPtr.Zero;
            ProcessInformation processInfo = default;

            try
            {
                // 2. Copy the access token of the process
                NativeMethods.GetWindowThreadProcessId(shell, out uint shellProcessId);
                hShellProcess = NativeMethods.OpenProcess(0x00000400 /* QueryInformation */, false, (int)shellProcessId);

                if (hShellProcess == IntPtr.Zero)
                {
                    throw new Win32Exception();
                }

                if (!NativeMethods.OpenProcessToken(hShellProcess, 2 /* TOKEN_DUPLICATE */, out hShellToken))
                {
                    throw new Win32Exception();
                }

                // 3. Duplicate the access token
                uint tokenAccess = 8 /*TOKEN_QUERY*/ | 1 /*TOKEN_ASSIGN_PRIMARY*/ | 2 /*TOKEN_DUPLICATE*/ | 0x80 /*TOKEN_ADJUST_DEFAULT*/ | 0x100 /*TOKEN_ADJUST_SESSIONID*/;
                var securityAttributes = new SecurityAttributes
                {
                    nLength = Marshal.SizeOf<SecurityAttributes>()
                };

                if (!NativeMethods.DuplicateTokenEx(
                    hShellToken,
                    tokenAccess,
                    ref securityAttributes,
                    2 /* SecurityImpersonation */,
                    1 /* TokenPrimary */,
                    out hToken))
                {
                    throw new Win32Exception();
                }

                // 4. Create a new process with the copied token
                var si = new Startupinfo
                {
                    cb = Marshal.SizeOf<Startupinfo>()
                };
                var commandLine = new StringBuilder(cmdArgs);

                if (!NativeMethods.CreateProcessWithTokenW(
                    hToken,
                    0x00000002 /* LogonNetcredentialsOnly */,
                    null,
                    commandLine,
                    0x00000010 /* CreateNewConsole */,
                    IntPtr.Zero,
                    null,
                    ref si,
                    out processInfo))
                {
                    int lastError = Marshal.GetLastWin32Error();

                    // Can't do that when not elevated (see https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-createprocesswithtokenw)
                    // -> start the process normally without requiring SeImpersonatePrivilege
                    if (lastError == 1314)
                    {
                        var processSecurityAttributes = new SecurityAttributes
                        {
                            nLength = Marshal.SizeOf<SecurityAttributes>()
                        };
                        var threadSecurityAttributes = new SecurityAttributes
                        {
                            nLength = Marshal.SizeOf<SecurityAttributes>()
                        };
                        var fallbackCommandLine = new StringBuilder(cmdArgs);

                        if (!NativeMethods.CreateProcessW(
                            null,
                            fallbackCommandLine,
                            ref processSecurityAttributes,
                            ref threadSecurityAttributes,
                            false,
                            0x00000010 /* CreateNewConsole */,
                            IntPtr.Zero,
                            null,
                            ref si,
                            out processInfo))
                        {
                            throw new Win32Exception();
                        }
                    }
                    else
                    {
                        throw new Win32Exception(lastError);
                    }
                }

                return processInfo.dwProcessId;
            }
            finally
            {
                CloseIfValid(processInfo.hThread);
                CloseIfValid(processInfo.hProcess);
                CloseIfValid(hToken);
                CloseIfValid(hShellToken);
                CloseIfValid(hShellProcess);
            }
        }

        /// <summary>
        /// Closes a native Win32 handle if it is non-zero.
        /// </summary>
        /// <param name="handle">The handle to close.</param>
        private static void CloseIfValid(IntPtr handle)
        {
            if (handle != IntPtr.Zero)
            {
                NativeMethods.CloseHandle(handle);
            }
        }

		/// <summary>
		/// The native methods.
		/// </summary>
		public class NativeMethods
        {
			/// <summary>
			/// Gets the shell window.
			/// </summary>
			/// <returns>An IntPtr.</returns>
			[DllImport("user32.dll")]
            public static extern IntPtr GetShellWindow();
			/// <summary>
			/// Gets the window thread process id.
			/// </summary>
			/// <param name="hWnd">The h wnd.</param>
			/// <param name="lpdwProcessId">The lpdw process id.</param>
			/// <returns>An uint.</returns>
			[DllImport("user32.dll", SetLastError = true)]
            public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
			/// <summary>
			/// Opens the process.
			/// </summary>
			/// <param name="processAccess">The process access.</param>
			/// <param name="bInheritHandle">If true, b inherit handle.</param>
			/// <param name="processId">The process id.</param>
			/// <returns>An IntPtr.</returns>
			[DllImport("kernel32.dll", SetLastError = true)]
            public static extern IntPtr OpenProcess(int processAccess, bool bInheritHandle, int processId);
			/// <summary>
			/// Opens the process token.
			/// </summary>
			/// <param name="processHandle">The process handle.</param>
			/// <param name="desiredAccess">The desired access.</param>
			/// <param name="tokenHandle">The token handle.</param>
			/// <returns>A bool.</returns>
			[DllImport("advapi32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool OpenProcessToken(IntPtr processHandle, uint desiredAccess, out IntPtr tokenHandle);
			/// <summary>
			/// Duplicates the token ex.
			/// </summary>
			/// <param name="hExistingToken">The h existing token.</param>
			/// <param name="dwDesiredAccess">The dw desired access.</param>
			/// <param name="lpTokenAttributes">The lp token attributes.</param>
			/// <param name="impersonationLevel">The impersonation level.</param>
			/// <param name="tokenType">The token type.</param>
			/// <param name="phNewToken">The ph new token.</param>
			/// <returns>A bool.</returns>
			[DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern bool DuplicateTokenEx(IntPtr hExistingToken, uint dwDesiredAccess,
                ref SecurityAttributes lpTokenAttributes,
                int impersonationLevel,
                int tokenType,
                out IntPtr phNewToken);
			/// <summary>
			/// Creates the process with token w.
			/// </summary>
			/// <param name="hToken">The h token.</param>
			/// <param name="dwLogonFlags">The dw logon flags.</param>
			/// <param name="lpApplicationName">The lp application name.</param>
			/// <param name="lpCommandLine">The lp command line.</param>
			/// <param name="dwCreationFlags">The dw creation flags.</param>
			/// <param name="lpEnvironment">The lp environment.</param>
			/// <param name="lpCurrentDirectory">The lp current directory.</param>
			/// <param name="lpStartupInfo">The lp startup info.</param>
			/// <param name="lpProcessInformation">The lp process information.</param>
			/// <returns>A bool.</returns>
			[DllImport("advapi32", SetLastError = true, CharSet = CharSet.Unicode)]
            public static extern bool CreateProcessWithTokenW(
                IntPtr hToken, int dwLogonFlags,
                string? lpApplicationName, StringBuilder lpCommandLine,
                int dwCreationFlags, IntPtr lpEnvironment,
                string? lpCurrentDirectory,
                [In] ref Startupinfo lpStartupInfo,
                out ProcessInformation lpProcessInformation);

			/// <summary>
			/// Creates a process in the current security context.
			/// </summary>
			/// <param name="lpApplicationName">The application name.</param>
			/// <param name="lpCommandLine">The mutable command line.</param>
			/// <param name="lpProcessAttributes">The process attributes.</param>
			/// <param name="lpThreadAttributes">The thread attributes.</param>
			/// <param name="bInheritHandles">Whether handles are inherited.</param>
			/// <param name="dwCreationFlags">Creation flags.</param>
			/// <param name="lpEnvironment">Environment pointer.</param>
			/// <param name="lpCurrentDirectory">Working directory.</param>
			/// <param name="lpStartupInfo">Startup info.</param>
			/// <param name="lpProcessInformation">Process info.</param>
			/// <returns>True if process creation succeeded.</returns>
			[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            public static extern bool CreateProcessW(
                string? lpApplicationName,
                StringBuilder lpCommandLine,
                ref SecurityAttributes lpProcessAttributes,
                ref SecurityAttributes lpThreadAttributes,
                bool bInheritHandles,
                uint dwCreationFlags,
                IntPtr lpEnvironment,
                string? lpCurrentDirectory,
                ref Startupinfo lpStartupInfo,
                out ProcessInformation lpProcessInformation);

			/// <summary>
			/// Closes an open kernel handle.
			/// </summary>
			/// <param name="hObject">Handle to close.</param>
			/// <returns>True on success.</returns>
			[DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool CloseHandle(IntPtr hObject);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ProcessInformation
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SecurityAttributes
        {
            public int nLength;
            public IntPtr lpSecurityDescriptor;
            public int bInheritHandle;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct Startupinfo
        {
            public int cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public int dwX;
            public int dwY;
            public int dwXSize;
            public int dwYSize;
            public int dwXCountChars;
            public int dwYCountChars;
            public int dwFillAttribute;
            public int dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }
    }

	/// <summary>
	/// The path helper.
	/// </summary>
	public static class PathHelper
	{
		/// <summary>
		/// Checks if the path is valid.
		/// </summary>
		/// <param name="path">The path.</param>
		/// <returns>A bool.</returns>
		public static bool PathIsValid(string? path)
		{
			if (string.IsNullOrEmpty(path))
			{
				return false;
			}

			if (!Directory.Exists(path))
			{
				return false;
			}

			return true;
		}

		/// <summary>
		/// Checks if the Paths is valid and can be read.
		/// </summary>
		/// <param name="path">The path.</param>
		/// <returns>A bool.</returns>
		public static bool PathIsValidAndCanRead(string? path)
		{
			return PathIsValid(path) ? CanRead(path) : false;
		}
		/// <summary>
		/// Checks if the executing user has read access to the given path.
		/// </summary>
		/// <param name="path">The path.</param>
		/// <returns>A bool.</returns>
		public static bool CanRead(string? path)
		{
			if (string.IsNullOrEmpty(path))
			{
				return false;
			}

			try
			{
				DirectoryInfo di = new DirectoryInfo(path);
				var accessControlList = di.GetAccessControl();

				if (accessControlList == null)
				{
					return ProbeDirectoryRead(path);
				}

				//get the access rules that pertain to a valid SID/NTAccount.
				var accessRules = accessControlList.GetAccessRules(true, true, typeof(SecurityIdentifier));

				if (accessRules == null)
				{
					return ProbeDirectoryRead(path);
				}

				return EvaluateReadAccessRules(accessRules);
			}
			catch (Exception ex) when (ex is InvalidOperationException or PlatformNotSupportedException or PrivilegeNotHeldException)
			{
				// Non-NTFS volumes (e.g. FAT32/exFAT) or shares without readable ACLs: probe directory listing directly
				return ProbeDirectoryRead(path);
			}
			catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
			{
				return false;
			}
		}

		/// <summary>
		/// Evaluates ACL read rules strictly for the current user's SID and group SIDs.
		/// </summary>
		/// <param name="accessRules">Directory access rules.</param>
		/// <returns>True when read access is allowed and not denied for the current identity.</returns>
		private static bool EvaluateReadAccessRules(AuthorizationRuleCollection accessRules)
		{
			using WindowsIdentity identity = WindowsIdentity.GetCurrent();
			var relevantSids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			if (identity.User != null)
			{
				relevantSids.Add(identity.User.Value);
			}

			if (identity.Groups != null)
			{
				foreach (IdentityReference group in identity.Groups)
				{
					relevantSids.Add(group.Value);
				}
			}

			bool readAllow = false;
			bool readDeny = false;

			foreach (FileSystemAccessRule rule in accessRules)
			{
				if (!relevantSids.Contains(rule.IdentityReference.Value))
				{
					continue;
				}

				if ((FileSystemRights.Read & rule.FileSystemRights) != FileSystemRights.Read)
				{
					continue;
				}

				if (rule.AccessControlType == AccessControlType.Deny)
				{
					readDeny = true;
				}
				else if (rule.AccessControlType == AccessControlType.Allow)
				{
					readAllow = true;
				}
			}

			return readAllow && !readDeny;
		}

		/// <summary>
		/// Probes whether a directory can be enumerated by the current process.
		/// </summary>
		/// <param name="path">Directory path to probe.</param>
		/// <returns>True if enumeration succeeds.</returns>
		private static bool ProbeDirectoryRead(string path)
		{
			try
			{
				using var enumerator = Directory.EnumerateFileSystemEntries(path).GetEnumerator();
				_ = enumerator.MoveNext();

				return true;
			}
			catch
			{
				return false;
			}
		}

	}
}