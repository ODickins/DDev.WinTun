namespace DDev.WinTun.Abstractions;

public interface IWinTunAdapter : IDisposable
{
    void StartSession(uint? capacity = null);
    void EndSession();

    event EventHandler<byte[]>? PacketReceived;
    void SendPacket(byte[] data);
}