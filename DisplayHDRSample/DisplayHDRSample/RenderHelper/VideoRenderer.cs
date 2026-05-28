using DisplayHDRSample.PreviewObject;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Media;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;

namespace DisplayHDRSample.RenderHelper
{
    public class VideoRenderer : IDisposable
    {
        private List<PreviewObjectVideo> _previewObjects;
        private CanvasDevice? _devicePreview;
        private CanvasSwapChainPanel? _swapChainPanel;
        private CanvasSwapChain? _swapChain;

        private MediaPlayer? _mediaPlayer;
        private MediaTimelineController? _mediaTimelineController;

        private CanvasRenderTarget? _frameTarget;
        private bool _frameReady;

        private bool _isRendering;
        private DispatcherTimer? _renderTimer;
        private readonly object _frameLock = new object();

        public void Initialize(CanvasSwapChainPanel swapChainPanel)
        {
            _swapChainPanel = swapChainPanel;
            _devicePreview = new CanvasDevice();

            _mediaTimelineController = new MediaTimelineController();
            _previewObjects = new List<PreviewObjectVideo>();

            // Create swap chain with HDR pixel format (FP16 for HDR values)
            var format = HdrHelper.GetOptimalPixelFormat(isHdr: true);
            var width = Math.Max(1, (float)swapChainPanel.ActualWidth);
            var height = Math.Max(1, (float)swapChainPanel.ActualHeight);

            _swapChain = new CanvasSwapChain(_devicePreview, width, height, 96, format, 2, CanvasAlphaMode.Premultiplied);

            // Configure HDR color space on the swap chain
            HdrHelper.ConfigureHdrSwapChain(_swapChain, isHdr: true);

            _swapChainPanel.SwapChain = _swapChain;

            // Resize
            _swapChainPanel.SizeChanged += OnSwapChainPanelSizeChanged;

            // Start render loop
            StartRenderLoop();
        }

        private void OnSwapChainPanelSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_swapChain != null && e.NewSize.Width > 0 && e.NewSize.Height > 0)
            {
                _swapChain.ResizeBuffers((float)e.NewSize.Width, (float)e.NewSize.Height);
            }
        }

        private void StartRenderLoop()
        {
            if (_isRendering) return;

            _isRendering = true;
            _renderTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) }; // ~60 FPS
            _renderTimer.Tick += OnRenderTick;
            _renderTimer.Start();
        }

        private void OnRenderTick(object sender, object e)
        {
            RenderFrame();
        }

        private void RenderFrame()
        {
            if (_swapChain == null || _devicePreview == null || _swapChainPanel == null) return;

            try
            {
                using var ds = _swapChain.CreateDrawingSession(Colors.Transparent);

                foreach (var previewObject in _previewObjects)
                {
                    DrawFrame(ds, previewObject);
                }


                //CanvasRenderTarget? frameToRender = null;
                //bool hasFrame;

                //lock (_frameLock)
                //{
                //    hasFrame = _frameReady && _frameTarget != null;
                //    if (hasFrame)
                //    {
                //        //CanvasBitmap? frameToRender = null;
                //        //byte[] pixelData = _frameTarget.GetPixelBytes();    // GPU -> CPU -> GPU
                //        //frameToRender = CanvasBitmap.CreateFromBytes(_device, pixelData, _videoWidth, _videoHeight, _frameTarget.Format);
                //        frameToRender = _frameTarget;
                //    }
                //}

                //if (!hasFrame || frameToRender == null)
                //{
                //    // ...
                //    return;
                //}

                //// Calculate scale to fit video in the panel wihile maintaining aspect ratio
                //var panelWidth = (float)_swapChainPanel.ActualWidth;
                //var panelHeight = (float)_swapChainPanel.ActualHeight;
                //var scaleX = panelWidth / _videoWidth;
                //var scaleY = panelHeight / _videoHeight;
                //var scale = Math.Min(scaleX, scaleY);

                //// Centure the video
                //var offsetX = (panelWidth - _videoWidth * scale) / 2;
                //var offsetY = (panelHeight - _videoHeight * scale) / 2;
                
                //ds.Transform = Matrix3x2.CreateScale(scale, scale) * Matrix3x2.CreateTranslation(offsetX, offsetY);
                //ds.DrawImage(frameToRender);

                //// Reset transform for info overlay
                //ds.Transform = Matrix3x2.Identity;

                _swapChain.Present();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during rendering: {ex.Message}");
            }
        }

        private void DrawFrame(CanvasDrawingSession ds, PreviewObjectVideo previewObject)
        {
            if (previewObject == null || ds == null) return;

            lock (_frameLock)
            {
                //if (previewObject.GetLastFrameBytes() is IBuffer bytesToDraw)
                //{
                //    int width = (int)previewObject.GetSourceRect().Width;
                //    int height = (int)previewObject.GetSourceRect().Height;
                //    using (CanvasBitmap frame = CanvasBitmap.CreateFromBytes(_devicePreview, bytesToDraw, width, height, HdrHelper.GetOptimalPixelFormat(isHdr: true)))
                //    {
                //        Rect destinationRect = previewObject.GetDestinationRect();
                //        Rect sourceRect = previewObject.GetSourceRect();

                //        ds.DrawImage(frame, destinationRect, sourceRect);
                //    }
                //}

                var videoFrame = previewObject.GetCurrentFrame();

                Rect destinationRect = previewObject.GetDestinationRect();
                Rect sourceRect = previewObject.GetSourceRect();

                if (videoFrame != null)
                {
                    ds.DrawImage(videoFrame, destinationRect, sourceRect);
                }
            }
        }


        public async Task OpenVideo()
        {
            try
            {
                var picker = new FileOpenPicker();
                picker.SuggestedStartLocation = PickerLocationId.VideosLibrary;
                picker.FileTypeFilter.Add(".mp4");

                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

                var file = await picker.PickSingleFileAsync();
                if (file == null) return;

                LoadVideoFile(file);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error opening video file: {ex.Message}");
            }
        }

        private void LoadVideoFile(StorageFile file)
        {
            if (_previewObjects == null || file == null || _mediaTimelineController == null) return;
            PreviewObjectVideo videoObject = new PreviewObjectVideo(file , _mediaTimelineController, _devicePreview);
            _previewObjects.Add(videoObject);

            UpdatePreviewObjectsPosion();
        }

        private void UpdatePreviewObjectsPosion()
        {
            if (_previewObjects == null || _previewObjects.Count == 0 || _swapChain == null) return;

            // Simple layout: horizontal row of videos at the top of the panel
            var swapChainSize = _swapChain.Size;
            var videoWidth = swapChainSize.Width / _previewObjects.Count;
            var videoHeight = swapChainSize.Height / _previewObjects.Count;

            for (int i = 0; i < _previewObjects.Count; i++)
            {
                _previewObjects[i].UpdateDestinationRect(new Rect(i * videoWidth, 0, videoWidth, videoHeight));
            }
        }

        public void Play()
        {
            if (_mediaTimelineController != null)
            {
                if (_mediaTimelineController.State == MediaTimelineControllerState.Paused)
                {
                    _mediaTimelineController.Resume();
                }
                else
                {
                    _mediaTimelineController.Start();
                }
            }
        }

        public void Pause()
        {
            if (_mediaTimelineController != null)
            {
                _mediaTimelineController.Pause();
            }
        }

        public void StopVideo()
        {
            if (_mediaTimelineController != null)
            {
                _mediaTimelineController.Pause();
                _mediaTimelineController = null;
            }
            lock (_frameLock)
            {
                _frameTarget?.Dispose();
                _frameTarget = null;
                _frameReady = false;
            }
        }

        private void StopRenderLoop()
        {
            if (!_isRendering) return;
            _isRendering = false;
            if (_renderTimer != null)
            {
                _renderTimer.Stop();
                _renderTimer.Tick -= OnRenderTick;
                _renderTimer = null;
            }
        }

        public void Dispose()
        {
            StopRenderLoop();
            StopVideo();

            _swapChain?.Dispose();
            _swapChain = null;
            _devicePreview = null;
        }
    }
}
