using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace CorporateAppStore.Helpers.CoreFoundation
{
    /// <summary>
    /// Binary PList parser
    /// </summary>
    internal class BinaryPropertyListParser : PropertyListParser
    {
        internal override object Load(string data)
        {
            byte[] byteArray = Encoding.UTF8.GetBytes(data);

            using (MemoryStream stream = new MemoryStream(byteArray))
            {
                return this.Load(stream);
            }
        }

        internal object Load(Stream data)
        {
            // First, we read the trailer: 32 bytes from the end.
            data.Seek(-32, SeekOrigin.End);
            byte[] buffer = new byte[32];
            data.Read(buffer, 0, 32);

            int tableOffset;




            return null;
        }
    }
}
