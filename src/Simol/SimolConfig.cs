/*
 *  Copyright 2008-2011 Ashley Tate
 *  Licensed under the GNU Library General Public License (LGPL) 2.1 
 *  
 *  License available at: http://simol.codeplex.com/license
 */
using Simol.Cache;
using Simol.Consistency;
using Simol.Formatters;
using Simol.Indexing;
using Simol.Security;

namespace Simol
{
    /// <summary>
    /// Options for choosing when to automatically use negative number offsets
    /// for numeric type conversions. 
    /// </summary>
    /// <seealso cref="SimolConfig.OffsetNumbers"/>
    public enum Offset
    {
        /// <summary>
        /// Do not automatically offset any numeric types.
        /// </summary>
        None,
        /// <summary>
        /// Automatically offset signed numeric types.
        /// </summary>
        Signed,
        /// <summary>
        /// Automatically offset all numeric types.
        /// </summary>
        All
    }

    /// <summary>
    /// Options for choosing how to store null property values in SimpleDB.
    /// </summary>
    /// <remarks>
    /// </remarks>
    /// <seealso cref="SimolConfig.NullPutBehavior"/>
    public enum NullBehavior
    {
        /// <summary>
        /// Null values and empty lists are ignored when putting objects into 
        /// SimpleDB.
        /// <para>
        /// When this behavior is selected existing attributes will
        /// NOT be overwritten or deleted if the corresponding 
        /// item object property is null. You will need to explicitly
        /// delete existing values.
        /// </para>
        /// </summary>
        Ignore,
        /// <summary>
        /// Null values and empty lists are always marked with a special character in SimpleDB.
        /// <para>
        /// When this behavior is selected existing attributes 
        /// will always be overwritten with a special character
        /// indicating to Simol that the attribute 
        /// should be treated as null on subsequent Get operations.
        /// </para>
        /// <para>
        /// This makes for simpler data management but
        /// may not be desirable if you have many null fields
        /// or wish to use the "is null" or "is not null" 
        /// select syntax.
        /// </para>
        /// </summary>
        MarkAsNull
    }

    /// <summary>
    /// Options for choosing the consistency of Simol data reads from SimpleDB.
    /// </summary>
    /// <seealso cref="SimolConfig.ReadConsistency"/>
    /// <seealso cref="ConsistentReadScope"/>
    public enum ConsistencyBehavior
    {
        /// <summary>
        /// Simol performs "eventually consistent" reads that may not always reflect the
        /// state of recent write operations.
        /// </summary>
        Eventual,
        /// <summary>
        /// Simol performs only "consistent reads" that always reflect the state of the last 
        /// completed write operation. Performance may be worse than when using 
        /// <see cref="Eventual"/>.
        /// </summary>
        Immediate
    }

    /// <summary>
    /// Holds advanced Simol configuration options.
    /// </summary>
    public class SimolConfig
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SimolConfig"/> class.
        /// </summary>
        public SimolConfig()
        {
            OffsetNumbers = Offset.Signed;
            Cache = new SimpleCache();
            AutoCreateDomains = true;
            NullPutBehavior = NullBehavior.MarkAsNull;
            // Limit values defined by Amazon service
            BatchPutMaxCount = 25;
            BatchDeleteMaxCount = 25;
            MaxAttributeLength = 1024;
            MaxSelectComparisons = 20;
            Formatter = new PropertyFormatter(this);
            Indexer = new LuceneIndexer();
            ReadConsistency = ConsistencyBehavior.Eventual;
            BatchReplaceAttributes = true;
            Encryptor = new AesEncryptor();
        }

        /// <summary>
        /// Gets or sets the cache implementation.
        /// </summary>
        /// <value>The cache.</value>
        /// <remarks>
        /// An instance of <see cref="SimpleCache"/> is installed by default. To disable caching set this
        /// property to <c>null</c>.
        /// </remarks>
        public IItemCache Cache { get; set; }

        /// <summary>
        /// Gets or sets the full-text indexer.
        /// </summary>
        /// <value>The indexer.</value>
        /// <remarks>
        /// An instance of <see cref="LuceneIndexer"/> is installed by default. To disable indexing 
        /// set this property to <c>null</c>.
        /// </remarks>
        public IIndexer Indexer { get; set; }

        /// <summary>
        /// Gets or sets the negative number offset setting to use when formatting numeric types.
        /// </summary>
        /// <value>The offset option to use.</value>
        /// <remarks>
        /// The default value is <see cref="Offset.Signed"/>. For more information on 
        /// negative number offsets see:
        /// <a href="http://docs.amazonwebservices.com/AmazonSimpleDB/latest/DeveloperGuide/NegativeNumbersOffsets.html">
        /// http://docs.amazonwebservices.com/AmazonSimpleDB/latest/DeveloperGuide/NegativeNumbersOffsets.html</a>
        /// </remarks>
        public Offset OffsetNumbers { get; set; }

        /// <summary>
        /// Gets or sets the behavior to use when putting items with null property values.
        /// </summary>
        /// <value>The null put behavior.</value>
        /// <remarks>
        /// The default value is <see cref="NullBehavior.MarkAsNull"/>.
        /// </remarks>
        public NullBehavior NullPutBehavior { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to automatically create domains.
        /// </summary>
        /// <value><c>true</c> if domains should be automatically created when necessary to store objects; otherwise, <c>false</c>.</value>
        /// <remarks>
        /// The default value is <c>true</c>.
        /// </remarks>
        public bool AutoCreateDomains { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of items that may be
        /// updated in a single call to BatchPutAttributes.
        /// </summary>
        /// <value>The maximum number of items.</value>
        /// <remarks>
        /// The default value for this property is 25, which corresponds to the limit defined by the Amazon
        /// service itself. See: <a href="http://docs.amazonwebservices.com/AmazonSimpleDB/latest/DeveloperGuide/SDBLimits.html">
        /// http://docs.amazonwebservices.com/AmazonSimpleDB/latest/DeveloperGuide/SDBLimits.html</a> 
        /// </remarks>
        public int BatchPutMaxCount { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of items that may be
        /// deleted in a single call to BatchDeleteAttributes.
        /// </summary>
        /// <value>The maximum number of items.</value>
        /// <remarks>
        /// The default value for this property is 25, which corresponds to the limit defined by the Amazon
        /// service itself. See: <a href="http://docs.amazonwebservices.com/AmazonSimpleDB/latest/DeveloperGuide/SDBLimits.html">
        /// http://docs.amazonwebservices.com/AmazonSimpleDB/latest/DeveloperGuide/SDBLimits.html</a>
        /// </remarks>
        public int BatchDeleteMaxCount { get; set; }

        /// <summary>
        /// Gets or sets the property formatter used when converting between attribute strings 
        /// and typed property values.
        /// </summary>
        /// <value>The formatter.</value>
        public PropertyFormatter Formatter { get; private set; }

        /// <summary>
        /// Gets or sets the maximum length of a single attribute.
        /// </summary>
        /// <value>The maximum attribute length.</value>
        /// <remarks>
        /// The default value for this property is 1024, which corresponds to the limit defined by the Amazon
        /// service itself. See: <a href="http://docs.amazonwebservices.com/AmazonSimpleDB/latest/DeveloperGuide/SDBLimits.html">
        /// http://docs.amazonwebservices.com/AmazonSimpleDB/latest/DeveloperGuide/SDBLimits.html</a>
        /// </remarks>
        public int MaxAttributeLength { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of comparisions per select expression.
        /// </summary>
        /// <value>The maximum number of comparisons.</value>
        /// <remarks>
        /// The default value for this property is 20, which corresponds to the limit defined by the Amazon
        /// service itself. See: <a href="http://docs.amazonwebservices.com/AmazonSimpleDB/latest/DeveloperGuide/SDBLimits.html">
        /// http://docs.amazonwebservices.com/AmazonSimpleDB/latest/DeveloperGuide/SDBLimits.html</a>
        /// </remarks>
        public int MaxSelectComparisons { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to replace existing attributes when performing BatchPutAttributes operations.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if existing attributes should be replaced; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        /// The default value is <c>true</c>. For improved performance while batch-loading new items set this value to <c>false</c>.
        /// See: <a href="http://docs.amazonwebservices.com/AmazonSimpleDB/latest/DeveloperGuide/SDB_API_BatchPutAttributes.html">
        /// http://docs.amazonwebservices.com/AmazonSimpleDB/latest/DeveloperGuide/SDB_API_BatchPutAttributes.html</a>
        /// </remarks>
        public bool BatchReplaceAttributes { get; set; }

        /// <summary>
        /// Gets or sets the default behavior to use when performing read operations.
        /// </summary>
        /// <value>The read consistency setting.</value>
        /// <remarks>
        /// The default value is <see cref="ConsistencyBehavior.Eventual"/>. Consistent read behavior may be enforced
        /// on a case-by-case basis using <see cref="ConsistentReadScope"/>.
        /// </remarks>
        public ConsistencyBehavior ReadConsistency { get; set; }

        /// <summary>
        /// Gets or sets the encryptor to use when storing encrypted values.
        /// </summary>
        /// <value>The encryptor.</value>
        /// <remarks>
        /// The default encryptor is an instance of <see cref="AesEncryptor"/> which must
        /// be properly configured for use.
        /// </remarks>
        public IEncryptor Encryptor { get; set; }
    }
}