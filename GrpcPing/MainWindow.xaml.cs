using System.Windows;

namespace GrpcPingWpf
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }

        private async void OnConnectClick(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
                await vm.CheckAsync();
        }

        private async void OnToggleTestServerClick(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
                await vm.ToggleTestServerAsync();
        }

        private async void Window_Closed(object sender, System.EventArgs e)
        {
            if (DataContext is MainViewModel vm)
                await vm.StopTestServerAsync();
        }
    }
}