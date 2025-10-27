using System.Runtime.InteropServices;
using DDev.WinTun.Abstractions;
using DDev.WinTun.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Win32.SafeHandles;

namespace DDev.WinTun.Implementation;

public class WinTunAdapter : IWinTunAdapter
{
    private const uint DefaultRingSize = 1u << 20;

    private readonly ILogger _logger;
    private readonly IntPtr _nativeAdapter;

    private IntPtr _session = IntPtr.Zero;

    private CancellationTokenSource? _receiverCts = null;
    private Task? _receiverTask = null;

    public WinTunAdapter(ILogger logger, IntPtr nativeLib, IntPtr nativeAdapter)
    {
        _logger = logger;
        _nativeAdapter = nativeAdapter;

        nativeLib.Export("WintunCloseAdapter", out _wintunCloseAdapter);
        nativeLib.Export("WintunStartSession", out _wintunStartSession);
        nativeLib.Export("WintunReceivePacket", out _wintunReceivePacket);
        nativeLib.Export("WintunGetReadWaitEvent", out _wintunGetReadWaitEvent);
        nativeLib.Export("WintunReleaseReceivePacket", out _wintunReleaseReceivePacket);
        nativeLib.Export("WintunEndSession", out _wintunEndSession);
        nativeLib.Export("WintunAllocateSendPacket", out _wintunAllocateSendPacket);
        nativeLib.Export("WintunSendPacket", out _wintunSendPacket);
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate void WintunCloseAdapterDelegate(IntPtr adapter);

    private readonly WintunCloseAdapterDelegate _wintunCloseAdapter;

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate IntPtr WintunStartSessionDelegate(IntPtr adapter, uint capacity);

    private readonly WintunStartSessionDelegate _wintunStartSession;

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate IntPtr WintunGetReadWaitEventDelegate(IntPtr session);

    private readonly WintunGetReadWaitEventDelegate _wintunGetReadWaitEvent;


    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate IntPtr WintunReceivePacketDelegate(IntPtr session, out uint packetSize);

    private readonly WintunReceivePacketDelegate _wintunReceivePacket;

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate void WintunReleaseReceivePacketDelegate(IntPtr session, IntPtr packet);

    private readonly WintunReleaseReceivePacketDelegate _wintunReleaseReceivePacket;


    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate IntPtr WintunAllocateSendPacketDelegate(IntPtr session, uint packetSize);

    private readonly WintunAllocateSendPacketDelegate _wintunAllocateSendPacket;

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate void WintunSendPacketDelegate(IntPtr session, IntPtr packet);

    private readonly WintunSendPacketDelegate _wintunSendPacket;


    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate void WintunEndSessionDelegate(IntPtr adapter);

    private readonly WintunEndSessionDelegate _wintunEndSession;

    private async Task ReceiveAsync(CancellationToken cancellationToken)
    {
        var readWaitEvent = _wintunGetReadWaitEvent(_session);

        var safeHandle = new SafeWaitHandle(readWaitEvent, ownsHandle: false);
        using var waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
        waitHandle.SafeWaitHandle = safeHandle;


        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                WaitHandle.WaitAny([waitHandle], Timeout.Infinite);

                while (!cancellationToken.IsCancellationRequested)
                {
                    IntPtr packetPtr = _wintunReceivePacket(_session, out var packetSize);
                    if (packetPtr == IntPtr.Zero || packetSize == 0)
                        break;

                    var managed = new byte[packetSize];
                    Marshal.Copy(packetPtr, managed, 0, (int)packetSize);

                    PacketReceived?.Invoke(this, managed);

                    _wintunReleaseReceivePacket(_session, packetPtr);
                }
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "Error receiving packet from WinTun adapter.");
            }
        }
    }

    public event EventHandler<byte[]>? PacketReceived;

    public void SendPacket(byte[] data)
    {
        if (_session == IntPtr.Zero)
            throw new InvalidOperationException("Session not started.");

        if (data == null || data.Length == 0)
            throw new ArgumentException("Packet data is empty", nameof(data));

        IntPtr packet = _wintunAllocateSendPacket(_session, (uint)data.Length);
        if (packet == IntPtr.Zero)
            throw new InvalidOperationException("Failed to allocate send packet.");

        Marshal.Copy(data, 0, packet, data.Length);
        _wintunSendPacket(_session, packet);
    }

    public void StartSession(uint? capacity = null)
    {
        if (_session != IntPtr.Zero)
            throw new InvalidOperationException("Session already started.");

        _session = _wintunStartSession(_nativeAdapter, capacity ?? DefaultRingSize);

        if (_session == IntPtr.Zero)
            throw new InvalidOperationException("Failed to start session.");

        _receiverCts = new CancellationTokenSource();
        _receiverTask = ReceiveAsync(_receiverCts.Token);
    }

    public void EndSession()
    {
        if (_session == IntPtr.Zero)
            throw new InvalidOperationException("Session not started.");


        _receiverCts?.Cancel();
        _receiverCts?.Dispose();

        _receiverTask?
            .ContinueWith(_ => { }, TaskContinuationOptions.None)
            .GetAwaiter()
            .GetResult();

        _receiverCts = null;
        _receiverTask = null;

        _wintunEndSession(_session);

        _session = IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_session != IntPtr.Zero) EndSession();
        _wintunCloseAdapter(_nativeAdapter);
    }
}