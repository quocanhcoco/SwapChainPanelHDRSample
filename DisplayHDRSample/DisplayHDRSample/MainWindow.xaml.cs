using DisplayHDRSample.RenderHelper;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Xaml;
using System;
using System.Diagnostics;

namespace DisplayHDRSample
{
    public sealed partial class MainWindow : Window
    {
        private VideoRenderer? _renderer;
        private DispatcherTimer _timer;

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

            CanvasControlPreview.CustomDevice = _renderer.GetCanvasDevice();
            InitTimer();
        }

        private void InitTimer()
        {
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
            _timer.Tick += OnTimerTick;
            _timer.Start();
        }

        private void OnTimerTick(object sender, object e)
        {
            CanvasControlPreview.Invalidate();
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

        private void CanvasControlPreview_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            var frameTarget = _renderer?.GetFrameTarget();
            if (frameTarget != null)
            {
                //Debug.WriteLine(frameTarget.Format.ToString());
                args.DrawingSession.DrawImage(frameTarget);
            }
        }
    }
}
