using System.IO;

namespace CorporateAppStore.Helpers
{
    public sealed class MimeTypes
    {
        public const string ImagePng = @"image/png";
        public const string ImageJpeg = @"image/jpeg";
        public const string ImageGif = @"image/gif";

        public const string OctetStream = @"application/octet-stream";

        /// <summary>
        /// Gets the standard MIME type for the specified filename.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <returns></returns>
        public static string GetMimeType(string filename)
        {
            switch (Path.GetExtension(filename).ToLowerInvariant())
            {
                case ".png":
                    return ImagePng;
                case ".jpeg":
                case ".jpg":
                    return ImageJpeg;
                case ".gif":
                    return ImageGif;
                
                default:
                    return OctetStream;
            }
        }
    }
}