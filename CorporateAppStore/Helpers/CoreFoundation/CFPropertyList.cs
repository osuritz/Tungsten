// Implementation inspired by the ruby version: https://github.com/ckruse/CFPropertyList/blob/master/lib/rbCFPropertyList.rb
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;

namespace CorporateAppStore.Helpers.CoreFoundation
{
    public enum CFPropertyListFormat {
        /// <summary>
        /// Automatic format discovery
        /// </summary>
        Auto = 0,
        
        /// <summary>
        /// Binary format
        /// </summary>
        Binary = 1,

        /// <summary>
        /// XML format
        /// </summary>
        Xml = 2
    }

    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// https://github.com/ckruse/CFPropertyList/blob/master/lib/rbCFPropertyList.rb
    /// </remarks>
    public class CFPropertyList
    {
        private string Data { get; set; }
        
        public static CFPropertyList Read(string file)
        {
            var propertyList = new CFPropertyList(file, CFPropertyListFormat.Auto);

            return propertyList;
        }

        public CFPropertyList(string filename = null, CFPropertyListFormat format = CFPropertyListFormat.Auto, string data = null)
        {
            this.Format = format;
            this.Data = data;

            if (!string.IsNullOrEmpty(filename)) {
                this.LoadFile(filename);
            }

            if (!string.IsNullOrEmpty(data))
            {
                this.LoadFromString(data);
            }
        }

        private CFPropertyListFormat GetFormatFromPlistHeader(string header)
        {
            // What we do now is ugly, but necessary to recognize the file format
            if (header.Length < 8)
            {
                throw new ArgumentException("Header data is too short to determine format type.", "header");
            }
                        
            string filetype = header.Substring(0, 6);
            string version = header.Substring(6, 2);

            if (string.Equals("bplist", filetype, StringComparison.Ordinal))
            {
                if (!string.Equals("00", version))
                {
                    throw new CFFormatError(string.Format("Wrong file version {0}", version));
                }

                return CFPropertyListFormat.Binary;
            }

            return CFPropertyListFormat.Xml;
        }

        /// <summary>
        /// Loads a plist from a string.
        /// </summary>
        /// <param name="data">The string containing the plist.</param>
        private void LoadFromString(string data = null, CFPropertyListFormat format = CFPropertyListFormat.Auto)
        {
            string str = (data == null) ? this.Data : data;
                        
            PropertyListParser parser;
            switch (format)
            {
                case CFPropertyListFormat.Binary:
                case CFPropertyListFormat.Xml:
                    parser = (format == CFPropertyListFormat.Binary) ? (PropertyListParser)new BinaryPropertyListParser() : new XmlPropertyListParser();
                    break;
                                
                case CFPropertyListFormat.Auto:
                    format = GetFormatFromPlistHeader(str);
                    parser = (format == CFPropertyListFormat.Binary) ? (PropertyListParser)new BinaryPropertyListParser() : new XmlPropertyListParser();
                    break;
                               
                default:
                    throw new ArgumentException("Unexpected Property List (plist) format specified.", "format");
            }

            object value = parser.Load(str);
        }

        private void LoadFile(string filename)
        {
            // TODO: Update to determine format from reading file instead.
            string data = File.ReadAllText(filename);
            this.LoadFromString(data: data);
        }

        public CFPropertyListFormat Format { get; set; }
    }
}