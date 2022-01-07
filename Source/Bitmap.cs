using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Runtime.Serialization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ImageProcess
{
    #region Useful C# bitmap graphics library, quick and easy to use, not tested with NET Core

    // EXAMPLE:
    //
    // using (BitmapWin bitmap = new BitmapWin(@"C:\Temp\test.png"))
    // using (FileStream file = new FileStream(@"C:\Temp\output.jpg", FileMode.Create))
    // {
    //     bitmap.SetResampleMode(ResampleMode.BiCubic);
    //     bitmap.Resize(50);
    //     bitmap.Rotate180();
    //     bitmap.Grayscale();
    //
    //     byte[] data = bitmap.EncodeJpeg(80);
    //     file.Write(data, 0, data.Length);
    //     file.Close();
    // }

    #region Object declarations

    /// <summary>
    /// GeneralLib.BitmapException class.
    /// </summary>
    [Serializable]
    public class BitmapException : Exception
    {
        private static string _baseMessage = "BitmapException";

        public BitmapException()
        { }

        public BitmapException(string message)
            : base(_baseMessage + ": " + message)
        { }

        public BitmapException(string message, Exception innerException)
            : base(_baseMessage + ": " + message, innerException)
        { }

        protected BitmapException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
    }

    public enum ResampleMode
    {
        Nearest = 0,
        BiLinear,
        BiCubic
    };

    public interface IBitmapBase : IDisposable
    {
        Bitmap Image { get; set; }
        ImageFormat ImageFormat { get; }
        System.Drawing.Imaging.PixelFormat PixelFormat { get; }
        float BytesPerPixel { get; }
        int OrigWidth  { get; }
        int OrigHeight { get; }
        int NewWidth   { get; }
        int NewHeight  { get; }

        void SetResampleMode(ResampleMode mode);
        void Create(int width, int height);
        void Create(int width, int height, System.Drawing.Imaging.PixelFormat format);
        void Reset();
        void Load(string file);
        void Load(MemoryStream data);
        void Load(Image data);
        void Save(string file);
        void Save(MemoryStream data);
        void Save(MemoryStream data, string format);
        void Save(MemoryStream data, ImageFormat format);
        void Resize(float percent);
        void Resize(int width, int height);
        void Crop(int x, int y, int width, int height);
        void Grayscale();
        void Grayscale_8Bpp();
        void Rotate90();
        void Rotate180();
        void Rotate270();
        void FlipVertical();
        void FlipHorizontal();
        void Rotate(float angle);
        void Brightness(float percent);
        void Contrast(float percent);
        void Reduce_1Bpp(float percent);

        // Really handy image encoding routines as part of the class
        byte[] ImageData(out int stride);
        byte[] EncodeJpeg(int quality);
        byte[] EncodePng();
        byte[] EncodeBmp();
        byte[] EncodeGif();
        byte[] EncodeTiff();
    }

    #endregion

    public class BitmapWin : IBitmapBase
    {
        #region General property declarations for the library

        private InterpolationMode _mode = InterpolationMode.NearestNeighbor;
        private Bitmap _orig;
        private Bitmap _tran;
        private ImageFormat _fmt = ImageFormat.MemoryBmp;
        private int rgbThresholdValue = 80; // Default percentage level for 1bpp operations

        public Bitmap Image
        {
            get => _tran;
            set
            {
                _tran = value;
                FindFormat(_tran);
            }
        }

        public ImageFormat ImageFormat
            => (_tran != null ? _fmt : ImageFormat.MemoryBmp);

        public System.Drawing.Imaging.PixelFormat PixelFormat
            => (_tran?.PixelFormat ?? System.Drawing.Imaging.PixelFormat.DontCare);

        public float BytesPerPixel
            => (_tran != null ? GetBytesPerPixel(_tran) : 0f);

        public int OrigWidth  => (_orig?.Width ?? 0);
        public int OrigHeight => (_orig?.Height ?? 0);
        public int NewWidth   => (_tran?.Width ?? 0);
        public int NewHeight  => (_tran?.Height ?? 0);

        #endregion

        #region Constructors, destructors and memory management routines

        // MEMORY -------------------------------------------------------------------------------------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BitmapWin()
        {
            try
            {
                SetupResampleMode(ResampleMode.Nearest);
            }
            catch (Exception ex)
            {
                throw new BitmapException("BitmapWin()", ex);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BitmapWin(string file)
        {
            try
            {
                SetupResampleMode(ResampleMode.Nearest);
                _orig = new Bitmap(file);
                _tran = new Bitmap(file);
                _fmt = FindFormat(file);
            }
            catch (Exception ex)
            {
                throw new BitmapException("BitmapWin(file)", ex);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BitmapWin(MemoryStream data)
        {
            try
            {
                SetupResampleMode(ResampleMode.Nearest);
                _orig = new Bitmap(data);
                _tran = new Bitmap(data);
                _fmt = FindFormat(data);
            }
            catch (Exception ex)
            {
                throw new BitmapException("BitmapWin(data)", ex);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BitmapWin(Image image)
        {
            try
            {
                SetupResampleMode(ResampleMode.Nearest);
                _orig = new Bitmap(image);
                _tran = new Bitmap(image);
                _fmt = FindFormat((Bitmap)image);
            }
            catch (Exception ex)
            {
                throw new BitmapException("BitmapWin(image)", ex);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BitmapWin(int width, int height)
        {
            try
            {
                SetupResampleMode(ResampleMode.Nearest);
                _orig = new Bitmap(width, height);
                _tran = new Bitmap(width, height);
                _fmt = ImageFormat.MemoryBmp;
            }
            catch (Exception ex)
            {
                throw new BitmapException("BitmapWin(width,height)", ex);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BitmapWin(int width, int height, System.Drawing.Imaging.PixelFormat format)
        {
            try
            {
                SetupResampleMode(ResampleMode.Nearest);
                _orig = new Bitmap(width, height, format);
                _tran = new Bitmap(width, height, format);
                _fmt = ImageFormat.MemoryBmp;
            }
            catch (Exception ex)
            {
                throw new BitmapException("BitmapWin(width,height,format)", ex);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        ~BitmapWin()
        {
            _orig?.Dispose();
            _tran?.Dispose();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            _orig?.Dispose();
            _tran?.Dispose();
        }

        #endregion

        #region Utility methods, private

        // UTILITY (PRIVATE) --------------------------------------------------------------------------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int BytesPerPixelStandard(Bitmap src)
        {
            int bpp;

            switch (src.PixelFormat)
            {
                case System.Drawing.Imaging.PixelFormat.Format24bppRgb: bpp = 3; break;
                case System.Drawing.Imaging.PixelFormat.Format32bppArgb: bpp = 4; break;
                case System.Drawing.Imaging.PixelFormat.Format32bppRgb: bpp = 4; break;
                default: throw new BitmapException("Image format not supported.");
            }

            return (bpp);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float GetBytesPerPixel(Bitmap src)
        {
            float depth = 0.0f;

            if (src != null)
            {
                try
                {
                    switch (src.PixelFormat)
                    {
                        case System.Drawing.Imaging.PixelFormat.Format1bppIndexed:
                            depth = 0.125f;
                            break;

                        case System.Drawing.Imaging.PixelFormat.Format4bppIndexed:
                            depth = 0.5f;
                            break;

                        case System.Drawing.Imaging.PixelFormat.Format8bppIndexed:
                            depth = 1.0f;
                            break;

                        case System.Drawing.Imaging.PixelFormat.Format16bppArgb1555:
                        case System.Drawing.Imaging.PixelFormat.Format16bppGrayScale:
                        case System.Drawing.Imaging.PixelFormat.Format16bppRgb555:
                        case System.Drawing.Imaging.PixelFormat.Format16bppRgb565:
                            depth = 2.0f;
                            break;

                        case System.Drawing.Imaging.PixelFormat.Format24bppRgb:
                            depth = 3.0f;
                            break;

                        case System.Drawing.Imaging.PixelFormat.Format32bppArgb:
                        case System.Drawing.Imaging.PixelFormat.Format32bppPArgb:
                        case System.Drawing.Imaging.PixelFormat.Format32bppRgb:
                            depth = 4.0f;
                            break;

                        case System.Drawing.Imaging.PixelFormat.Format48bppRgb:
                            depth = 6.0f;
                            break;

                        case System.Drawing.Imaging.PixelFormat.Format64bppArgb:
                        case System.Drawing.Imaging.PixelFormat.Format64bppPArgb:
                            depth = 8.0f;
                            break;

                        default: throw new BitmapException("Image format not supported.");
                    }
                }
                catch (Exception ex)
                {
                    throw new BitmapException("GetImageBytesPerPixel()", ex);
                }
            }

            return (depth);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ImageFormat FindFormat(byte[] data)
        {
            // Byte array constants for image type detection, better than standard C# bitmap
            byte[] jpg1 = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };
            byte[] jpg2 = new byte[] { 0xFF, 0xD8, 0xFF, 0xE1 };
            byte[] png1 = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
            byte[] bmp1 = new byte[] { 0x42, 0x4D };
            byte[] gif1 = new byte[] { 0x47, 0x49, 0x46 };
            byte[] tif1 = new byte[] { 0x49, 0x49, 0x2A };
            byte[] tif2 = new byte[] { 0x4D, 0x4D, 0x2A };

            ImageFormat result = ImageFormat.MemoryBmp;

            if (data.FindByte(jpg1) > -1 || data.FindByte(jpg2) > -1)
                result = ImageFormat.Jpeg;
            else if (data.FindByte(png1) > -1)
                result = ImageFormat.Png;
            else if (data.FindByte(bmp1) > -1)
                result = ImageFormat.Bmp;
            else if (data.FindByte(gif1) > -1)
                result = ImageFormat.Gif;
            else if (data.FindByte(tif1) > -1 || data.FindByte(tif2) > -1)
                result = ImageFormat.Tiff;

            return (result);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ImageFormat FindFormat(object obj)
        {
            ImageFormat result = ImageFormat.MemoryBmp;
            byte[] imgData = null;

            if (obj is string)
            {
                // Raw byte data read directly from the file stream
                string file = (string)obj;
                using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read))
                {
                    Array.Resize(ref imgData, 10);
                    fs.Read(imgData, 0, imgData.Length);
                    result = FindFormat(imgData);
                }
            }
            else if (obj is MemoryStream)
            {
                // Raw byte data read by serializing the memory stream, method on the class
                MemoryStream data = (MemoryStream)obj;
                imgData = data.ToArray();
                result = FindFormat(imgData);
            }
            else if (obj is Bitmap)
            {
                // Raw byte data read by serializing bitmap class, extension method
                Bitmap bmp = (Bitmap)obj;
                imgData = bmp.ToByteArray();
                result = FindFormat(imgData);
            }

            return (result);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetupResampleMode(ResampleMode mode)
        {
            switch (mode)
            {
                case ResampleMode.Nearest:
                    _mode = InterpolationMode.NearestNeighbor;
                    break;

                case ResampleMode.BiLinear:
                    _mode = InterpolationMode.HighQualityBilinear;
                    break;

                case ResampleMode.BiCubic:
                    _mode = InterpolationMode.HighQualityBicubic;
                    break;

                default:
                    _mode = InterpolationMode.NearestNeighbor;
                    break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Bitmap ResizeImage(Bitmap src, int width, int height)
        {
            Bitmap dest = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(dest))
            {
                g.InterpolationMode = _mode;
                g.CompositingQuality = CompositingQuality.HighSpeed;
                g.CompositingMode = CompositingMode.SourceCopy;
                g.DrawImage(src, new Rectangle(0, 0, width, height));
                g.Save();
            }

            return (dest);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe Bitmap Grayscale(Bitmap src)
        {
            Bitmap bmp = (Bitmap)src.Clone();
            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            BitmapData data = bmp.LockBits(rect, ImageLockMode.ReadWrite, bmp.PixelFormat);
            int bytesPerPixel = BytesPerPixelStandard(src);

            for (int y = 0; y < data.Height; y++)
            {
                byte* destPixels = (byte*)data.Scan0 + (y * data.Stride);
                for (int x = 0; x < data.Width; x++)
                {
                    int addr = x * bytesPerPixel; // Pre-calculate, faster in the loop
                    int value = (destPixels[addr] + destPixels[addr + 1] + destPixels[addr + 2]) / 3;

                    destPixels[addr + 0] = (byte)value; // B
                    destPixels[addr + 1] = (byte)value; // G
                    destPixels[addr + 2] = (byte)value; // R
                }
            }

            bmp.UnlockBits(data);

            return (bmp);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe Bitmap Grayscale_8Bpp(Bitmap src)
        {
            int w = src.Width;
            int h = src.Height;
            int bytesPerPixel = BytesPerPixelStandard(src);

            // Create the new bitmap
            Bitmap dest = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);

            // Build a grayscale color Palette
            ColorPalette palette = dest.Palette;
            for (int i = 0; i < 256; i++)
            {
                palette.Entries[i] = System.Drawing.Color.FromArgb(255, i, i, i);
            }
            dest.Palette = palette;

            // No need to convert formats if already in 8 bit
            if (src.PixelFormat == System.Drawing.Imaging.PixelFormat.Format8bppIndexed)
            {
                dest = (Bitmap)src.Clone();

                // Make sure palette is grayscale palette and not some other 8-bit indexed palette
                dest.Palette = palette;

                return (dest);
            }

            // Lock the images
            BitmapData srcData =
                src.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, src.PixelFormat);
            BitmapData destData =
                dest.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly,
                System.Drawing.Imaging.PixelFormat.Format8bppIndexed);

            int srcStride = srcData.Stride;
            int destStride = destData.Stride;

            byte* srcPtr = (byte*)srcData.Scan0.ToPointer();
            byte* destPtr = (byte*)destData.Scan0.ToPointer();

            // Convert the pixel to it's luminance using the formula:
            // L = 0.299*R + 0.587*G + 0.114*B
            // Note that [ic] is input column and [oc] is output column, alpha channel is ignored
            for (int r = 0; r < h; r++)
            {
                for (int ic = 0, oc = 0; oc < w; ic += bytesPerPixel, ++oc)
                {
                    long srcIdx = (r * srcStride + ic);
                    long destIdx = (r * destStride + oc);

                    destPtr[destIdx] = (byte)(
                        0.299f * srcPtr[srcIdx + 0] +
                        0.587f * srcPtr[srcIdx + 1] +
                        0.114f * srcPtr[srcIdx + 2]);
                }
            }

            // Unlock the images
            src.UnlockBits(srcData);
            dest.UnlockBits(destData);

            return (dest);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Bitmap RotateBitmap(Bitmap bmp, float angle)
        {
            System.Drawing.Drawing2D.Matrix origin = new System.Drawing.Drawing2D.Matrix();
            origin.Rotate(angle);

            // Rotate the image corners to see how big it will be after rotation
            PointF[] points =
            {
                new PointF(0, 0),
                new PointF(bmp.Width, 0),
                new PointF(bmp.Width, bmp.Height),
                new PointF(0, bmp.Height),
            };
            origin.TransformPoints(points);

            origin.GetPointBounds(points,
                out float xmin, out float xmax, out float ymin, out float ymax);

            // Make a bitmap to hold the rotated result
            int width = (int)Math.Round(xmax - xmin);
            int height = (int)Math.Round(ymax - ymin);
            Bitmap result = new Bitmap(width, height, bmp.PixelFormat);

            // Create the real rotation transformation
            System.Drawing.Drawing2D.Matrix centre = new System.Drawing.Drawing2D.Matrix();
            centre.RotateAt(angle, new PointF(width / 2f, height / 2f));

            // Draw the image onto the new bitmap rotated
            using (Graphics g = Graphics.FromImage(result))
            {
                g.InterpolationMode = this._mode;
                g.CompositingQuality = CompositingQuality.HighSpeed;
                g.Clear(bmp.GetPixel(0, 0));
                g.Transform = centre;

                // Draw the image centered on the bitmap
                int x = (width - bmp.Width) / 2;
                int y = (height - bmp.Height) / 2;
                g.DrawImage(bmp, new Rectangle(x, y, bmp.Width, bmp.Height));
            }

            return (result);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Bitmap AdjustBrightness(Bitmap src, float percent)
        {
            float b = (percent >= 0.0f && percent <= 100.0f) ? (100.0f + percent) / 100.0f : 1.0f;
            var matrix = new ColorMatrix(
                new float [][]
                {
                    new [] {   b, 0.0f, 0.0f, 0.0f, 0.0f},
                    new [] {0.0f,    b, 0.0f, 0.0f, 0.0f},
                    new [] {0.0f, 0.0f,    b, 0.0f, 0.0f},
                    new [] {0.0f, 0.0f, 0.0f, 1.0f, 0.0f},
                    new [] {0.0f, 0.0f, 0.0f, 0.0f, 1.0f}
                }
            );

            ImageAttributes attrib = new ImageAttributes();
            attrib.SetColorMatrix(matrix);

            Point[] points =
            {
                new Point(0, 0),
                new Point(src.Width, 0),
                new Point(0, src.Height - 1),
            };
            Rectangle rect = new Rectangle(0, 0, src.Width, src.Height);

            Bitmap dest = new Bitmap(src.Width, src.Height);
            using (Graphics g = Graphics.FromImage(dest))
            {
                g.InterpolationMode = _mode;
                g.CompositingQuality = CompositingQuality.HighSpeed;
                g.DrawImage(src, points, rect, GraphicsUnit.Pixel, attrib);
            }

            return (dest);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe Bitmap AdjustContrast(Bitmap src, float percent)
        {
            float c = (percent >= 0.0f && percent <= 100.0f) ? (100.0f + percent) / 100.0f : 1.0f;
            Bitmap bmp = (Bitmap)src.Clone();
            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            BitmapData data = bmp.LockBits(rect, ImageLockMode.ReadWrite, bmp.PixelFormat);
            int bytesPerPixel = BytesPerPixelStandard(src);
            byte[] contrastLookup = new byte[256];

            c *= c;

            // Old games programming trick, stick contrast values in a lookup table, much faster
            for (int i = 0; i < 256; i++)
            {
                float newValue = i;
                newValue /= 255.0f;
                newValue -= 0.5f;
                newValue *= c;
                newValue += 0.5f;
                newValue *= 255;

                if (newValue < 0) { newValue = 0; }
                if (newValue > 255) { newValue = 255; }

                contrastLookup[i] = (byte)newValue;
            }

            for (int y = 0; y < data.Height; y++)
            {
                byte* destPixels = (byte*)data.Scan0 + (y * data.Stride);

                for (int x = 0; x < data.Width; x++)
                {
                    int addr = x * bytesPerPixel; // Pre-calculate, faster in the loop

                    destPixels[addr + 0] = contrastLookup[destPixels[addr + 0]]; // B
                    destPixels[addr + 1] = contrastLookup[destPixels[addr + 1]]; // G
                    destPixels[addr + 2] = contrastLookup[destPixels[addr + 2]]; // R

                    if (bytesPerPixel == 4) // Standard RGB with Alpha channel
                    {
                        destPixels[addr + 3] = contrastLookup[destPixels[addr + 3]]; // A
                    }
                }
            }

            bmp.UnlockBits(data);

            return (bmp);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe Bitmap Reduce_1Bpp(Bitmap src, int threshold)
        {
            int th = (threshold >= 0 && threshold <= 255) ? threshold : 128;
            int bytesPerPixel = BytesPerPixelStandard(src);

            // Create new destination bitmap, dimensions of original and 1bpp
            Bitmap dest = new Bitmap(src.Width, src.Height,
                System.Drawing.Imaging.PixelFormat.Format1bppIndexed);

            // Lock the images to obtain the image data
            BitmapData srcData = src.LockBits(
                new Rectangle(0, 0, src.Width, src.Height),
                ImageLockMode.ReadOnly, src.PixelFormat);

            BitmapData destData = dest.LockBits(
                new Rectangle(0, 0, dest.Width, dest.Height),
                ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format1bppIndexed);

            for (int y = 0; y < srcData.Height; y++)
            {
                byte* srcPtr = (byte*)srcData.Scan0.ToPointer() + (y * srcData.Stride);
                byte* destPtr = (byte*)destData.Scan0.ToPointer() + (y * destData.Stride);

                for (int x = 0; x < srcData.Width; x++)
                {
                    int addr = x * bytesPerPixel; // Pre-calculate, faster in the loop

                    // Average out the RGB values before comparing to threshold
                    int rgbAvg = (srcPtr[addr] + srcPtr[addr + 1] + srcPtr[addr + 2]) / 3;
                    byte value = (byte)(rgbAvg <= th ? 0 : 0xff);

                    if (value == 0xff)
                    {
                        // Destination byte for pixel (1bpp, ie 8 pixels per byte)
                        var idx = (x >> 3);
                        // Mask out pixel bit in destination byte
                        destPtr[idx] |= (byte)(0x80 >> (x & 0x7));
                    }
                }
            }

            // Unlock the images
            src.UnlockBits(srcData);
            dest.UnlockBits(destData);

            return (dest);
        }

        #endregion

        #region Image encoding routines (JPEG, PNG, BMP, GIF, TIFF)

        // IMAGE ENCODING (PRIVATE) -------------------------------------------------------------------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private byte[] GetImageData(Bitmap bmp, out int stride)
        {
            var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            var data = bmp.LockBits(rect, ImageLockMode.ReadOnly, bmp.PixelFormat);
            var length = data.Stride * data.Height;
            byte[] bytes = new byte[length];

            Marshal.Copy(data.Scan0, bytes, 0, length);
            stride = data.Stride;
            bmp.UnlockBits(data);

            return (bytes);
        }

        // Image encoding for the newer image standards: JPEG, PNG, BMP
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private BitmapSource EncodeBitmap(Bitmap bmp)
        {
            int width = bmp.Width;
            int height = bmp.Height;
            float horz = bmp.HorizontalResolution;
            float vert = bmp.VerticalResolution;
            byte[] imgData = GetImageData(bmp, out int stride);
            BitmapSource image;

            if (bmp.PixelFormat == System.Drawing.Imaging.PixelFormat.Format1bppIndexed)
            {
                // Black and white image, usually a TIFF is the source, use that pixel format
                image = BitmapSource.Create(
                    width, height, horz, vert,
                    PixelFormats.Indexed1, BitmapPalettes.BlackAndWhite, imgData, stride);
            }
            else if (bmp.PixelFormat == System.Drawing.Imaging.PixelFormat.Format8bppIndexed)
            {
                // Assume grayscale as that is the only routine that returns that pixel format
                image = BitmapSource.Create(
                    width, height, horz, vert,
                    PixelFormats.Indexed8, BitmapPalettes.Gray256, imgData, stride);
            }
            else if (bmp.PixelFormat == System.Drawing.Imaging.PixelFormat.Format16bppArgb1555)
            {
                image = BitmapSource.Create(
                    width, height, horz, vert,
                    PixelFormats.Bgr555, null, imgData, stride);
            }
            else if (bmp.PixelFormat == System.Drawing.Imaging.PixelFormat.Format16bppGrayScale)
            {
                image = BitmapSource.Create(
                    width, height, horz, vert,
                    PixelFormats.Default, null, imgData, stride);
            }
            else if (bmp.PixelFormat == System.Drawing.Imaging.PixelFormat.Format16bppRgb555)
            {
                image = BitmapSource.Create(
                    width, height, horz, vert,
                    PixelFormats.Bgr555, null, imgData, stride);
            }
            else if (bmp.PixelFormat == System.Drawing.Imaging.PixelFormat.Format16bppRgb565)
            {
                image = BitmapSource.Create(
                    width, height, horz, vert,
                    PixelFormats.Bgr565, null, imgData, stride);
            }
            else if (bmp.PixelFormat == System.Drawing.Imaging.PixelFormat.Format24bppRgb)
            {
                image = BitmapSource.Create(
                    width, height, horz, vert,
                    PixelFormats.Bgr24, null, imgData, stride);
            }
            else if (bmp.PixelFormat == System.Drawing.Imaging.PixelFormat.Format32bppArgb)
            {
                image = BitmapSource.Create(
                    width, height, horz, vert,
                    PixelFormats.Bgra32, null, imgData, stride);
            }
            else if (bmp.PixelFormat == System.Drawing.Imaging.PixelFormat.Format32bppPArgb)
            {
                image = BitmapSource.Create(
                    width, height, horz, vert,
                    PixelFormats.Pbgra32, null, imgData, stride);
            }
            else if (bmp.PixelFormat == System.Drawing.Imaging.PixelFormat.Format32bppRgb)
            {
                image = BitmapSource.Create(
                    width, height, horz, vert,
                    PixelFormats.Bgr32, null, imgData, stride);
            }
            else if (bmp.PixelFormat == System.Drawing.Imaging.PixelFormat.Format48bppRgb)
            {
                image = BitmapSource.Create(
                    width, height, horz, vert,
                    PixelFormats.Rgb48, null, imgData, stride);
            }
            else
            {
                throw new BitmapException("New: Bitmap pixel type not supported.");
            }

            return (image);
        }

        // Image encoding for the old image standards (been around for donkeys years): GIF, TIFF
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private BitmapSource EncodeBitmapOld(Bitmap bmp)
        {
            int width = bmp.Width;
            int height = bmp.Height;
            float horz = bmp.HorizontalResolution;
            float vert = bmp.VerticalResolution;
            byte[] imgData = GetImageData(bmp, out int stride);
            BitmapSource image;

            if (bmp.PixelFormat == System.Drawing.Imaging.PixelFormat.Format1bppIndexed)
            {
                image = BitmapSource.Create(
                    width, height, horz, vert,
                    PixelFormats.Indexed1, BitmapPalettes.BlackAndWhite, imgData, stride);
            }
            else if (bmp.PixelFormat == System.Drawing.Imaging.PixelFormat.Format4bppIndexed)
            {
                image = BitmapSource.Create(
                    width, height, horz, vert,
                    PixelFormats.Indexed4, BitmapPalettes.Halftone8, imgData, stride);
            }
            else if (bmp.PixelFormat == System.Drawing.Imaging.PixelFormat.Format8bppIndexed)
            {
                // Assume grayscale as that is the only routine in here that returns that pixel format
                image = BitmapSource.Create(
                    width, height, horz, vert,
                    PixelFormats.Indexed8, BitmapPalettes.Gray256, imgData, stride);
            }
            else
            {
                throw new BitmapException("Old: Bitmap pixel type not supported.");
            }

            return (image);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private byte[] EncodeJpegData(Bitmap bmp, int quality)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                JpegBitmapEncoder encoder = new JpegBitmapEncoder()
                {
                    FlipHorizontal = false,
                    FlipVertical = false,
                    QualityLevel = quality
                };

                encoder.Frames.Add(BitmapFrame.Create(EncodeBitmap(bmp)));
                encoder.Save(stream);

                return (stream.ToArray());
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private byte[] EncodePngData(Bitmap bmp)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                PngBitmapEncoder encoder = new PngBitmapEncoder();

                encoder.Frames.Add(BitmapFrame.Create(EncodeBitmap(bmp)));
                encoder.Save(stream);

                return (stream.ToArray());
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private byte[] EncodeBmpData(Bitmap bmp)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BmpBitmapEncoder encoder = new BmpBitmapEncoder();

                encoder.Frames.Add(BitmapFrame.Create(EncodeBitmap(bmp)));
                encoder.Save(stream);

                return (stream.ToArray());
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private byte[] EncodeGifData(Bitmap bmp)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                GifBitmapEncoder encoder = new GifBitmapEncoder();

                encoder.Frames.Add(BitmapFrame.Create(EncodeBitmapOld(bmp)));
                encoder.Save(stream);

                return (stream.ToArray());
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private byte[] EncodeTiffData(Bitmap bmp)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                TiffBitmapEncoder encoder = new TiffBitmapEncoder();

                if (bmp.PixelFormat == System.Drawing.Imaging.PixelFormat.Format1bppIndexed)
                    encoder.Compression = TiffCompressOption.Ccitt3;
                else if (bmp.PixelFormat == System.Drawing.Imaging.PixelFormat.Format4bppIndexed)
                    encoder.Compression = TiffCompressOption.Zip;
                else if (bmp.PixelFormat == System.Drawing.Imaging.PixelFormat.Format8bppIndexed)
                    encoder.Compression = TiffCompressOption.Zip;

                encoder.Frames.Add(BitmapFrame.Create(EncodeBitmapOld(bmp)));
                encoder.Save(stream);

                return (stream.ToArray());
            }
        }

        #endregion

        #region Public API interface routines

        // API ----------------------------------------------------------------------------------------------

        /// <summary>
        /// Image resample method: [Nearest], [BiLinear], [BiCubic] (best quality but slowest).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetResampleMode(ResampleMode mode)
        {
            try
            {
                SetupResampleMode(mode);
            }
            catch (Exception ex)
            {
                throw new BitmapException("SetResampleMode(mode)", ex);
            }
        }

        /// <summary>
        /// Create a blank bitmap object of the desired dimensions (width and height).
        /// </summary>
        /// <param name="width">Width in pixels</param>
        /// <param name="height">Height in pixels</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Create(int width, int height)
        {
            try
            {
                _orig?.Dispose();
                _orig = new Bitmap(width, height);
                _tran?.Dispose();
                _tran = new Bitmap(width, height);
            }
            catch (Exception ex)
            {
                throw new BitmapException("Create(width,height)", ex);
            }
        }

        /// <summary>
        /// Create a blank bitmap object of the desired dimensions (width and height) and pixel format.
        /// </summary>
        /// <param name="width">Width in pixels</param>
        /// <param name="height">Height in pixels</param>
        /// <param name="format">Requied pixel format</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Create(int width, int height, System.Drawing.Imaging.PixelFormat format)
        {
            try
            {
                _orig?.Dispose();
                _orig = new Bitmap(width, height, format);
                _tran?.Dispose();
                _tran = new Bitmap(width, height, format);
            }
            catch (Exception ex)
            {
                throw new BitmapException("Create(width,height,format)", ex);
            }
        }

        /// <summary>
        /// Allows bitmap to be reset back to its original state without reloading, kind of double buffering.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            try
            {
                if (_orig != null)
                {
                    _tran?.Dispose();
                    _tran = new Bitmap(_orig);
                }
            }
            catch (Exception ex)
            {
                throw new BitmapException("Reset()", ex);
            }
        }

        /// <summary>
        /// Loads from the passed filename, creates two instances, one original, the other for manipulation.
        /// </summary>
        /// <param name="file">The filename to load</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Load(string file)
        {
            try
            {
                _orig?.Dispose();
                _orig = new Bitmap(file);
                _tran?.Dispose();
                _tran = new Bitmap(file);
                _fmt = FindFormat(file);
            }
            catch (Exception ex)
            {
                throw new BitmapException("Load(file)", ex);
            }
        }

        /// <summary>
        /// Loads from the passed stream, creates two instances, one original, the other for manipulation.
        /// </summary>
        /// <param name="data">The stream to load</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Load(MemoryStream data)
        {
            try
            {
                _orig?.Dispose();
                _orig = new Bitmap(data);
                _tran?.Dispose();
                _tran = new Bitmap(data);
                _fmt = FindFormat(data);
            }
            catch (Exception ex)
            {
                throw new BitmapException("Load(data)", ex);
            }
        }

        /// <summary>
        /// Loads from passed image object, creates two instances, one original, the other for manipulation.
        /// </summary>
        /// <param name="image">The image to load</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Load(Image image)
        {
            try
            {
                _orig?.Dispose();
                _orig = new Bitmap(image);
                _tran?.Dispose();
                _tran = new Bitmap(image);
                _fmt = FindFormat((Bitmap)image);
            }
            catch (Exception ex)
            {
                throw new BitmapException("Load(image)", ex);
            }
        }

        /// <summary>
        /// Saves the current transformed bitmap to the specified filename.
        /// </summary>
        /// <param name="file">The filename to save</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Save(string file)
        {
            try
            {
                _tran?.Save(file);
            }
            catch (Exception ex)
            {
                throw new BitmapException("Save(file)", ex);
            }
        }

        /// <summary>
        /// Saves the image data to the MemoryStream, automatically uses the detected format when loaded. 
        /// </summary>
        /// <param name="data">MemoryStream to save image data</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Save(MemoryStream data)
        {
            try
            {
                _tran?.Save(data, _fmt);
            }
            catch (Exception ex)
            {
                throw new BitmapException("Save(data)", ex);
            }
        }

        /// <summary>
        /// Saves the image data to the MemoryStream, allows the specification of image format by string. 
        /// </summary>
        /// <param name="data">MemoryStream to save image data</param>
        /// <param name="format">Image format to use (JPG, PNG, BMP, GIF, TIF)</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Save(MemoryStream data, string format)
        {
            try
            {
                if (_orig != null && _tran != null)
                {
                    byte[] imgData;
                    string fmt = format.ToLower();
                    StringComparison cmp = StringComparison.Ordinal;

                    // Assume JPEG quality level of 90%, pretty much covers everything
                    if (fmt.IndexOf("jpg", cmp) > -1 || fmt.IndexOf("jpeg", cmp) > -1)
                        imgData = EncodeJpegData(_tran, 90);
                    else if (fmt.IndexOf("png", cmp) > -1)
                        imgData = EncodePngData(_tran);
                    else if (fmt.IndexOf("bmp", cmp) > -1 || fmt.IndexOf("bitmap", cmp) > -1)
                        imgData = EncodeBmpData(_tran);
                    else if (fmt.IndexOf("gif", cmp) > -1)
                        imgData = EncodeGifData(_tran);
                    else if (fmt.IndexOf("tif", cmp) > -1 || fmt.IndexOf("tiff", cmp) > -1)
                        imgData = EncodeTiffData(_tran);
                    else
                        throw new BitmapException("Image format not supported.");

                    data.Write(imgData, 0, imgData.Length);
                }
            }
            catch (Exception ex)
            {
                throw new BitmapException("Save(data,format)", ex);
            }
        }

        /// <summary>
        /// Saves the image data to the MemoryStream, this allows the specification of the image format. 
        /// </summary>
        /// <param name="data">MemoryStream to save image data</param>
        /// <param name="format">Image format to use (JPG, PNG, BMP, GIF, TIF)</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Save(MemoryStream data, ImageFormat format)
        {
            try
            {
                if (_orig != null && _tran != null)
                {
                    byte[] imgData;

                    // Assume JPEG quality level of 90%, pretty much covers everything
                    if (format.Equals(ImageFormat.Jpeg))
                        imgData = EncodeJpegData(_tran, 90);
                    else if (format.Equals(ImageFormat.Png))
                        imgData = EncodePngData(_tran);
                    else if (format.Equals(ImageFormat.Bmp))
                        imgData = EncodeBmpData(_tran);
                    else if (format.Equals(ImageFormat.Gif))
                        imgData = EncodeGifData(_tran);
                    else if (format.Equals(ImageFormat.Tiff))
                        imgData = EncodeTiffData(_tran);
                    else
                        throw new BitmapException("Image format not supported.");

                    data.Write(imgData, 0, imgData.Length);
                }
            }
            catch (Exception ex)
            {
                throw new BitmapException("Save(data,format)", ex);
            }
        }

        /// <summary>
        /// Resize image by percentage, below 100% reduces size, above 100% increases size, keeps aspect ratio. 
        /// </summary>
        /// <param name="percent">Percentage to resize image by</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Resize(float percent)
        {
            try
            {
                if (_orig != null && _tran != null)
                {
                    float fractionalPercentage = (percent / 100.0f);
                    int outputWidth = (int)(_orig.Width * fractionalPercentage);
                    int outputHeight = (int)(_orig.Height * fractionalPercentage);

                    _tran = ResizeImage(_tran, outputWidth, outputHeight);
                }
            }
            catch (Exception ex)
            {
                throw new BitmapException("Resize(percent)", ex);
            }
        }

        /// <summary>
        /// Resize the image to the desired dimensions using the currently set resample mode.
        /// </summary>
        /// <param name="width">Resize width in pixels</param>
        /// <param name="height">Resize height in pixels</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Resize(int width, int height)
        {
            try
            {
                _tran = (_orig != null && _tran != null) ? ResizeImage(_tran, width, height) : null;
            }
            catch (Exception ex)
            {
                throw new BitmapException("Resize(width,height)", ex);
            }
        }

        /// <summary>
        /// This will crop the image from a starting (x, y) point for (width, height) pixels.
        /// </summary>
        /// <param name="x">Starting X position</param>
        /// <param name="y">Starting Y position</param>
        /// <param name="width">Width in pixels</param>
        /// <param name="height">Height in pixels</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Crop(int x, int y, int width, int height)
        {
            try
            {
                if (_orig != null && _tran != null)
                {
                    Rectangle rect = new Rectangle(x, y, width, height);
                    _tran = _tran.Clone(rect, System.Drawing.Imaging.PixelFormat.DontCare);
                }
            }
            catch (Exception ex)
            {
                throw new BitmapException("Crop(x,y,width,height)", ex);
            }
        }

        /// <summary>
        /// This will apply a grayscale transformation to the image, done at default bit depth.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Grayscale()
        {
            try
            {
                _tran = (_orig != null && _tran != null) ? Grayscale(_tran) : null;
            }
            catch (Exception ex)
            {
                throw new BitmapException("Grayscale()", ex);
            }
        }

        /// <summary>
        /// This will apply a grayscale transformation to the image and convert to 8bpp.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Grayscale_8Bpp()
        {
            try
            {
                _tran = (_orig != null && _tran != null) ? Grayscale_8Bpp(_tran) : null;
            }
            catch (Exception ex)
            {
                throw new BitmapException("Grayscale_8Bpp()", ex);
            }
        }

        /// <summary>
        /// This will apply a 90deg rotation clockwise transformation, done at standard bit depth. 
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Rotate90()
        {
            try
            {
                _tran?.RotateFlip(RotateFlipType.Rotate90FlipNone);
            }
            catch (Exception ex)
            {
                throw new BitmapException("Rotate90()", ex);
            }
        }

        /// <summary>
        /// This will apply a 180deg rotation clockwise transformation, done at standard bit depth. 
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Rotate180()
        {
            try
            {
                _tran?.RotateFlip(RotateFlipType.Rotate180FlipNone);
            }
            catch (Exception ex)
            {
                throw new BitmapException("Rotate180()", ex);
            }
        }

        /// <summary>
        /// This will apply a 270deg rotation clockwise transformation, done at standard bit depth. 
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Rotate270()
        {
            try
            {
                _tran?.RotateFlip(RotateFlipType.Rotate270FlipNone);
            }
            catch (Exception ex)
            {
                throw new BitmapException("Rotate270()", ex);
            }
        }

        /// <summary>
        /// This will apply a vertical flip transformation, done at standard bit depth. 
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FlipVertical()
        {
            try
            {
                _tran?.RotateFlip(RotateFlipType.RotateNoneFlipX);
            }
            catch (Exception ex)
            {
                throw new BitmapException("FlipVertical()", ex);
            }
        }

        /// <summary>
        /// This will apply a horizontal flip transformation, done at standard bit depth. 
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FlipHorizontal()
        {
            try
            {
                _tran?.RotateFlip(RotateFlipType.RotateNoneFlipY);
            }
            catch (Exception ex)
            {
                throw new BitmapException("FlipHorizontal()", ex);
            }
        }

        /// <summary>
        /// Performs image rotation at any angle, much slower than the custom 90, 180 and 270 rotations.
        /// </summary>
        /// <param name="angle">The angle to rotate in degrees (0 - 360)</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Rotate(float angle)
        {
            try
            {
                _tran = (_orig != null && _tran != null) ? RotateBitmap(_tran, angle) : null;
            }
            catch (Exception ex)
            {
                throw new BitmapException("Rotate(angle)", ex);
            }
        }

        /// <summary>
        /// Adjust the colour brightness of the image.
        /// </summary>
        /// <param name="percent">Adjustment as percentage (0 - 100)</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Brightness(float percent)
        {
            try
            {
                _tran = (_orig != null && _tran != null) ? AdjustBrightness(_tran, percent) : null;
            }
            catch (Exception ex)
            {
                throw new BitmapException("Brightness(percent)", ex);
            }
        }

        /// <summary>
        /// Adjust the colour contrast of the image (V1) 
        /// </summary>
        /// <param name="percent">Adjustment as percentage (0 - 100)</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Contrast(float percent)
        {
            try
            {
                _tran = (_orig != null && _tran != null) ? AdjustContrast(_tran, percent) : null;
            }
            catch (Exception ex)
            {
                throw new BitmapException("Contrast(percent)", ex);
            }
        }

        /// <summary>
        /// RGB reduce image with threshold value then convert to 1bpp, used for TIFF operations (PDF).
        /// </summary>
        /// <param name="percent">Threshold percentage value ([-1 default] or [0 - 100])</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reduce_1Bpp(float percent)
        {
            try
            {
                if (_orig != null && _tran != null)
                {
                    int threshold = (Math.Abs(percent) <= -1) ?
                        (int)Math.Ceiling((255.0 / 100.0) * rgbThresholdValue) :
                        (int)Math.Ceiling((255.0 / 100.0) * percent);

                    _tran = Reduce_1Bpp(_tran, threshold);
                }
            }
            catch (Exception ex)
            {
                throw new BitmapException("Reduce_1Bpp(percent)", ex);
            }
        }

        /// <summary>
        /// Returns image data and scan line stride length, C# bitmap object does not allow this easily.
        /// </summary>
        /// <returns>Byte array of the image data, stride in bytes of a single scan line.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte[] ImageData(out int stride)
        {
            try
            {
                stride = 0;
                return ((_orig != null && _tran != null) ? GetImageData(_tran, out stride) : null);
            }
            catch (Exception ex)
            {
                throw new BitmapException("ImageData(stride)", ex);
            }
        }

        /// <summary>
        /// This takes the current bitmap image and returns a JPEG encoded byte array.
        /// </summary>
        /// <param name="quality">JPEG encoding quality (1 - 100)</param>
        /// <returns>JPEG encoded byte array.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte[] EncodeJpeg(int quality)
        {
            try
            {
                return ((_orig != null && _tran != null) ? EncodeJpegData(_tran, quality) : null);
            }
            catch (Exception ex)
            {
                throw new BitmapException("EncodeJpeg(quality)", ex);
            }
        }

        /// <summary>
        /// This takes the current bitmap image and returns a PNG encoded byte array.
        /// </summary>
        /// <returns>PNG encoded byte array.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte[] EncodePng()
        {
            try
            {
                return ((_orig != null && _tran != null) ? EncodePngData(_tran) : null);
            }
            catch (Exception ex)
            {
                throw new BitmapException("EncodePng()", ex);
            }
        }

        /// <summary>
        /// This takes the current bitmap image and returns a BMP encoded byte array.
        /// </summary>
        /// <returns>BMP encoded byte array.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte[] EncodeBmp()
        {
            try
            {
                return ((_orig != null && _tran != null) ? EncodeBmpData(_tran) : null);
            }
            catch (Exception ex)
            {
                throw new BitmapException("EncodeBmp()", ex);
            }
        }

        /// <summary>
        /// This takes the current bitmap image and returns a GIF encoded byte array.
        /// </summary>
        /// <returns>GIF encoded byte array.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte[] EncodeGif()
        {
            try
            {
                return ((_orig != null && _tran != null) ? EncodeGifData(_tran) : null);
            }
            catch (Exception ex)
            {
                throw new BitmapException("EncodeGif()", ex);
            }
        }

        /// <summary>
        /// This takes the current bitmap image and returns a TIFF encoded byte array.
        /// </summary>
        /// <returns>TIFF encoded byte array.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte[] EncodeTiff()
        {
            try
            {
                return ((_orig != null && _tran != null) ? EncodeTiffData(_tran) : null);
            }
            catch (Exception ex)
            {
                throw new BitmapException("EncodeTiff()", ex);
            }
        }

        #endregion
    }

    #endregion
}
