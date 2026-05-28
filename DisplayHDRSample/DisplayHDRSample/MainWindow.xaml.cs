using DisplayHDRSample.RenderHelper;
using Microsoft.UI.Xaml;

namespace DisplayHDRSample
{
    public sealed partial class MainWindow : Window
    {
        private VideoRenderer? _renderer;

        public MainWindow()
        {
            InitializeComponent();

            ((FrameworkElement)this.Content).Loaded += OnContentLoaded;
            Closed += MainWindow_Closed;
        }

        private void OnContentLoaded(object sender, RoutedEventArgs e)
        {
            _renderer = new VideoRenderer();
            _renderer.Initialize(VideoPanel);
        }

        private void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            _renderer?.Dispose();
        }

        private void BtnOpenVideo1_Click(object sender, RoutedEventArgs e)
        {
            _renderer?.OpenVideo();
        }

        private void BtnPlay_Click(object sender, RoutedEventArgs e)
        {
            _renderer?.Play();
        }
    }
}
