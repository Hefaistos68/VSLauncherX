using static VSLauncher.Helpers.Native.ProcessApi;
using Microsoft.Win32.SafeHandles;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;
using VSLauncher.Helpers.Native;

namespace VSLauncherX.Helpers
{
    /// <summary>
    /// Borrowed from gsudo
    /// </summary>
    internal static class ProcessHelper
    {
        private static string _cacheOwnExeName;

        public static WindowsIdentity GetProcessUser(this Process process)
        {
            IntPtr processHandle = IntPtr.Zero;
            try
            {
                OpenProcessToken(process.Handle, 8, out processHandle);
                WindowsIdentity wi = new WindowsIdentity(processHandle);
                return wi;
            }
            catch
            {
                return null;
            }
            finally
            {
                if (processHandle != IntPtr.Zero)
                {
                    CloseHandle(processHandle);
                }
            }
        }

        static internal int GetProcessIntegrityLevel(IntPtr processHandle)
        {
            /*
             * https://docs.microsoft.com/en-us/previous-versions/dotnet/articles/bb625963(v=msdn.10)?redirectedfrom=MSDN
             * https://support.microsoft.com/en-us/help/243330/well-known-security-identifiers-in-windows-operating-systems
            S-1-16-0		Untrusted Mandatory Level	An untrusted integrity level.
            S-1-16-4096		Low Mandatory Level	A low integrity level.
            S-1-16-8192		Medium Mandatory Level	A medium integrity level.
            S-1-16-8448		Medium Plus Mandatory Level	A medium plus integrity level.
            S-1-16-12288    	High Mandatory Level	A high integrity level.
            S-1-16-16384	    System Mandatory Level	A system integrity level.
            S-1-16-20480	    Protected Process Mandatory Level	A protected-process integrity level.
            S-1-16-28672	    Secure Process Mandatory Level	A secure process integrity level.
            */
            int IL = -1;
            //SafeWaitHandle hToken = null;
            IntPtr hToken = IntPtr.Zero;

            int cbTokenIL = 0;
            IntPtr pTokenIL = IntPtr.Zero;

            try
            {
                // Open the access token of the current process with TOKEN_QUERY.
                if (!OpenProcessToken(processHandle,
                    TokensApi.TOKEN_QUERY, out hToken))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                // Then we must query the size of the integrity level information
                // associated with the token. Note that we expect GetTokenInformation
                // to return false with the ERROR_INSUFFICIENT_BUFFER error code
                // because we've given it a null buffer. On exit cbTokenIL will tell
                // the size of the group information.
                if (!TokensApi.GetTokenInformation(hToken,
                    TokensApi.TOKEN_INFORMATION_CLASS.TokenIntegrityLevel, IntPtr.Zero, 0,
                    out cbTokenIL))
                {
                    int error = Marshal.GetLastWin32Error();
                    const int ERROR_INSUFFICIENT_BUFFER = 0x7a;
                    if (error != ERROR_INSUFFICIENT_BUFFER)
                    {
                        // When the process is run on operating systems prior to
                        // Windows Vista, GetTokenInformation returns false with the
                        // ERROR_INVALID_PARAMETER error code because
                        // TokenIntegrityLevel is not supported on those OS's.
                        throw new Win32Exception(error);
                    }
                }

                // Now we allocate a buffer for the integrity level information.
                pTokenIL = Marshal.AllocHGlobal(cbTokenIL);
                if (pTokenIL == IntPtr.Zero)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                // Now we ask for the integrity level information again. This may fail
                // if an administrator has added this account to an additional group
                // between our first call to GetTokenInformation and this one.
                if (!TokensApi.GetTokenInformation(hToken,
                    TokensApi.TOKEN_INFORMATION_CLASS.TokenIntegrityLevel, pTokenIL, cbTokenIL,
                    out cbTokenIL))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                // Marshal the TOKEN_MANDATORY_LABEL struct from native to .NET object.
                TokensApi.TOKEN_MANDATORY_LABEL tokenIL = (TokensApi.TOKEN_MANDATORY_LABEL)
                    Marshal.PtrToStructure(pTokenIL, typeof(TokensApi.TOKEN_MANDATORY_LABEL));

                IntPtr pIL = TokensApi.GetSidSubAuthority(tokenIL.Label.Sid, 0);
                IL = Marshal.ReadInt32(pIL);
            }
            finally
            {
                // Centralized cleanup for all allocated resources. Clean up only
                // those which were allocated, and clean them up in the right order.

                if (hToken != IntPtr.Zero)
                {
                    CloseHandle(hToken);
                    //                    Marshal.FreeHGlobal(hToken);
                    //                    hToken.Close();
                    //                    hToken = null;
                }

                if (pTokenIL != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(pTokenIL);
                    pTokenIL = IntPtr.Zero;
                    cbTokenIL = 0;
                }
            }

            return IL;
        }
    }
}
