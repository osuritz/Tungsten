using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Web;
using System.IO;
using System.Text;

namespace CorporateAppStore.Helpers
{
    //// https://github.com/pcans/PngCompote/blob/master/pngCompote.php
    //// PNG binary format description at http://www.fileformat.info/format/png/corion.htm
    ////public class PngFile
    ////{
    ////    private const string ChunkTypeCgbi = "CgBI";
    ////    private const string ChunkTypeImageHeader = "IHDR";
    ////    private const string ChunkTypeImageData = "IDAT";
    ////    private const string ChunkTypeImageTrailer = "IEND";
        
    ////    ////internal static readonly byte[] PngHeader = BitConverter.ToUInt64(new byte[] { 0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A }, 0);
    ////    internal static readonly UInt64 PngHeader = BitConverter.ToUInt64(new byte[] { 0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A }, 0);
    ////    ////internal UInt64 PngHeader = 0x89504E470D0A1A0A;
    ////    //internal UInt64 PngHeader = 0x0A1A0A0D474E5089;

    ////    public PngFile(byte[] data)
    ////    {
    ////        using (var readerStream = new MemoryStream(data, false))
    ////        {
    ////            var binaryReader = new BinaryReader(readerStream);

    ////            // Read and check for PNG header (8 bytes).
    ////            ulong header = binaryReader.ReadUInt64();
    ////            if (header != PngHeader)
    ////            {
    ////                throw new ArgumentException("It is NOT a PNG file.", "data");
    ////            }

    ////            // Check the first chunk header for CgBI (an extra critical header indicative of Apple's crunshed PNG format)
    ////            // http://iphonedevwiki.net/index.php/CgBI_file_format
    ////            var chunkHeader = new PngChunkHeader(binaryReader);

    ////            if (!chunkHeader.IsChunkType(ChunkTypeCgbi))
    ////            {
    ////                // No conversion needed.
    ////                this.Data = data;
    ////                return;
    ////            }

    ////            this.IsIphoneCrushed = true;

    ////            // Need to "rewind" by the size of the chunk header
    ////            readerStream.Seek(PngChunkHeader.ChunkHeaderSize * -1, SeekOrigin.Current);

    ////            this.UncrushPngData(data, readerStream, binaryReader);
    ////        }
    ////    }

    ////    private void UncrushPngData(byte[] data, MemoryStream readerStream, BinaryReader binaryReader)
    ////    {
    ////        // Create new, uncrushed PNG data
    ////        var newPngData = new byte[data.Length];

    ////        using (var writerStream = new MemoryStream(newPngData, true))
    ////        {
    ////            var writer = new BinaryWriter(writerStream);

    ////            // Write header
    ////            writer.Write(PngHeader);

    ////            uint width = 0;
    ////            uint height = 0;
    ////            while (readerStream.Position < readerStream.Length - 1)
    ////            {
    ////                // Read next chunk
    ////                var chunk = new PngChunk(binaryReader);

    ////                // Skip the CgBI chunk.
    ////                if (chunk.Header.IsChunkType(ChunkTypeCgbi))
    ////                {
    ////                    continue;
    ////                }

    ////                byte[] chunkData = chunk.Data;

    ////                // Parse the header chunk
    ////                if (chunk.Header.IsChunkType(ChunkTypeImageHeader))
    ////                {
    ////                    width = chunk.Data.ReadBigEndianUInt32();
    ////                    height = chunk.Data.ReadBigEndianUInt32(startIndex: sizeof (UInt32));
    ////                } else if (chunk.Header.IsChunkType(ChunkTypeImageData))
    ////                {
    ////                    // Uncompress (inflate) the image chunk
    ////                    uint bufferSize = width*height*4 + height;

    ////                    chunkData = this.Decompress(chunkData);

    ////                    /* Note:
    ////                     * -----
    ////                     * In CgBI, pixel data are byteswapped (RGBA -> BGRA),
    ////                     * presumably for high-speed direct blitting to the frame buffer.
    ////                     */

    ////                    // Swap red & blue bytes for each pixel (to switch back to RGBA).
    ////                    uint i = 0;
    ////                    for (int y = 0; y < height; y++)
    ////                    {
    ////                        i++;

    ////                        for (int x = 0; x < width; x++)
    ////                        {
    ////                            byte tmp = chunkData[i];            // Save blue
    ////                            chunkData[i] = chunkData[i + 2];    // Move red
    ////                            chunkData[i + 2] = tmp;             // Put blue in place
    ////                            i += 4;                             // Skipping to the next RGBA segment, thus 4.
    ////                        }
    ////                    }

    ////                    // Compress the image chunk
    ////                    chunkData = this.Compress(chunkData);

    ////                    // Update dat length on chunk header
    ////                    chunk.Header.DataLength = (uint) chunkData.Length;

    ////                    // Update CRC
    ////                    Crc
    ////                }

    ////                chunk.WriteTo(writer, chunkData);

    ////                // Stop parsing the PNG file.
    ////                if (chunk.Header.IsChunkType(ChunkTypeImageTrailer))
    ////                {                        
    ////                    break;
    ////                }
    ////            }
    ////        }

    ////        this.Data = newPngData;
    ////    }        

    ////    public byte[] Data { get; set; }

    ////    public bool IsIphoneCrushed { get; set; }

    ////    /// <summary>
    ////    /// Compresses the specified data.
    ////    /// </summary>
    ////    /// <param name="data">The data.</param>
    ////    /// <returns></returns>
    ////    private byte[] Compress(byte[] data)
    ////    {
    ////        return this.Deflate(data, CompressionMode.Compress);
    ////    }

    ////    private byte[] Decompress(byte[] data)
    ////    {
    ////        return this.Deflate(data, CompressionMode.Decompress);
    ////    }

    ////    private byte[] Deflate(byte[] data, CompressionMode compressionMode)
    ////    {
    ////        using (var deflateStream = new DeflateStream(new MemoryStream(data, false), compressionMode))
    ////        {
    ////            using (var newData = new MemoryStream())
    ////            {
    ////                deflateStream.CopyTo(newData);
    ////                return newData.ToArray();
    ////            }
    ////        }            
    ////    }
    ////}


    /////// <summary>
    /////// A PNG chunk header is made of 8 bytes. The first 4 indicate the length of the chunk data.
    /////// The last 4 indicate the chunk type.
    /////// </summary>
    ////internal class PngChunkHeader
    ////{
    ////    public const int ChunkHeaderSize = ChunkTypeLength + sizeof(UInt32);

    ////    private const int ChunkTypeLength = 4;

    ////    public PngChunkHeader(BinaryReader reader)
    ////    {
    ////        this.DataLength = reader.ReadBigEndianUInt32(); ;
            
    ////        char[] chunkType = reader.ReadChars(ChunkTypeLength);
    ////        this.ChunkType = new string(chunkType);
    ////    }

    ////    public PngChunkHeader(UInt32 dataLength, string chunkType)
    ////    {
    ////        this.DataLength = dataLength;
    ////        this.ChunkType = chunkType;
    ////    }

    ////    public uint DataLength { get; set; }
        
    ////    /// <summary>
    ////    /// Gets the chunk type.
    ////    /// </summary>
    ////    /// <value>
    ////    /// The type of the chunk.
    ////    /// </value>
    ////    public string ChunkType { get; private set; }

    ////    public bool IsChunkType(string chunkType)
    ////    {
    ////        bool match = string.Equals(chunkType, this.ChunkType, StringComparison.Ordinal);
    ////        return match;
    ////    }
    ////}

    /////// <summary>
    /////// A PNG chunk is made of a chunk header (8 bytes), its data, and a CRC.
    /////// </summary>
    ////internal  class PngChunk
    ////{
    ////    public PngChunk(BinaryReader reader)
    ////    {
    ////        this.Header = new PngChunkHeader(reader);
    ////        this.Data = reader.ReadBytes((int) this.Header.DataLength);
    ////        this.Crc = reader.ReadUInt32();
    ////    }

    ////    public PngChunkHeader Header { get; private set; }
    ////    public byte[] Data { get; set; }
    ////    public uint Crc { get; set; }

    ////    /// <summary>
    ////    /// Writes the chunk to the specified <see cref="BinaryWriter"/>.
    ////    /// </summary>
    ////    /// <param name="writer">The writer.</param>
    ////    /// <param name="data">The chunk data.</param>
    ////    public void WriteTo(BinaryWriter writer, byte[] data)
    ////    {
    ////        writer.Write(data.Length);
    ////        writer.Write(Encoding.ASCII.GetBytes(this.Header.ChunkType));
    ////        writer.Write(data);
    ////        writer.Write(this.Crc);
    ////    }

    ////    public void WriteTo(BinaryWriter writer)
    ////    {
    ////        WriteTo(writer, this.Data);
    ////    }
    ////}
    

    ////public static class StreamExtensions
    ////{
    ////    public static byte[] Read(this Stream stream, long length)
    ////    {
    ////        var buffer = new byte[length];
    ////        int read = stream.Read(buffer, 0, (int)length);

    ////        if (read < length)
    ////        {
    ////            return buffer.Take(read).ToArray();
    ////        }

    ////        return buffer;
    ////    }

    ////    private static short ReadLittleEndianInt16(this BinaryReader binaryReader)
    ////    {
    ////        byte[] bytes = new byte[sizeof(short)];
    ////        for (int i = 0; i < sizeof(short); i += 1)
    ////        {
    ////            bytes[sizeof(short) - 1 - i] = binaryReader.ReadByte();
    ////        }
    ////        return BitConverter.ToInt16(bytes, 0);
    ////    }
        
    ////    public static int ReadLittleEndianInt32(this BinaryReader binaryReader)
    ////    {
    ////        byte[] bytes = new byte[sizeof(int)];
    ////        for (int i = 0; i < sizeof(int); i+=1)
    ////        {
    ////            bytes[sizeof(int) - 1 - i] = binaryReader.ReadByte();
    ////        }

    ////        return BitConverter.ToInt32(bytes, 0);
    ////    }

    ////    public static int ReadBigEndianInt32(this BinaryReader binaryReader)
    ////    {
    ////        if (BitConverter.IsLittleEndian)
    ////        {
    ////            byte[] arr = binaryReader.ReadBytes(sizeof(UInt32));
    ////            int val = BitConverter.ToInt32(arr.Reverse().ToArray(), 0);
    ////            return val;
    ////        }

    ////        return binaryReader.ReadInt32();            
    ////    }

    ////    public static uint ReadBigEndianUInt32(this byte[] bytes, int startIndex = 0)
    ////    {
    ////        if (BitConverter.IsLittleEndian)
    ////        {
    ////            uint val = BitConverter.ToUInt32(bytes.Take(sizeof(UInt32)).Reverse().ToArray(), 0);
    ////            return val;
    ////        }

    ////        return BitConverter.ToUInt32(bytes, startIndex);
    ////    }

    ////    public static uint ReadBigEndianUInt32(this BinaryReader binaryReader)
    ////    {
    ////        if (BitConverter.IsLittleEndian)
    ////        {
    ////            return binaryReader.ReadBytes(sizeof (UInt32)).ReadBigEndianUInt32();
    ////        }

    ////        return binaryReader.ReadUInt32();            
    ////    }
    ////}

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
        internal static readonly byte[] PngHeader = new byte[] { 0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A };

        public static ImageFormat GetImageFormat(byte[] data)
        {
            if (PngHeader.SequenceEqual(data.Take(PngHeader.Length)))
            {
                return ImageFormat.Png;
            }

            return ImageFormat.Unknown;
        }
    }
}