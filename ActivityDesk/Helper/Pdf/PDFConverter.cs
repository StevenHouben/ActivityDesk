using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;
using ImageMagick;
using System.Windows.Controls;


namespace ActivityDesk.Helper.Pdf
{
    public sealed class PdfConverter
    {
        public static BitmapImage ConvertPdfThumbnail(string pathOfPdf)
        {
            if (!File.Exists(pathOfPdf))
                throw new FileNotFoundException("Invalid path");

            var bitmapImage = new BitmapImage();

            var settings = new MagickReadSettings
            {
                Density = new MagickGeometry(25,25),
                FrameIndex = 0,
                FrameCount = 0
            };

            using (var images = new MagickImageCollection())
            {
                images.Read(pathOfPdf, settings);
                var bitmap = images.First().ToBitmap();
                using (var memory = new MemoryStream())
                {
                    bitmap.Save(memory, ImageFormat.Bmp);
                    memory.Position = 0;

                    bitmapImage.BeginInit();
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.StreamSource = memory;
                    bitmapImage.EndInit();
                    bitmapImage.Freeze();
                }
                bitmap.Dispose();
            }
            return bitmapImage;
        }

        public static BitmapImage ConvertPdfToImage(string pathOfPdf)
        {

            if (!File.Exists(pathOfPdf))
                throw new FileNotFoundException("Invalid path");

            const int width = 595;
            const int height = 841;
            const float scale = 0.1f;

            var bitmapImage = new BitmapImage();

            var settings = new MagickReadSettings
            {
                Density = new MagickGeometry((int)(width * scale), (int)(height * scale))
            };

            using (var images = new MagickImageCollection())
            {
                images.Read(pathOfPdf,settings);

                var vertical = images.AppendVertically();

                var bitmap = vertical.ToBitmap(ImageFormat.Bmp);
                using (var memory = new MemoryStream())
                {
                    bitmap.Save(memory, ImageFormat.Bmp);
                    memory.Position = 0;
    
                    bitmapImage.BeginInit();
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.StreamSource = memory;
                    bitmapImage.EndInit();
                    bitmapImage.Freeze();
                }
                bitmap.Dispose();
            }
            return bitmapImage ;
        }

        public static List<Image> ConvertPdfToImageList(string pathOfPdf)
        {
            if (!File.Exists(pathOfPdf))
                throw new FileNotFoundException("Invalid path");

            var imageList = new List<Image>();


            const int width = 595;
            const int height = 841;
            const float scale = 0.4f;

            var settings = new MagickReadSettings
            {
                Density = new MagickGeometry((int)(width * scale), (int)(height * scale))
            };

            using (var images = new MagickImageCollection())
            {
                images.Read(pathOfPdf, settings);

                foreach (var pdfImage in images)
                {
                    var image = new Image();
                    var bitmap = pdfImage.ToBitmap(ImageFormat.Bmp);
                    using (var memory = new MemoryStream())
                    {
                        bitmap.Save(memory, ImageFormat.Bmp);
                        memory.Position = 0;
                        var bitmapImage = new BitmapImage();
                        bitmapImage.BeginInit();
                        bitmapImage.StreamSource = memory;
                        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                        bitmapImage.EndInit();
                        image.Source = bitmapImage;
                    }
                    bitmap.Dispose();
                    imageList.Add(image);
                }
            }

            return imageList;
        }
    }
}
