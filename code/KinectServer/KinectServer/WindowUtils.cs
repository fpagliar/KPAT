
using System.Windows.Media;
using Microsoft.Kinect;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;
using System.Windows;

namespace KinectServer
{
    /// <summary>
    /// Credit to github project LightBuzz/Vitruvius, who provided this methods to show what the Kinect camera detects.
    /// </summary>
    static class WindowUtils
    {

        /// <summary>
        /// Kinect DPI.
        /// </summary>
        public static readonly double DPI = 96.0;

        /// <summary>
        /// Default format.
        /// </summary>
        public static readonly PixelFormat FORMAT = PixelFormats.Bgr32;

        /// <summary>
        /// Bytes per pixel.
        /// </summary>
        public static readonly int BYTES_PER_PIXEL = (FORMAT.BitsPerPixel + 7) / 8;

        #region Members

        /// <summary>
        /// The bitmap source.
        /// </summary>
        static WriteableBitmap _bitmap = null;

        /// <summary>
        /// Frame width.
        /// </summary>
        static int _width;

        /// <summary>
        /// Frame height.
        /// </summary>
        static int _height;

        /// <summary>
        /// The RGB pixel values.
        /// </summary>
        static byte[] _pixels = null;

        #endregion

        #region Public methods

        /// <summary>
        /// Converts a color frame to a System.Media.ImageSource.
        /// </summary>
        /// <param name="frame">A ColorImageFrame generated from a Kinect sensor.</param>
        /// <returns>The specified frame in a System.media.ImageSource format.</returns>
        public static WriteableBitmap ToBitmap(this ColorImageFrame frame)
        {
            if (_bitmap == null)
            {
                _width = frame.Width;
                _height = frame.Height;
                _pixels = new byte[_width * _height * BYTES_PER_PIXEL];
                _bitmap = new WriteableBitmap(_width, _height, DPI, DPI, FORMAT, null);
            }

            frame.CopyPixelDataTo(_pixels);
            _bitmap.Lock();

            Marshal.Copy(_pixels, 0, _bitmap.BackBuffer, _pixels.Length);
            _bitmap.AddDirtyRect(new Int32Rect(0, 0, _width, _height));

            _bitmap.Unlock();
            return _bitmap;
        }
        #endregion
    }
}