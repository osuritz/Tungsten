using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;

namespace CorporateAppStore.Helpers
{
    //// https://github.com/pcans/PngCompote/blob/master/pngCompote.php
    public class PngFile
    {
        internal static readonly byte[] MagicHeader = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } ;
        
        public PngFile(byte[] data)
        {            
            if (ImageHelper.GetImageFormat(data) != ImageFormat.Png)
            {
                throw new ArgumentException("It is NOT a PNG file.", "data");
            }

            using (var memStream = new MemoryStream(data, MagicHeader.Length, data.Length - MagicHeader.Length, false))
            {
                var chunk = new PngChunk(this, new BinaryReader( memStream));
            }
        }

        public bool IsIphoneCrushed { get; set; }
    }

    internal class PngChunk
    {
        private const int ChunkSizeLength = 4;
        private const int ChunkTypeLength = 4;
        private const int ChunkCrcLength = 4;

        private const string CgbiChunkType = "CgBI";

        private int _dataLength;
        private string _type;
        private string _crc;
        
        public PngChunk(PngFile png, BinaryReader stream)
        {
            
            
            // Read data length
            ////byte[] val = stream.Read
            ////int val = stream.ReadLittleEndianInt32();
            this._dataLength = stream.ReadInt32();

            // Read chunk type
            char[] chunkType = stream.ReadChars(ChunkTypeLength);
            this._type = new string(chunkType);

            // Skip chunk data
            long position = stream.BaseStream.Seek(this._dataLength, SeekOrigin.Current);
            if (position < 0)
            {
                throw new ArgumentException("Unexpected PNG content encountered");
            }

            // Read CRC
            char[] crc = stream.ReadChars(ChunkTypeLength);
            this._crc = new string(crc);

            // CgBI
            if (string.Equals(CgbiChunkType, this._type))
            {
                png.IsIphoneCrushed = true;
            }
        }
    }

    public static class StreamExtensions
    {
        public static byte[] Read(this Stream stream, int length)
        {
            byte[] buffer = new byte[length];
            int read = stream.Read(buffer, 0, length);

            if (read < length)
            {
                return buffer.Take(read).ToArray();
            }

            return buffer;
        }

        private static short ReadLittleEndianInt16(this BinaryReader binaryReader)
        {
            byte[] bytes = new byte[sizeof(short)];
            for (int i = 0; i < sizeof(short); i += 1)
            {
                bytes[sizeof(short) - 1 - i] = binaryReader.ReadByte();
            }
            return BitConverter.ToInt16(bytes, 0);
        }
        
        public static int ReadLittleEndianInt32(this BinaryReader binaryReader)
        {
            byte[] bytes = new byte[sizeof(int)];
            for (int i = 0; i < sizeof(int); i+=1)
            {
                bytes[sizeof(int) - 1 - i] = binaryReader.ReadByte();
            }

            return BitConverter.ToInt32(bytes, 0);
        }
    }

    public enum ImageFormat
    {
        Unknown = 0,
        Bmp,
        Jpeg,
        Gif,
        Tiff,
        Png
    }
    
    public class ImageHelper
    {
        //public static bool IsIPhone(s

        public static ImageFormat GetImageFormat(byte[] data)
        {
            if (PngFile.MagicHeader.SequenceEqual(data.Take(PngFile.MagicHeader.Length)))
            {
                return ImageFormat.Png;
            }

            return ImageFormat.Unknown;
        }
    }
}