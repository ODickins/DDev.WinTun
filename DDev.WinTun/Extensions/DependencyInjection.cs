using DDev.WinTun.Abstractions;
using DDev.WinTun.Implementation;
using Microsoft.Extensions.DependencyInjection;

namespace DDev.WinTun.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddWinTun(this IServiceCollection services)
    {
        services.AddSingleton<IWinTunService, WinTunService>();
        return services;
    }
}