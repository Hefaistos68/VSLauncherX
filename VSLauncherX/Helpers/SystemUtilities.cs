using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Security.Principal;

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
            WindowsIdentity id = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(id);

            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        /// <summary>
        /// We are elevated and should launch the process unelevated. We can't create the
        /// process directly without it becoming elevated. So to workaround this, we have
        /// explorer do the process creation (explorer is typically running unelevated).
        /// </summary>
        internal static void ExecuteProcessUnElevated(string process, string args, string currentDirectory = "")
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
            [return: MarshalAs(UnmanagedType.IDispatch)]
            object FindWindowSW([MarshalAs(UnmanagedType.Struct)] ref object pvarloc, [MarshalAs(UnmanagedType.Struct)] ref object pvarlocRoot, int swClass, out int pHWND, int swfwOptions);
        }

        [ComImport]
        [Guid("6d5140c1-7436-11ce-8034-00aa006009fa")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IServiceProvider
        {
            [return: MarshalAs(UnmanagedType.Interface)]
            object QueryService(ref Guid guidService, ref Guid riid);
        }

        [ComImport]
        [Guid("000214E2-0000-0000-C000-000000000046")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellBrowser
        {
            void VTableGap01(); // GetWindow
            void VTableGap02(); // ContextSensitiveHelp
            void VTableGap03(); // InsertMenusSB
            void VTableGap04(); // SetMenuSB
            void VTableGap05(); // RemoveMenusSB
            void VTableGap06(); // SetStatusTextSB
            void VTableGap07(); // EnableModelessSB
            void VTableGap08(); // TranslateAcceleratorSB
            void VTableGap09(); // BrowseObject
            void VTableGap10(); // GetViewStateStream
            void VTableGap11(); // GetControlWindow
            void VTableGap12(); // SendControlMsg
            IShellView QueryActiveShellView();
        }

        [ComImport]
        [Guid("000214E3-0000-0000-C000-000000000046")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellView
        {
            void VTableGap01(); // GetWindow
            void VTableGap02(); // ContextSensitiveHelp
            void VTableGap03(); // TranslateAcceleratorA
            void VTableGap04(); // EnableModeless
            void VTableGap05(); // UIActivate
            void VTableGap06(); // Refresh
            void VTableGap07(); // CreateViewWindow
            void VTableGap08(); // DestroyViewWindow
            void VTableGap09(); // GetCurrentInfo
            void VTableGap10(); // AddPropertySheetPages
            void VTableGap11(); // SaveViewState
            void VTableGap12(); // SelectItem

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
            object Application { [return: MarshalAs(UnmanagedType.IDispatch)] get; }
        }

        [ComImport]
        [Guid("A4C6892C-3BA9-11D2-9DEA-00C04FB16162")]
        [InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
        public interface IShellDispatch2
        {
            void ShellExecute([MarshalAs(UnmanagedType.BStr)] string File, [MarshalAs(UnmanagedType.Struct)] object vArgs, [MarshalAs(UnmanagedType.Struct)] object vDir, [MarshalAs(UnmanagedType.Struct)] object vOperation, [MarshalAs(UnmanagedType.Struct)] object vShow);
        }
    }

    public static class UnelevatedProcessStarter
    {
        public static int Start(string cmdArgs)
        {
            // 1. Get the shell
            var shell = NativeMethods.GetShellWindow();
            if (shell == IntPtr.Zero)
            {
                throw new Exception("Could not find shell window");
            }

            // 2. Copy the access token of the process
            uint shellProcessId;
            NativeMethods.GetWindowThreadProcessId(shell, out shellProcessId);
            var hShellProcess = NativeMethods.OpenProcess(0x00000400 /* QueryInformation */, false, (int)shellProcessId);
            IntPtr hShellToken;
            if (!NativeMethods.OpenProcessToken(hShellProcess, 2 /* TOKEN_DUPLICATE */, out hShellToken))
            {
                throw new Win32Exception();
            }

            // 3. Duplicate the access token
            uint tokenAccess = 8 /*TOKEN_QUERY*/ | 1 /*TOKEN_ASSIGN_PRIMARY*/ | 2 /*TOKEN_DUPLICATE*/ | 0x80 /*TOKEN_ADJUST_DEFAULT*/ | 0x100 /*TOKEN_ADJUST_SESSIONID*/;
            var securityAttributes = new SecurityAttributes();
            IntPtr hToken;
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
            var si = new Startupinfo();
            si.cb = Marshal.SizeOf(si);
            ProcessInformation processInfo;
            if (!NativeMethods.CreateProcessWithTokenW(
                hToken,
                0x00000002 /* LogonNetcredentialsOnly */,
                null,
                cmdArgs,
                0x00000010 /* CreateNewConsole */,
                IntPtr.Zero,
                null,
                ref si,
                out processInfo))
            {
                // Can't do that when not elevated (see https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-createprocesswithtokenw)
                // -> start the process as usual
                if (Marshal.GetLastWin32Error() == 1314)
                {
                    SecurityAttributes processSecurityAttributes = new SecurityAttributes();
                    SecurityAttributes threadSecurityAttributes = new SecurityAttributes();
                    if (!NativeMethods.CreateProcessAsUser(
                        IntPtr.Zero,
                        null,
                        cmdArgs,
                        ref processSecurityAttributes,
                        ref threadSecurityAttributes,
                        true,
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
                    throw new Win32Exception();
                }
            }

            return processInfo.dwProcessId;
        }

        public class NativeMethods
        {
            [DllImport("user32.dll")]
            public static extern IntPtr GetShellWindow();
            [DllImport("user32.dll", SetLastError = true)]
            public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern IntPtr OpenProcess(int processAccess, bool bInheritHandle, int processId);
            [DllImport("advapi32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool OpenProcessToken(IntPtr processHandle, uint desiredAccess, out IntPtr tokenHandle);
            [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern bool DuplicateTokenEx(IntPtr hExistingToken, uint dwDesiredAccess,
                ref SecurityAttributes lpTokenAttributes,
                int impersonationLevel,
                int tokenType,
                out IntPtr phNewToken);
            [DllImport("advapi32", SetLastError = true, CharSet = CharSet.Unicode)]
            public static extern bool CreateProcessWithTokenW(
                IntPtr hToken, int dwLogonFlags,
                string lpApplicationName, string lpCommandLine,
                int dwCreationFlags, IntPtr lpEnvironment,
                string lpCurrentDirectory,
                [In] ref Startupinfo lpStartupInfo,
                out ProcessInformation lpProcessInformation);

            [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            public static extern bool CreateProcessAsUser(
                IntPtr hToken,
                string lpApplicationName,
                string lpCommandLine,
                ref SecurityAttributes lpProcessAttributes,
                ref SecurityAttributes lpThreadAttributes,
                bool bInheritHandles,
                uint dwCreationFlags,
                IntPtr lpEnvironment,
                string lpCurrentDirectory,
                ref Startupinfo lpStartupInfo,
                out ProcessInformation lpProcessInformation);
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
}