namespace DDev.WinTun.Abstractions;

public interface IWinTunService : IDisposable
{
    IWinTunAdapter CreateAdapter(string adapterName);
}