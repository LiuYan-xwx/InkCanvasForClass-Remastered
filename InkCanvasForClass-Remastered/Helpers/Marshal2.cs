using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace InkCanvasForClass_Remastered.Helpers
{
    public static class Marshal2
    {
        internal const string OLEAUT32 = "oleaut32.dll";
        internal const string OLE32 = "ole32.dll";

        [System.Security.SecurityCritical]  // auto-generated_required
        public static unsafe object GetActiveObject(string progID)
        {
            if (string.IsNullOrEmpty(progID))
                throw new ArgumentNullException(nameof(progID));

            HRESULT hr;

            hr = PInvoke.CLSIDFromProgIDEx(progID, out Guid clsid);

            if (hr.Failed)
            {
                hr = PInvoke.CLSIDFromProgID(progID, out clsid);
            }

            if (hr.Failed)
            {
                Marshal.ThrowExceptionForHR(hr);
            }
            hr = PInvoke.GetActiveObject(in clsid, null, out object obj);

            if (hr.Failed)
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            return obj;
        }
    }
}
