﻿using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Reflection;

namespace Resizer
{
    public static class Program
    {
        public static string GetRootDirectory()
        {
            return AppContext.BaseDirectory;
        }

        public static void Main(params string[] paths)
        {
            if (paths.Length == 0)
            {
                Console.WriteLine("Provide a path");
            }

            var rootDir = GetRootDirectory();
            var outPath = Path.Combine(rootDir, "output");
            if (!Directory.Exists(outPath))
            {
                Directory.CreateDirectory(outPath);
            }
            Console.WriteLine("Output folder: " + outPath);

            var set = new HashSet<string>();
            foreach (var path in paths)
            {
                var files = GetAllFiles(path, "*.*").ToArray();
                foreach (var file in files)
                {
                    var fi = new FileInfo(file);
                    var folder = fi.Directory.FullName.Replace(path, "");
                    if (folder.StartsWith("\\"))
                    {
                        folder = folder.Substring(1);
                    }
                    var outFolder = Path.Combine(outPath, folder);
                    if (!string.IsNullOrEmpty(outFolder) && !Directory.Exists(outFolder))
                    {
                        Directory.CreateDirectory(outFolder);
                    }
                    var ext = fi.Extension;
                    var newFileName = Path.GetFileNameWithoutExtension(fi.Name) + ".jpg";
                    switch (ext)
                    {
                        case ".jpg":
                        case ".jpeg":
                        case ".gif":
                        case ".png":
                        case ".tiff":
                        case ".bmp":
                            var image = Image.FromFile(file);
                            image = ResizeImage(image, 750, 1000);
                            SaveJpeg(Path.Combine(outFolder, newFileName), image, 80);
                            break;
                        default:
                            if (set.Add(ext))
                            {
                                Console.WriteLine("Not supported: " + ext);
                            }

                            break;
                    }
                    Console.WriteLine("processed " + file);
                }
            }
        }

        /// <summary> 
        /// Saves an image as a jpeg image, with the given quality 
        /// </summary> 
        /// <param name="path"> Path to which the image would be saved. </param> 
        /// <param name="quality"> An integer from 0 to 100, with 100 being the highest quality. </param> 
        public static void SaveJpeg(string path, Image img, int quality)
        {
            if (quality < 0 || quality > 100)
                throw new ArgumentOutOfRangeException("quality must be between 0 and 100.");

            // JPEG image codec 
            var jpegCodec = GetEncoderInfo("image/jpeg");
            var encoderParams = new EncoderParameters(1);
            encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, quality);

            try
            {
                img.Save(path, jpegCodec, encoderParams);
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to convert " + path);
                Console.WriteLine("Error: " + e.Message);
            }
        }

        /// <summary>
        /// Resize the image to the specified width and height.
        /// </summary>
        /// <param name="image">The image to resize.</param>
        /// <param name="width">The width to resize to.</param>
        /// <param name="height">The height to resize to.</param>
        /// <returns>The resized image.</returns>
        public static Image ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    var ratio = (double)width / height;
                    if (image.Width > image.Height)
                    {
                        var sourceWidth = (int)(image.Height * ratio);
                        graphics.DrawImage(image, destRect, (image.Width - sourceWidth) / 2, 0, sourceWidth, image.Height, GraphicsUnit.Pixel, wrapMode);
                    }
                    else
                    {
                        graphics.DrawImage(image, destRect, 0, 0, (int)(image.Height * ratio), image.Height, GraphicsUnit.Pixel, wrapMode);
                    }
                }
            }

            return (Image)destImage;
        }

        /// <summary> 
        /// Returns the image codec with the given mime type 
        /// </summary> 
        private static ImageCodecInfo GetEncoderInfo(string mimeType)
        {
            // Get image codecs for all image formats 
            var codecs = ImageCodecInfo.GetImageEncoders();

            // Find the correct image codec 
            for (var i = 0; i < codecs.Length; i++)
                if (codecs[i].MimeType == mimeType)
                    return codecs[i];

            return null;
        }

        public static IEnumerable<string> GetAllFiles(string path, string mask, Func<FileInfo, bool>? checkFile = null)
        {
            if (string.IsNullOrEmpty(mask))
                mask = "*.*";
            var files = Directory.GetFiles(path, mask, SearchOption.AllDirectories);
            foreach (var file in files)
            {
                if (checkFile == null || checkFile(new FileInfo(file)))
                    yield return file;
            }
        }
    }
}