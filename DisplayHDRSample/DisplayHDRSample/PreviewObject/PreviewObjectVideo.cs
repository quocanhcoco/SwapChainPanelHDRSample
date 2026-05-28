using DisplayHDRSample.RenderHelper;
using Microsoft.Graphics.Canvas;
using System;
using System.Diagnostics;
using Windows.Foundation;
using Windows.Media;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Streams;

namespace DisplayHDRSample.PreviewObject
{
    public class PreviewObjectVideo : IDisposable
    {
        private CanvasDevice? _device;

        private MediaPlayer? _mediaPlayer;
        private MediaTimelineController? _mediaTimelineController;

        public IBuffer LastFrameBytes { get; private set; } = null;
        private CanvasRenderTarget? _frameTarget;
        private bool _frameReady;

        private Rect? _sourceRect;
        private Rect? _destinationRect;

        private int _videoWidth;
        private int _videoHeight;

        public PreviewObjectVideo(StorageFile file, MediaTimelineController mediaTimelineController, CanvasDevice canvasDevice)
        {
            _device = canvasDevice;
            _mediaTimelineController = mediaTimelineController;
            LoadVideoFile(file);
        }


        public void LoadVideoFile(StorageFile file)
        {
            try
            {
                _mediaPlayer = new MediaPlayer();
                _mediaPlayer.IsVideoFrameServerEnabled = true;
                _mediaPlayer.TimelineController = _mediaTimelineController;

                var source = MediaSource.CreateFromStorageFile(file);
                var item = new MediaPlaybackItem(source);

                _mediaPlayer.Source = item;

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

        private void OnVideoFrameAvailable(MediaPlayer sender, object args)
        {
            try
            {
                if (_frameTarget == null) return;

                using (CanvasLock lockDevice = _device.Lock())
                {
                    sender.CopyFrameToVideoSurface(_frameTarget);
                    //_frameTarget?.GetPixelBytes(LastFrameBytes);
                    _frameReady = true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error occurred while copying video frame: {ex.Message}");
            }
        }

        private void EnsureFrameTarger()
        {
            if (_device == null || _videoWidth <= 0 || _videoHeight <= 0) return;

            // Create render target to match video dimensions and HDR format
            if (_frameTarget == null || _frameTarget.Size.Width != _videoWidth || _frameTarget.Size.Height != _videoHeight)
            {
                _frameTarget?.Dispose();
                _sourceRect = new Rect(0, 0, _videoWidth, _videoHeight);
                _frameTarget = new CanvasRenderTarget(_device, _videoWidth, _videoHeight, 96, HdrHelper.GetOptimalPixelFormat(isHdr: true), CanvasAlphaMode.Premultiplied);
            }
        }

        public CanvasRenderTarget GetCurrentFrame()
        {
            return _frameTarget;
        }

        public IBuffer GetLastFrameBytes()
        {
            return LastFrameBytes;
        }

        public Rect GetSourceRect()
        {
            return _sourceRect ?? new Rect(0, 0, _videoWidth, _videoHeight);
        }

        public Rect GetDestinationRect()
        {
            return _destinationRect ?? new Rect(0, 0, _videoWidth, _videoHeight);
        }

        public void UpdateDestinationRect(Rect rect)
        {
            _destinationRect = rect;
        }

        public void Dispose()
        {
            _device = null;
        }
    }
}
