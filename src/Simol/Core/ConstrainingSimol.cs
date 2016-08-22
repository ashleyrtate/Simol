/*
 *  Copyright 2008-2011 Ashley Tate
 *  Licensed under the GNU Library General Public License (LGPL) 2.1 
 *  
 *  License available at: http://simol.codeplex.com/license
 */
using System.Collections.Generic;
namespace Simol.Core
{
    /// <summary>
    /// Decorating simol implementation that applies custom domain constraints when loading/saving/deleting items.
    /// </summary>
    internal class ConstrainingSimol : DecoratingSimol
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConstrainingSimol"/> class.
        /// </summary>
        /// <param name="decoratedSimol">The decorated simol.</param>
        public ConstrainingSimol(ISimolInternal decoratedSimol)
            : base(decoratedSimol)
        {
        }

        public override void PutAttributes(ItemMapping mapping, List<PropertyValues> values)
        {
            foreach (PropertyValues v in values)
            {
                mapping.Constraint.BeforeSave(v);
            }

            base.PutAttributes(mapping, values);
        }

        public override void DeleteAttributes(ItemMapping mapping, List<object> itemNames, List<string> propertyNames)
        {
            foreach (object itemName in itemNames)
            {
                mapping.Constraint.BeforeDelete(itemName, propertyNames);
            }

            base.DeleteAttributes(mapping, itemNames, propertyNames);
        }

        public override PropertyValues GetAttributes(ItemMapping mapping, object itemName, List<string> propertyNames)
        {
            PropertyValues values = base.GetAttributes(mapping, itemName, propertyNames);

            mapping.Constraint.AfterLoad(values);

            return values;
        }

        public override SelectResults<PropertyValues> SelectAttributes(SelectCommand command)
        {
            SelectResults<PropertyValues> results = base.SelectAttributes(command);

            foreach (PropertyValues values in results)
            {
                command.Mapping.Constraint.AfterLoad(values);
            }

            return results;
        }
    }
}