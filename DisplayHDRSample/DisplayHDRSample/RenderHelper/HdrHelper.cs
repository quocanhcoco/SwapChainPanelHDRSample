using Microsoft.Graphics.Canvas;
using Windows.Graphics.DirectX;

namespace DisplayHDRSample.RenderHelper
{
    public static class HdrHelper
    {
        public static bool IsHdrDisplayAvailable()
        {
            return true;    // test -> Need to check later
        }

        public static DirectXPixelFormat GetOptimalPixelFormat(bool isHdr)
        {
            return isHdr ? DirectXPixelFormat.R16G16B16A16Float : DirectXPixelFormat.B8G8R8A8UIntNormalized;
        }

        /// <summary>
        /// Use native DXGI SwapChain3::SetColorSpace1 to set the color space.
        /// DXGI_COLOR_SPACE_RGB_FULL_G2084_NONE_P2020 = 24 (HDR/BT.2020/PQ) is used for HDR
        /// DXGI_COLOR_SPACE_RGB_FULL_G22_NONE_P709 = 0 (SDR/BT.709/sRGB) is used for SDR.
        /// </summary>
        public static void ConfigureHdrSwapChain(CanvasSwapChain swapChain, bool isHdr)
        {
            if (swapChain == null) return;

            try
            {

            }
            catch (System.Exception)
            {

                throw;
            }
        }
    }
}
