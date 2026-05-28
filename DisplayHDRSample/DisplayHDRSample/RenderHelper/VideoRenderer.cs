using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using System;
using System.Diagnostics;
using System.Numerics;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace DisplayHDRSample.RenderHelper
{
    public class VideoRenderer : IDisposable
    {
        private CanvasDevice? _device;
        private CanvasSwapChainPanel? _swapChainPanel;
        private CanvasSwapChain? _swapChain;
        private MediaPlayer? _mediaPlayer;

        private CanvasBitmap? _frameTarget;
        private bool _frameReady;

        private int _videoWidth;
        private int _videoHeight;

        private bool _isRendering;
        private DispatcherTimer? _renderTimer;
        private readonly object _frameLock = new object();

        public void Initialize(CanvasSwapChainPanel swapChainPanel)
        {
            _swapChainPanel = swapChainPanel;
            _device = CanvasDevice.GetSharedDevice();

            // Create swap chain with HDR pixel format (FP16 for HDR values)
            var format = HdrHelper.GetOptimalPixelFormat(isHdr: true);
            var width = Math.Max(1, (float)swapChainPanel.ActualWidth);
            var height = Math.Max(1, (float)swapChainPanel.ActualHeight);

            _swapChain = new CanvasSwapChain(_device, width, height, 96, format, 2, CanvasAlphaMode.Premultiplied);

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
            _renderTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) }; // ~60 FPS
            _renderTimer.Tick += OnRenderTick;
            _renderTimer.Start();
        }

        private void OnRenderTick(object sender, object e)
        {
            RenderFrame();
        }

        private void RenderFrame()
        {
            if (_swapChain == null || _device == null || _swapChainPanel == null) return;

            try
            {
                using var ds = _swapChain.CreateDrawingSession(Colors.Transparent);

                CanvasBitmap? frameToRender = null;
                bool hasFrame;

                lock (_frameLock)
                {
                    hasFrame = _frameReady && _frameTarget != null;
                    if (hasFrame)
                    {
                        frameToRender = _frameTarget;
                    }
                }

                if (!hasFrame || frameToRender == null)
                {
                    // ...
                    return;
                }

                // Calculate scale to fit video in the panel wihile maintaining aspect ratio
                var panelWidth = (float)_swapChainPanel.ActualWidth;
                var panelHeight = (float)_swapChainPanel.ActualHeight;
                var scaleX = panelWidth / _videoWidth;
                var scaleY = panelHeight / _videoHeight;
                var scale = Math.Min(scaleX, scaleY);

                // Centure the video
                var offsetX = (panelWidth - _videoWidth * scale) / 2;
                var offsetY = (panelHeight - _videoHeight * scale) / 2;
                
                ds.Transform = Matrix3x2.CreateScale(scale, scale) * Matrix3x2.CreateTranslation(offsetX, offsetY);
                ds.DrawImage(frameToRender);

                // Reset transform for info overlay
                ds.Transform = Matrix3x2.Identity;

                _swapChain.Present();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during rendering: {ex.Message}");
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
            StopVideo();

            try
            {
                _mediaPlayer = new MediaPlayer();
                _mediaPlayer.IsVideoFrameServerEnabled = true;

                var source = MediaSource.CreateFromStorageFile(file);
                var item = new MediaPlaybackItem(source);

                _mediaPlayer.Source = item;
                _mediaPlayer.AutoPlay = false;

                // Subscribe to video frame available event
                _mediaPlayer.VideoFrameAvailable += OnVideoFrameAvailable;

                // Get video dimensions
                _mediaPlayer.MediaOpened += (s, e) =>
                {
                    try
                    {
                        var playbackItem = s.Source as MediaPlaybackItem;
                        if (playbackItem != null)
                        {
                            var tracks = playbackItem.VideoTracks;
                            if (tracks.Count > 0)
                            {
                                var encoding = tracks[0].GetEncodingProperties();
                                _videoHeight = (int)encoding.Height;
                                _videoWidth = (int)encoding.Width;

                                // Create frame target with video dimensions and HDR format
                                EnsureFrameTarger();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error getting video dimensions: {ex.Message}");
                    }
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading video file: {ex.Message}");
            }
        }

        private void EnsureFrameTarger()
        {
            if (_device == null || _videoWidth <= 0 || _videoHeight <= 0) return;

            // Create render target to match video dimensions and HDR format
            if (_frameTarget == null || _frameTarget.Size.Width != _videoWidth || _frameTarget.Size.Height != _videoHeight)
            {
                _frameTarget?.Dispose();
                _frameTarget = new CanvasRenderTarget(_device, _videoWidth, _videoHeight, 96, HdrHelper.GetOptimalPixelFormat(isHdr: true), CanvasAlphaMode.Premultiplied);
            }
        }

        public void Play()
        {
            if (_mediaPlayer != null)
            {
                _mediaPlayer.Play();
            }
        }

        public void Pause()
        {
            if ( _mediaPlayer != null)
            {
                _mediaPlayer.Pause();
            }
        }

        public void StopVideo()
        {
            if (_mediaPlayer != null)
            {
                _mediaPlayer.Pause();
                _mediaPlayer.VideoFrameAvailable -= OnVideoFrameAvailable;
                _mediaPlayer.Source = null;
                _mediaPlayer = null;
            }
            lock (_frameLock)
            {
                _frameTarget?.Dispose();
                _frameTarget = null;
                _frameReady = false;
            }
        }

        private void OnVideoFrameAvailable(MediaPlayer sender, object args)
        {
            lock (_frameLock)
            {
                try
                {
                    if (_frameTarget == null) return;
                    sender.CopyFrameToVideoSurface(_frameTarget);
                    _frameReady = true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error occurred while copying video frame: {ex.Message}");
                }
            }
        }

        public void Dispose()
        {
            StopRenderLoop();
            StopVideo();

            _swapChain?.Dispose();
            _swapChain = null;
            _device = null;
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
    }
}
