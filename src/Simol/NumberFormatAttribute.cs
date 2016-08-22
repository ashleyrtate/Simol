/*
 *  Copyright 2008-2011 Ashley Tate
 *  Licensed under the GNU Library General Public License (LGPL) 2.1 
 *  
 *  License available at: http://simol.codeplex.com/license
 */
namespace Simol
{
    /// <summary>
    /// Supports custom formatting of numeric properties.
    /// </summary>
    /// <remarks>
    /// Mark properties with this attribute to override the default formatting and 
    /// conversion rules used for numeric types. See <see cref="ISimol"/> for 
    /// more information on the default number formatting rules.
    /// <para>
    /// The total number of whole and decimal digits requested may not exceed the maximum number of 
    /// digits supported by the property type. Care should be taken when using an offset with 
    /// floating point types. If the number of whole digits requested is too high, precision can be
    /// lost due to application of the offset value during formatting.
    /// </para>
    /// </remarks>
    public class NumberFormatAttribute : SimolFormatAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SimolFormatAttribute"/> class.
        /// </summary>
        /// <param name="wholeDigits">The number of whole digits.</param>
        /// <param name="decimalDigits">The number of decimal digits.</param>
        /// <param name="applyOffset">if set to <c>true</c> apply a negative number offset when formatting the property value.</param>
        public NumberFormatAttribute(byte wholeDigits, byte decimalDigits, bool applyOffset)
        {
            WholeDigits = wholeDigits;
            DecimalDigits = decimalDigits;
            ApplyOffset = applyOffset;
        }

        /// <summary>
        /// Gets or sets the number of whole digits to use when formatting and offsetting 
        /// the property value.
        /// </summary>
        /// <value>The number of whole digits.</value>
        public byte WholeDigits { get; private set; }

        /// <summary>
        /// Gets or sets the number of decimal digits to use when formatting the property value.
        /// </summary>
        /// <value>The number of decimal digits.</value>
        public byte DecimalDigits { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether a negative number offset should be applied
        /// when formatting the property value.
        /// </summary>
        /// <value><c>true</c> if the value should be offset; otherwise, <c>false</c>.</value>
        /// <remarks>
        /// The offset value will be automatically adjusted to exactly accomodate the number of 
        /// whole digits requested.
        /// 
        /// <para>For more information on 
        /// negative number offsets see the SimpleDB developer guide:
        /// http://docs.amazonwebservices.com/AmazonSimpleDB/2007-11-07/DeveloperGuide/NegativeNumbersOffsets.html</para>
        /// </remarks>
        public bool ApplyOffset { get; private set; }
    }
}