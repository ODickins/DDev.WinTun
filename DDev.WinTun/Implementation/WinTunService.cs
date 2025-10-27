using System.Runtime.InteropServices;
using DDev.WinTun.Abstractions;
using DDev.WinTun.Extensions;
using Microsoft.Extensions.Logging;

namespace DDev.WinTun.Implementation;

public class WinTunService : IWinTunService
{
    private const string AdapterType = "Wintun";

    private readonly ILogger<WinTunService> _logger;
    private IntPtr _nativeLib;

    public WinTunService(ILogger<WinTunService> logger)
    {
        _logger = logger;

        var dllPath = RuntimeInformation.OSArchitecture switch
        {
            Architecture.Arm64 => Path.Combine(AppContext.BaseDirectory, "includes", "arm64", "wintun.dll"),
            Architecture.Arm => Path.Combine(AppContext.BaseDirectory, "includes", "arm", "wintun.dll"),
            Architecture.X64 => Path.Combine(AppContext.BaseDirectory, "includes", "amd64", "wintun.dll"),
            Architecture.X86 => Path.Combine(AppContext.BaseDirectory, "includes", "x86", "wintun.dll"),
            _ => throw new Exception("Unsupported architecture")
        };

        _nativeLib = NativeLibrary.Load(dllPath);
        _nativeLib.Export("WintunCreateAdapter", out _wintunCreateAdapter);
    }


    public IWinTunAdapter CreateAdapter(string adapterName)
    {
        var adapter = _wintunCreateAdapter(adapterName, AdapterType, Guid.NewGuid());
        return new WinTunAdapter(_logger, _nativeLib, adapter);
    }


    public void Dispose()
    {
        if (_nativeLib == IntPtr.Zero) return;
        NativeLibrary.Free(_nativeLib);
        _nativeLib = IntPtr.Zero;
    }


    #region Exports

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate IntPtr WintunCreateAdapterDelegate([MarshalAs(UnmanagedType.LPWStr)] string name,
        [MarshalAs(UnmanagedType.LPWStr)] string tunFriendlyName,
        Guid adapterGuid);

    private readonly WintunCreateAdapterDelegate _wintunCreateAdapter;

    #endregion
}