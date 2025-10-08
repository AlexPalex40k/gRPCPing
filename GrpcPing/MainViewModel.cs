using Grpc.Core;
using Grpc.Health.V1;
using Grpc.Net.Client;
using System.ComponentModel;
using System.Net.Http;
using System.Net.Security;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace GrpcPingWpf
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private string _address = "127.0.0.1";
        private string _port = "9010";
        private string _statusText = "Idle.";
        private string _lastError = string.Empty;
        private Brush _statusBrush = Brushes.Gray;

        private readonly TestServerManager _testServer = new();
        private string _testServerStatusText = "Test server: STOPPED.";
        private int _runningTestServerPort;
        private bool _isServerBusy;
        private bool _isBusy;
        private bool _useTls;

        public string Address
        {
            get => _address;
            set
            {
                _address = value; OnPropertyChanged();
            }
        }

        public string Port
        {
            get => _port;
            set
            {
                _port = value; 
                OnPropertyChanged();
            }
        }

        public bool UseTls
        {
            get => _useTls;
            set
            {
                _useTls = value; 
                OnPropertyChanged();
            }
        }

        public string StatusText
        {
            get => _statusText;
            set
            {
                _statusText = value; 
                OnPropertyChanged();
            }
        }

        public string LastError
        {
            get => _lastError;
            set
            {
                _lastError = value; 
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasError));
            }
        }
        public bool HasError => !string.IsNullOrWhiteSpace(_lastError);

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                _isBusy = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(ConnectButtonText));
            }
        }

        public Brush StatusBrush
        {
            get => _statusBrush; 
            set 
            { 
                _statusBrush = value; 
                OnPropertyChanged();
            }
        }

        public bool IsServerBusy
        {
            get => _isServerBusy; 
            set 
            {
                _isServerBusy = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(StartServerButtonText));
            }
        }

        public string TestServerStatusText
        {
            get => _testServerStatusText; 
            set 
            { 
                _testServerStatusText = value; 
                OnPropertyChanged(); }
        }

        public string ConnectButtonText => IsBusy ? "Checking..." : "Check connection";
        public string StartServerButtonText => _testServer.IsRunning ? $"Stop test server ({_runningTestServerPort})" : $"Start test server ({Port})";

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? prop = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));

        public async Task ToggleTestServerAsync()
        {
            if (IsServerBusy) return;
            IsServerBusy = true;
            LastError = string.Empty;

            try
            {
                if (!_testServer.IsRunning)
                {
                    if (!int.TryParse(Port, out var port) || port <= 0 || port > 65535)
                    {
                        LastError = "Invalid port.";
                        return;
                    }

                    // Поднимаем тест-сервер на ТЕКУЩЕМ порту (например, 9010).
                    // Если другое ПО уже слушает этот порт — будет ошибка binding.
                    var (ok, err) = await _testServer.StartAsync(port, useTls: false);
                    if (ok)
                    {
                        _runningTestServerPort = port;
                        TestServerStatusText = $"Test server: RUNNING on 127.0.0.1:{port}.";
                        Address = "127.0.0.1";
                        UseTls = false;
                    }
                    else
                    {
                        TestServerStatusText = "Test server: FAILED to start.";
                        LastError = err ?? "Unknown error (maybe the port is already in use).";
                    }
                }
                else
                {
                    await _testServer.StopAsync();
                    _runningTestServerPort = 0;
                    TestServerStatusText = "Test server: STOPPED.";
                }
            }
            finally
            {
                IsServerBusy = false;
                OnPropertyChanged(nameof(StartServerButtonText));
            }
        }

        public async Task CheckAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            LastError = string.Empty;
            StatusText = "Connecting...";
            StatusBrush = Brushes.SlateGray;

            try
            {
                var scheme = UseTls ? "https" : "http";
                var uri = $"{scheme}://{Address}:{Port}";

                using var handler = new SocketsHttpHandler
                {
                    EnableMultipleHttp2Connections = true,
                    PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
                    KeepAlivePingDelay = TimeSpan.FromSeconds(20),
                    KeepAlivePingTimeout = TimeSpan.FromSeconds(10),
                    KeepAlivePingPolicy = HttpKeepAlivePingPolicy.Always
                };

                if (UseTls)
                {
                    // Для локальной отладки можно ослабить проверку сертификата (НЕ ДЕЛАТЬ ТАК в основных проектах)
                    handler.SslOptions = new SslClientAuthenticationOptions
                    {
                        RemoteCertificateValidationCallback = (sender, cert, chain, errors) => true
                    };
                }

                using var httpClient = new HttpClient(handler);
                using var channel = GrpcChannel.ForAddress(uri, new GrpcChannelOptions { HttpClient = httpClient });

                // Пытаемся вызвать стандартный health-check
                var client = new Health.HealthClient(channel);

                // Короткий дедлайн, чтобы быстро узнать статус
                var deadline = DateTime.UtcNow.AddSeconds(2);

                HealthCheckResponse response;
                try
                {
                    response = await client.CheckAsync(new HealthCheckRequest { Service = "" }, deadline: deadline);
                }
                catch (RpcException ex) when (ex.StatusCode == StatusCode.Unimplemented)
                {
                    // Сервер доступен, но Health не реализован: это всё равно доказывает подключение
                    StatusText = "Connected (Health service is not implemented).";
                    StatusBrush = Brushes.ForestGreen;

                    return;
                }

                if (response.Status == HealthCheckResponse.Types.ServingStatus.Serving)
                {
                    StatusText = "Connected (SERVING).";
                    StatusBrush = Brushes.ForestGreen;
                }
                else
                {
                    StatusText = $"Connected, status: {response.Status}.";
                    StatusBrush = Brushes.ForestGreen;
                }
            }
            catch (RpcException ex)
            {
                // Если к порту никто не подключен, то будет RpcException
                StatusText = "Not connected. Caught RpcException";
                StatusBrush = Brushes.IndianRed;
                LastError = $"{ex.StatusCode}: {ex.Status.Detail}";
            }
            catch (Exception ex)
            {
                StatusText = "Not connected.";
                StatusBrush = Brushes.IndianRed;
                LastError = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task StopTestServerAsync()
        {
            if (_testServer.IsRunning)
            {
                await _testServer.StopAsync();
                TestServerStatusText = "Test server: STOPPED.";
                OnPropertyChanged(nameof(StartServerButtonText));
            }
        }
    }
}
