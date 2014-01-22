/****************************************************************************
 (c) 2012 Steven Houben(shou@itu.dk) and Søren Nielsen(snielsen@itu.dk)

 Pervasive Interaction Technology Laboratory (pIT lab)
 IT University of Copenhagen

 This library is free software; you can redistribute it and/or 
 modify it under the terms of the GNU GENERAL PUBLIC LICENSE V3 or later, 
 as published by the Free Software Foundation. Check 
 http://www.gnu.org/licenses/gpl.html for details.
****************************************************************************/

using System;
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
        public static Image ConvertPdfThumbnail(string pathOfPdf)
        {
            if (!File.Exists(pathOfPdf))
                throw new FileNotFoundException("Invalid path");

            var image = new Image();

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
                    var bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = memory;
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();
                    image.Source = bitmapImage;
                }
                bitmap.Dispose();
            }
            return image;
        }

        public static Image ConvertPdfToImage(string pathOfPdf)
        {

            if (!File.Exists(pathOfPdf))
                throw new FileNotFoundException("Invalid path");

            var image = new Image();

            const int width = 595;
            const int height = 841;
            const float scale = 0.1f;

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
                    var bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = memory;
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();
                    image.Source = bitmapImage;
                }
                bitmap.Dispose();
            }
            return image;
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
