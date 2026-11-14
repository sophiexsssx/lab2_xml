using System.Runtime.InteropServices;
using Foundation;
using ObjCRuntime;

namespace XmlAnalyzerApp;

internal static class MacFileOpener
{
    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr SendIntPtr(IntPtr receiver, IntPtr selector);

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool SendBool(IntPtr receiver, IntPtr selector, IntPtr arg);

    public static bool Open(string path)
    {
        var workspace = SendIntPtr(Class.GetHandle("NSWorkspace"), Selector.GetHandle("sharedWorkspace"));
        if (workspace == IntPtr.Zero) return false;
        using var url = NSUrl.FromFilename(path);
        return SendBool(workspace, Selector.GetHandle("openURL:"), url.Handle);
    }
}
