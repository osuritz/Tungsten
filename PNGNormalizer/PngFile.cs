using System;
using System.IO;
using System.Text;
using CorporateAppStore.Helpers;
using Ionic.Zlib;
using CompressionMode = System.IO.Compression.CompressionMode;
using DeflateStream = System.IO.Compression.DeflateStream;

namespace PNGNormalizer
{
    //// https://github.com/pcans/PngCompote/blob/master/pngCompote.php
    //// PNG binary format description at http://www.fileformat.info/format/png/corion.htm
    public class PngFile
    {
        private const string ChunkTypeCgbi = "CgBI";
        private const string ChunkTypeImageHeader = "IHDR";
        private const string ChunkTypeImageData = "IDAT";
        private const string ChunkTypeImageTrailer = "IEND";
        
        internal static readonly UInt64 PngHeader = BitConverter.ToUInt64(new byte[] { 0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A }, 0);
        internal const UInt64 PngHeaderBigEndian = 0x89504E470D0A1A0A;
        internal const UInt64 PngHeaderLittleEndian = 0x0A1A0A0D474E5089;

        public PngFile(byte[] data)
        {
            using (var readerStream = new MemoryStream(data, false))
            {
                var binaryReader = new BinaryReader(readerStream);

                // Read and check for PNG header (8 bytes).
                ulong header = binaryReader.ReadUInt64();
                if (header != PngHeader)
                {
                    throw new ArgumentException("It is NOT a PNG file.", "data");
                }

                // Check the first chunk header for CgBI (an extra critical header indicative of Apple's crunshed PNG format)
                // http://iphonedevwiki.net/index.php/CgBI_file_format
                var chunkHeader = new PngChunkHeader(binaryReader);

                if (!chunkHeader.IsChunkType(ChunkTypeCgbi))
                {
                    // No conversion needed.
                    this.Data = data;
                    return;
                }

                this.IsIphoneCrushed = true;

                // Need to "rewind" by the size of the chunk header
                readerStream.Seek(PngChunkHeader.ChunkHeaderSize * -1, SeekOrigin.Current);

                this.UncrushPngData(data, readerStream, binaryReader);
            }
        }

        public byte[] Data { get; set; }

        public bool IsIphoneCrushed { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }


        private void UncrushPngData(byte[] data, MemoryStream readerStream, BinaryReader binaryReader)
        {
            // Create new, uncrushed PNG data
            byte[] newPngData = null;

            using (var writerStream = new MemoryStream())
            {
                //var writer = new BinaryWriter(writerStream);
                var writer = new EndianBinaryWriter(writerStream, EndianBitConverter.BigEndianConverter);

                // Write header
                writer.Write(PngHeaderBigEndian);

                uint width = 0;
                uint height = 0;
                while (readerStream.Position < readerStream.Length - 1)
                {
                    // Read next chunk
                    var chunk = new PngChunk(binaryReader);

                    // Skip the CgBI chunk.
                    if (chunk.Header.IsChunkType(ChunkTypeCgbi))
                    {
                        continue;
                    }

                    ////byte[] chunkData = chunk.Data;
                    byte[] inflatedData;

                    // Parse the header chunk
                    if (chunk.Header.IsChunkType(ChunkTypeImageHeader))
                    {
                        width = chunk.Data.ReadBigEndianUInt32();
                        height = chunk.Data.ReadBigEndianUInt32(startIndex: sizeof (UInt32));

                        this.Width = (int) width;
                        this.Height = (int) height;
                    } else if (chunk.Header.IsChunkType(ChunkTypeImageData))
                    {
                        // Uncompress (inflate) the image chunk
                        uint bufferSize = width*height*4 + height;

                        inflatedData = this.Decompress(chunk.Data);
                        ////byte[] newData = this.GZInflate(chunk.Data);

                        /* Note:
                         * -----
                         * In CgBI, pixel data are byteswapped (RGBA -> BGRA),
                         * presumably for high-speed direct blitting to the frame buffer.
                         */

                        // Swap red & blue bytes for each pixel (to switch back to RGBA).
                        ////uint i = 0;
                        var scanLineSize = 1 + (this.Width * 4);   // Line length = filtertype + nb pixel on a line * size of a pixel
                        byte[] newData;
                        using (var dataRes = new MemoryStream())
                        {
                            for (int y = 0; y < this.Height; y++)
                            {
                                ////i++;
                                // Filter-type
                                var filterType = inflatedData[y * scanLineSize];
                                dataRes.WriteByte(filterType);

                                for (int x = 0; x < this.Width; x++)
                                {
                                    var pixel = inflatedData.Take(4, (y * scanLineSize + 1) + (x * 4));
                                    dataRes.WriteBytes(pixel[2], pixel[1], pixel[0], pixel[3]);                                    
                                    
                                    ////var pixel = inflatedData.Take(4, i);
                                    //dataRes.WriteByte(pixel[2]);
                                    //newData.WriteByte(pixel[1]);
                                    //newData.WriteByte(pixel[0]);
                                    //newData.WriteByte(pixel[3]);

                                    ////byte tmp = chunkData[i];            // Save blue
                                    ////chunkData[i] = chunkData[i + 2];    // Move red
                                    ////chunkData[i + 2] = tmp;             // Put blue in place
                                    ////i += 4;                             // Skipping to the next RGBA segment, thus 4.
                                }
                            }

                            newData = dataRes.ToArray();
                        }

                        // Compress the image chunk
                        byte[] deflatedData = this.Compress(newData, CompressionLevel.Level0);

                        // Update dat length on chunk header
                        chunk.Header.DataLength = (uint) deflatedData.Length;

                        chunk.Data = deflatedData;

                        // Update CRC
                        chunk.RecomputeCrc();
                    }

                    ////if (chunk.Header.IsChunkType("tEXt"))
                    ////{
                    ////    continue;
                    ////}

                    chunk.WriteTo(writer);
                    newPngData = writerStream.ToArray();

                    // Stop parsing the PNG file.
                    if (chunk.Header.IsChunkType(ChunkTypeImageTrailer))
                    {                        
                        break;
                    }
                }
            }

            this.Data = newPngData;
        }


        /// <summary>
        /// Compresses the specified data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="compressionLevel">The compression level.</param>
        /// <returns></returns>
        private byte[] Compress(byte[] data, CompressionLevel compressionLevel = CompressionLevel.Default)
        {
            using (var output = new MemoryStream())
            {
                using (var deflateStream = new Ionic.Zlib.DeflateStream(output, Ionic.Zlib.CompressionMode.Compress, compressionLevel, true))
                {
                    output.WriteBytes(0x78, 0x01);
                    
                    deflateStream.Write(data, 0, data.Length);                    
                }

                output.WriteBytes(0x1D, 0xE7, 0x4B, 0xE2);

                ////using (var deflateStream = new Ionic.Zlib.GZipStream(output, Ionic.Zlib.CompressionMode.Compress, compressionLevel))
                ////{
                ////    deflateStream.Write(data, 0, data.Length);
                ////}
                
                return output.ToArray();
            }
        }

        private byte[] Decompress(byte[] data)
        {
            using (var inputStream = new MemoryStream(data, false))
            {
                using (var deflateStream = new DeflateStream(inputStream, CompressionMode.Decompress))
                {
                    using (var newData = new MemoryStream())
                    {
                        deflateStream.CopyTo(newData);
                        return newData.ToArray();
                    }
                }            
                
            }
        }

        private byte[] GZInflate(byte[] data)
        {
            var buffer = new byte[256];
            using (var inflate = new MemoryStream())
            {
                using (var inputStream = new Ionic.Zlib.DeflateStream(new MemoryStream(data, false), Ionic.Zlib.CompressionMode.Decompress))
                {
                    int readBytes;
                    while ((readBytes = inputStream.Read(buffer, 0, buffer.Length)) != 0)
                    {
                        inflate.Write(buffer, 0, readBytes);
                    }
                }

                return inflate.ToArray();
            }
        }

        ////private byte[] Deflate(byte[] data, CompressionMode compressionMode)
        ////{
        ////    using (var deflateStream = new DeflateStream( new MemoryStream(data, false), compressionMode))
        ////    {
        ////        using (var newData = new MemoryStream())
        ////        {
        ////            deflateStream.CopyTo(newData);
        ////            return newData.ToArray();
        ////        }
        ////    }            
        ////}

        /// <summary>
        /// A PNG chunk header is made of 8 bytes. The first 4 indicate the length of the chunk data.
        /// The last 4 indicate the chunk type.
        /// </summary>
        private class PngChunkHeader
        {
            internal const int ChunkHeaderSize = ChunkTypeLength + sizeof(UInt32);
            private const int ChunkTypeLength = 4;

            /// <summary>
            /// Initializes a new instance of the <see cref="PngChunkHeader"/> class.
            /// </summary>
            /// <param name="reader">The reader.</param>
            public PngChunkHeader(BinaryReader reader)
            {
                this.DataLength = reader.ReadBigEndianUInt32(); ;

                char[] chunkType = reader.ReadChars(ChunkTypeLength);
                this.ChunkType = new string(chunkType);
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="PngChunkHeader"/> class.
            /// </summary>
            /// <param name="dataLength">Length of the data.</param>
            /// <param name="chunkType">Type of the chunk.</param>
            public PngChunkHeader(UInt32 dataLength, string chunkType)
            {
                this.DataLength = dataLength;
                this.ChunkType = chunkType;
            }

            public uint DataLength { get; set; }

            /// <summary>
            /// Gets the chunk type.
            /// </summary>
            /// <value>
            /// The type of the chunk.
            /// </value>
            public string ChunkType { get; private set; }

            public bool IsChunkType(string chunkType)
            {
                bool match = string.Equals(chunkType, this.ChunkType, StringComparison.Ordinal);
                return match;
            }
        }

        /// <summary>
        /// A PNG chunk is made of a chunk header (8 bytes), its data, and a CRC.
        /// </summary>
        private class PngChunk
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="PngChunk"/> class.
            /// </summary>
            /// <param name="reader">The reader.</param>
            public PngChunk(BinaryReader reader)
            {
                this.Header = new PngChunkHeader(reader);
                this.Data = reader.ReadBytes((int)this.Header.DataLength);
                this.Crc = reader.ReadUInt32();
            }

            public PngChunkHeader Header { get; private set; }
            public byte[] Data { get; set; }
            private UInt32 Crc { get; set; }

            /// <summary>
            /// Writes the chunk to the specified <see cref="BinaryWriter"/>.
            /// </summary>
            /// <param name="writer">The writer.</param>
            /// <param name="data">The chunk data.</param>
            public void WriteTo(EndianBinaryWriter writer)
            {
                writer.Write(this.Data.Length);
                writer.Write(Encoding.ASCII.GetBytes(this.Header.ChunkType));
                writer.Write(this.Data);
                byte[] crc = BitConverter.GetBytes(this.Crc);
                writer.Write(crc);
            }

            ////public void WriteTo(EndianBinaryWriter writer)
            ////{
            ////    WriteTo(writer, this.Data);
            ////}

            /// <summary>
            /// Recomputes the Cyclyic Redundancy Check (CRC) for the current chunk.
            /// </summary>
            public void RecomputeCrc()
            {
                /* CRC is calculated on the preceding bytes in that chunk,
                 * including the chunk type code and chunk data fields,
                 * but not including the length field.
                 */
                
                uint crc = Crc32.ComputeCrc32(Encoding.ASCII.GetBytes(this.Header.ChunkType));
                crc = Crc32.ComputeCrc32(this.Data, crc);
                crc = (uint) ((crc + 0x100000000) % 0x100000000);

                this.Crc = crc;
            }
        }

    }
}