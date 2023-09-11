using Microsoft.Win32.SafeHandles;
using System;

namespace VSLauncher.Helpers.Native
{
    internal class SafeTokenHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal SafeTokenHandle(IntPtr handle)
            : base(true)
        {
            SetHandle(handle);
        }

        private SafeTokenHandle()
            : base(true)
        {
        }

        protected override bool ReleaseHandle()
        {
            return ProcessApi.CloseHandle(handle);
        }
    }
}
