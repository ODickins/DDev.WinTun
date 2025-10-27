using System.Runtime.InteropServices;

namespace DDev.WinTun.Extensions;

internal static class IntPtrExtensions
{
    internal static void Export<TDelegate>(this IntPtr ptr, string delegateName, out TDelegate @delegate) where TDelegate : Delegate
    {
        if (ptr == IntPtr.Zero)
            throw new InvalidOperationException("Native library not loaded.");

        if (!NativeLibrary.TryGetExport(ptr, delegateName, out var address) || ptr == IntPtr.Zero)
            throw new MissingMethodException($"Export '{delegateName}' not found");

        @delegate = Marshal.GetDelegateForFunctionPointer<TDelegate>(address);
    }
}