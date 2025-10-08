using System.Windows;

namespace GrpcPingWpf
{
    public partial class App : Application
    {
        public App()
        {
            // Разрешает HTTP/2 без TLS для локального подключения (http://)
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
        }
    }
}