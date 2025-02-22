using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using PluginCore.Managers;

namespace PluginCore.Utilities
{
    public class ImageKonverter
    {
        /// <summary>
        /// Converts image to an icon
        /// </summary>
        public static Icon ImageToIcon(Image image)
        {
            try
            {
                Bitmap bmp = new Bitmap(image);
                IntPtr hIcon = bmp.GetHicon();
                return Icon.FromHandle(hIcon);
            }
            catch (Exception ex)
            {
                ErrorManager.ShowError(ex);
                return null;
            }
        }

        /// <summary>
        /// Adjusts the saturation and brightness of the image.
        /// </summary>
        public static Image ImageAdjust(Image image, int saturation, int brightness)
        {
            Image dest = new Bitmap(image.Width, image.Height);
            ImageAdjust(image, dest, saturation, brightness);
            return dest;
        }

        /// <summary>
        /// Adjusts the saturation and brightness of the image.
        /// </summary>
        public static void ImageAdjust(Image source, Image dest, int saturation, int brightness)
        {
            try
            {
                float rwgt = 0.3086f;
                float gwgt = 0.6094f;
                float bwgt = 0.0820f;
                float sat = 1f + (saturation / 100f);
                float bri = 1f + (brightness / 100f);
                float baseSat = 1.0f - sat;
                float adjBrightness = bri - 1f;
                using (Graphics graphics = Graphics.FromImage(dest))
                {
                    // clear the destination before drawing the image
                    graphics.Clear(Color.Transparent);
                    ColorMatrix colorMatrix = new ColorMatrix();
                    // adjust saturation
                    colorMatrix[0, 0] = baseSat * rwgt + sat;
                    colorMatrix[0, 1] = baseSat * rwgt;
                    colorMatrix[0, 2] = baseSat * rwgt;
                    colorMatrix[1, 0] = baseSat * gwgt;
                    colorMatrix[1, 1] = baseSat * gwgt + sat;
                    colorMatrix[1, 2] = baseSat * gwgt;
                    colorMatrix[2, 0] = baseSat * bwgt;
                    colorMatrix[2, 1] = baseSat * bwgt;
                    colorMatrix[2, 2] = baseSat * bwgt + sat;
                    // adjust brightness
                    colorMatrix[4, 0] = adjBrightness;
                    colorMatrix[4, 1] = adjBrightness;
                    colorMatrix[4, 2] = adjBrightness;
                    ImageAttributes attributes = new ImageAttributes();
                    attributes.SetColorMatrix(colorMatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
                    graphics.DrawImage(source, new Rectangle(0, 0, dest.Width, dest.Height), 0, 0, source.Width, source.Height, GraphicsUnit.Pixel, attributes);
                }
            }
            catch (Exception ex)
            {
                ErrorManager.ShowError(ex);
            }
        }

        /// <summary>
        /// Converts image to a grayscale image
        /// </summary>
        public static Image ImageToGrayscale(Image image)
        {
            try
            {
                Bitmap bitmap = new Bitmap(image.Width, image.Height);
                using (Graphics graphics = Graphics.FromImage(bitmap))
                {
                    ColorMatrix matrix = new ColorMatrix(new[]
                    {
                        new[] { 0.30f, 0.30f, 0.30f, 0, 0 },
                        new[] { 0.59f, 0.59f, 0.59f, 0, 0},
                        new[] { 0.11f, 0.11f, 0.11f, 0, 0},
                        new float[] { 0, 0, 0, 1, 0, 0},
                        new float[] { 0, 0, 0, 0, 1, 0},
                        new float[] { 0, 0, 0, 0, 0, 1 }
                    });
                    ImageAttributes attributes = new ImageAttributes();
                    attributes.SetColorMatrix(matrix);
                    graphics.DrawImage(image, new Rectangle(0, 0, image.Width, image.Height), 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, attributes);
                }
                return bitmap;
            }
            catch (Exception ex)
            {
                ErrorManager.ShowError(ex);
                return image;
            }
        }

        /// <summary>
        /// Resize image with GDI+ so that image is nice and clear with required size.
        /// </summary>
        public static Image ImageResize(Bitmap source, int width, int height)
        {
            Bitmap bitmap = new Bitmap(width, height, source.PixelFormat);
            Graphics graphicsImage = Graphics.FromImage(bitmap);
            graphicsImage.SmoothingMode = SmoothingMode.HighQuality;
            graphicsImage.PixelOffsetMode = PixelOffsetMode.Half;
            graphicsImage.InterpolationMode = InterpolationMode.Bicubic;
            graphicsImage.DrawImage(source, 0, 0, bitmap.Width, bitmap.Height);
            graphicsImage.Dispose();
            return bitmap;
        }

    }

}

