/*
 *  Copyright 2008-2011 Ashley Tate
 *  Licensed under the GNU Library General Public License (LGPL) 2.1 
 *  
 *  License available at: http://simol.codeplex.com/license
 */
using Amazon.SimpleDB;
using Coditate.Common.Util;
using System.Collections.Generic;

namespace Simol.Core
{
    /// <summary>
    /// Base class for Simol implementations that decorate other implementations.
    /// </summary>
    internal abstract class DecoratingSimol : ISimolInternal
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DecoratingSimol"/> class.
        /// </summary>
        /// <param name="decoratedSimol">The decorated simol.</param>
        protected DecoratingSimol(ISimolInternal decoratedSimol)
        {
            Arg.CheckNull("decoratedSimol", decoratedSimol);

            DecoratedSimol = decoratedSimol;
        }

        /// <summary>
        /// Gets or sets the decorated simol implementation.
        /// </summary>
        /// <value>The decorated simol.</value>
        public ISimolInternal DecoratedSimol { get; protected set; }

        public virtual void PutAttributes(ItemMapping mapping, List<PropertyValues> values)
        {
            DecoratedSimol.PutAttributes(mapping, values);
        }

        public virtual PropertyValues GetAttributes(ItemMapping mapping, object itemName, List<string> propertyNames)
        {
            return DecoratedSimol.GetAttributes(mapping, itemName, propertyNames);
        }

        public virtual void DeleteAttributes(ItemMapping mapping, List<object> itemNames, List<string> propertyNames)
        {
            DecoratedSimol.DeleteAttributes(mapping, itemNames, propertyNames);
        }

        public virtual SelectResults<PropertyValues> SelectAttributes(SelectCommand command)
        {
            return DecoratedSimol.SelectAttributes(command);
        }

        public virtual object SelectScalar(SelectCommand command)
        {
            return DecoratedSimol.SelectScalar(command);
        }

        /// <summary>
        /// Gets or sets the simple db client.
        /// </summary>
        /// <value>The simple db.</value>
        public AmazonSimpleDB SimpleDB
        {
            get { return DecoratedSimol.SimpleDB; }
        }

        /// <summary>
        /// Gets or sets the configuration settings.
        /// </summary>
        /// <value>The config settings.</value>
        public SimolConfig Config
        {
            get { return DecoratedSimol.Config; }
        }
    }
}