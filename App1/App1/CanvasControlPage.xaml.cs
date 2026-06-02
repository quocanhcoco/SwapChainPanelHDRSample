using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using Windows.Foundation;

namespace App1
{
    public sealed partial class CanvasControlPage : Page
    {
        private const int FPS = 30;

        private int _myPositionCount = 0;
        CanvasDevice _myCanvasDevice = new CanvasDevice();
        CanvasBitmap _myCanvasBitmap = null;

        DispatcherTimer _myTimer = new DispatcherTimer();

        public CanvasControlPage()
        {
            InitializeComponent();

            InitTimer();
            InitSample();
        }

        private void InitTimer()
        {
            _myTimer.Interval = TimeSpan.FromMilliseconds(FPS);
            _myTimer.Tick += Timer_Tick;
            _myTimer.Start();
        }

        private void InitSample()
        {
            myCanvas.CustomDevice = _myCanvasDevice;
        }

        private void Timer_Tick(object sender, object e)
        {
            if (myCanvas == null)
                return;

            _myPositionCount += 1;
            if (_myPositionCount > 100)
            {
                _myPositionCount = 0;
            }

            myCanvas.Invalidate();
        }

        private void myCanvas_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            //DrawTextLineCircle(sender, args);
            //DrawImageByCanvasBitmap(sender, args);
            //DrawImageWithEffect(sender, args);
            //DrawOutScreen(sender, args);
        }

        private void DrawTextLineCircle(CanvasControl sender, CanvasDrawEventArgs args)
        {
            args.DrawingSession.DrawText($"Hello, World!    {_myPositionCount}", 100 + _myPositionCount, 100, Colors.Black);
            args.DrawingSession.DrawLine(100, 100, 200 + _myPositionCount, 200 + _myPositionCount, Colors.Red);
            args.DrawingSession.DrawCircle(300, 300 + _myPositionCount, 50, Colors.Blue);
        }

        private void DrawImageByCanvasBitmap(CanvasControl sender, CanvasDrawEventArgs args)
        {
            if (_myCanvasBitmap != null)
            {
                myCanvas.Invalidate();
                using (CanvasDrawingSession ds = args.DrawingSession)
                {
                    //ds.Clear(Colors.Transparent); --> No need here, because the background of CanvasControl is transparent by default.
                    ds.DrawImage(_myCanvasBitmap);
                }
            }
        }

        private void DrawImageWithEffect(CanvasControl sender, CanvasDrawEventArgs args)
        {
            if (_myCanvasBitmap != null)
            {
                using (CanvasDrawingSession ds = args.DrawingSession)
                {
                    GaussianBlurEffect gaussianBlurEffect = new GaussianBlurEffect();
                    gaussianBlurEffect.Source = _myCanvasBitmap;
                    gaussianBlurEffect.BlurAmount = 5;
                    ds.DrawImage(gaussianBlurEffect);
                }
            }
        }

        private void DrawOutScreen(CanvasControl sender, CanvasDrawEventArgs args)
        {
            if (_myCanvasBitmap != null)
            {
                using (CanvasRenderTarget canvasRenderTarget = new CanvasRenderTarget(_myCanvasDevice, (float)_myCanvasBitmap.Size.Width, (float)_myCanvasBitmap.Size.Height, 96f))
                {
                    using (CanvasDrawingSession ds = canvasRenderTarget.CreateDrawingSession())
                    {
                        ds.Clear(Colors.Transparent);
                        ds.DrawImage(_myCanvasBitmap);

                        Rect sourceRect = new Rect(0, 0, _myCanvasBitmap.Size.Width, _myCanvasBitmap.Size.Height);
                        Rect destRect = new Rect(100 + _myPositionCount, 100 + _myPositionCount, myCanvas.ActualWidth / 2, myCanvas.ActualHeight / 2);
                        args.DrawingSession.DrawImage(canvasRenderTarget, destRect, sourceRect);
                    }
                }
            }
        }

        private async void myCanvas_CreateResources(CanvasControl sender, CanvasCreateResourcesEventArgs args)
        {
            await LoadImage("ms-appx:///Assets/Image1.png");
        }

        private async void LoadImageButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadImage("ms-appx:///Assets/Image2.png");
        }

        private async Task LoadImage(string path)
        {
            var file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(new Uri(path));
            using (var stream = await file.OpenReadAsync())
            {
                _myCanvasBitmap = await CanvasBitmap.LoadAsync(_myCanvasDevice, stream);
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            // Avoid memory leak by removing the CanvasControl from the visual tree and setting it to null.
            this.myCanvas.RemoveFromVisualTree();
            this.myCanvas = null;
        }
    }
}
