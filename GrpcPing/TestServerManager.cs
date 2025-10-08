using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GrpcPingWpf
{
    public sealed class TestServerManager : IDisposable
    {
        private CancellationTokenSource? _cts;
        private Task? _runTask;
        private WebApplication? _app;

        public bool IsRunning => _runTask is { IsCompleted: false };

        public async Task<(bool Ok, string? Error)> StartAsync(int port = 901, bool useTls = false)
        {
            if (IsRunning) return (true, null);

            try
            {
                var builder = WebApplication.CreateBuilder(new WebApplicationOptions
                {
                    Args = Array.Empty<string>(),
                    ContentRootPath = AppContext.BaseDirectory
                });

                builder.Services.AddGrpc();
                builder.Services.AddGrpcHealthChecks();

                builder.WebHost.ConfigureKestrel(o =>
                {
                    o.ListenLocalhost(port, lo =>
                    {
                        lo.Protocols = HttpProtocols.Http2;
                        // dev-cert; по умолчанию h2c без TLS
                        if (useTls)
                        {
                            lo.UseHttps();
                        }
                    });
                });

                var app = builder.Build();
                app.MapGrpcHealthChecksService();
                app.MapGet("/", () => "Test gRPC server is up.");

                _cts = new CancellationTokenSource();
                _app = app;
                _runTask = app.RunAsync(_cts.Token);

                // Время для старта Kestrelу
                await Task.Delay(300);
                return (true, null);
            }
            catch (Exception ex)
            {
                await StopAsync();
                return (false, ex.Message);
            }
        }

        public async Task StopAsync()
        {
            try
            {
                _cts?.Cancel();

                if (_app != null)
                {
                    try
                    {
                        // Остановка Kestrel
                        await _app.StopAsync(TimeSpan.FromSeconds(1));
                    }
                    catch { /* ignore */ }

                    // Освобождение ресурсов (явные реализации интерфейсов)
                    if (_app is IAsyncDisposable asyncDisp)
                    {
                        await asyncDisp.DisposeAsync();
                    }
                    else if (_app is IDisposable disp)
                    {
                        disp.Dispose();
                    }
                }

                if (_runTask != null)
                {
                    await Task.WhenAny(_runTask, Task.Delay(1000));
                }
            }
            finally
            {
                _runTask = null;
                _cts?.Dispose();
                _cts = null;
                _app = null;
            }
        }

        public void Dispose()
        {
            _ = StopAsync();
        }
    }
}
