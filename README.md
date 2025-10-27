# WinTun .NET Library

A simple .NET library for working with **Wintun virtual network adapters**, allowing you to create adapters, send and receive packets, and integrate with your applications using dependency injection.

---

## Features

- Create and manage Wintun adapters.
- Send and receive raw network packets.
- Supports async packet handling via events.
- Easy integration with **.NET Dependency Injection**.

---

## Usage

### 1. Register the service

Use the built-in **Dependency Injection** support to register the Wintun service:

```csharp
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddWinTun();

var serviceProvider = services.BuildServiceProvider();
var winTunService = serviceProvider.GetRequiredService<IWinTunService>();
```

### 2. Create an adapter

```csharp
var adapter = winTunService.CreateAdapter("MyAdapter");
```

### 3. Start a session

```csharp
adapter.StartSession(capacity: 1024); // Optional capacity parameter
```

### 4. Receive packets

Subscribe to the `PacketReceived` event to handle incoming packets:

```csharp
adapter.PacketReceived += (sender, packet) =>
{
    Console.WriteLine($"Received {packet.Length} bytes");
    // Process packet here
};
```

### 5. Send packets

```csharp
byte[] data = new byte[] { 0x01, 0x02, 0x03 };
adapter.SendPacket(data);
```

### 6. End the session and dispose

```csharp
adapter.EndSession();
adapter.Dispose();
winTunService.Dispose();
```

---

## API Overview

### `IWinTunService`

```csharp
public interface IWinTunService : IDisposable
{
    IWinTunAdapter CreateAdapter(string adapterName);
}
```

### `IWinTunAdapter`

```csharp
public interface IWinTunAdapter : IDisposable
{
    void StartSession(uint? capacity = null);
    void EndSession();

    event EventHandler<byte[]>? PacketReceived;
    void SendPacket(byte[] data);
}
```

### Dependency Injection

```csharp
public static class DependencyInjection
{
    public static IServiceCollection AddWinTun(this IServiceCollection services)
    {
        services.AddSingleton<IWinTunService, WinTunService>();
        return services;
    }
}
```

---

## License

This project is licensed under the **MIT License**. See [LICENSE](LICENSE) for details.

---

## Notes

* Running as administrator may be required for certain network operations.
* Written and tested in **.NET 9**.
