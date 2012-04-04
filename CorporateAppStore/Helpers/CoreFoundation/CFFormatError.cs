using System;

namespace CorporateAppStore.Helpers.CoreFoundation
{
    internal class CFFormatError : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CFFormatError"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        public CFFormatError(string message)
            : base(message)
        {
        }
    }
}
