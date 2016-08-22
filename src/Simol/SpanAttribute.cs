/*
 *  Copyright 2008-2011 Ashley Tate
 *  Licensed under the GNU Library General Public License (LGPL) 2.1 
 *  
 *  License available at: http://simol.codeplex.com/license
 */
using System;

namespace Simol
{
    /// <summary>
    /// Supports storage of large property values by spanning multiple SimpleDB attributes. Also supports compression and encryption of
    /// data.
    /// </summary>
    /// <remarks>
    /// Use this attribute on string properties to store values larger than the SimpleDB limit
    /// of 1024 bytes per attribute.
    /// 
    /// <para>
    /// Spanned properties may be up to 261,376 bytes in length, depending on whether the item 
    /// contains any other properties. This limit is derived from the SimpleDB limits of 1024 bytes per attribute x 256 attributes per item
    /// with 768 bytes used in overhead. This limit can be increased further by enabling compression, which may allow storage of properties as large as .5 MB. 
    /// </para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class SpanAttribute : SimolAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SpanAttribute"/> class.
        /// </summary>
        public SpanAttribute() : this(false, false)
        {
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="SpanAttribute"/> class.
        /// </summary>
        /// <param name="compress">if set to <c>true</c> compress the data.</param>
        public SpanAttribute(bool compress) : this (compress, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpanAttribute"/> class.
        /// </summary>
        /// <param name="compress">if set to <c>true</c> compress the data.</param>
        /// <param name="encrypt">if set to <c>true</c> encrypt the data.</param>
        public SpanAttribute(bool compress, bool encrypt)
        {
            Compress = compress;
            Encrypt = encrypt;
        }

        /// <summary>
        /// Gets or sets a value indicating whether data should be compressed when stored in SimpleDB.
        /// </summary>
        /// <value><c>true</c> if data should be compressed; otherwise, <c>false</c>.</value>
        public bool Compress { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether data should be encrypted when stored in SimpleDB.
        /// </summary>
        /// <value><c>true</c> if data should be encrypted; otherwise, <c>false</c>.</value>
        public bool Encrypt { get; set; }
    }
}