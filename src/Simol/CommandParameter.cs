/*
 *  Copyright 2008-2011 Ashley Tate
 *  Licensed under the GNU Library General Public License (LGPL) 2.1 
 *  
 *  License available at: http://simol.codeplex.com/license
 */
using System.Collections.Generic;
using System.Text;
using Coditate.Common.Util;
using Simol.Formatters;
using System;
using System.Collections;
using System.Linq;

namespace Simol
{
    /// <summary>
    /// Holds information about a single <see cref="SelectCommand"/> parameter.
    /// </summary>
    /// <remarks>
    /// See <see cref="SelectCommand"/> for details on using command parameters with select statements.
    /// </remarks>
    /// <seealso cref="SelectCommand"/>
    public class CommandParameter : ICloneable
    {
        private string propertyName;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandParameter"/> class
        /// using the same value for <see cref="Name"/> and <see cref="PropertyName"/>.
        /// </summary>
        /// <param name="name">The parameter name.</param>
        /// <param name="value">The parameter value. The provided value 
        /// is automatically added to the <see cref="Values"/> list. Optional parameter.</param>
        public CommandParameter(string name, object value)
            : this(name, null, value)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandParameter"/> class
        /// using the same value for <see cref="Name"/> and <see cref="PropertyName"/>
        /// and a list of values.
        /// </summary>
        /// <param name="name">The parameter name.</param>
        /// <param name="values">The parameter values.</param>
        public CommandParameter(string name, IList values)
            : this(name, null, values)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandParameter"/> class
        /// allowing different values for <see cref="Name"/> and <see cref="PropertyName"/>.
        /// </summary>
        /// <param name="name">The parameter name.</param>
        /// <param name="propertyName">Name of the item property to use for formatting purposes. Optional parameter. 
        /// Required only if the item property name and parameter name are different.</param>
        /// <param name="value">The parameter value. The provided value 
        /// is automatically added to the <see cref="Values"/> list. Null is allowed.</param>
        public CommandParameter(string name, string propertyName, object value) : this(name, propertyName, new List<object> { value}) 
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandParameter"/> class
        /// allowing different values for <see cref="Name"/> and <see cref="PropertyName"/> and a list of 
        /// values.
        /// </summary>
        /// <param name="name">The parameter name.</param>
        /// <param name="propertyName">Name of the item property to use for formatting purposes. Optional parameter. 
        /// Required only if the item property name and parameter name are different.</param>
        /// <param name="values">The parameter values.</param>
        public CommandParameter(string name, string propertyName, IList values)
        {
            Arg.CheckNullOrEmpty("name", name);

            Name = name;
            PropertyName = propertyName;
            Values = new List<object>();

            if (values == null)
            {
                Values.Add(null);                
            }
            else
            {
                AddValues(values);
            }
        }

        /// <summary>
        /// Gets or sets the parameter name.
        /// </summary>
        /// <value>The name.</value>
        /// <remarks>
        /// The parameter name must match exactly one parameter
        /// defined in your select query. For example, the select query 
        /// <c>select * from Person where Zipcode = @Zipcode</c> 
        /// would require one parameter named "Zipcode".
        /// </remarks>
        public string Name { get; private set; }

        /// <summary>
        /// Gets or sets the name of the item property to which the parameter
        /// is "bound" for formatting purposes.
        /// </summary>
        /// <value>The name of the property.</value>
        /// <remarks>
        /// By default this value is the same as <see cref="Name"/>. However, 
        /// when <c>Name</c> does not exactly match any
        /// property of your item type or item mapping, you must explicitly 
        /// provide a property name. This property name is used to "bind" 
        /// the parameter to an item property for formatting purposes.
        /// </remarks>
        public string PropertyName
        {
            get
            {
                if (propertyName == null)
                {
                    return Name;
                }
                return propertyName;
            }
            set { propertyName = value; }
        }

        /// <summary>
        /// Gets or sets the parameter values.
        /// </summary>
        /// <value>The parameter values.</value>
        /// <remarks>
        /// If multiple values are provided they will be expanded into a 
        /// comma-delimited list for use with "in" clauses. For example:
        /// <c>select * from Person where FirstName in ('Tom','Dick','Harry')</c>
        /// </remarks>
        public List<object> Values
        {
            get;
            private set;
        }

        /// <summary>
        /// Adds a list of values to the parameter.
        /// </summary>
        /// <param name="values">The values.</param>
        public void AddValues(IList values)
        {
            Arg.CheckNull("values", values);

            foreach (var v in values)
            {
                Values.Add(v);
            }
        }

        internal string ValueString { get; set; }

        internal AttributeMapping Mapping { get; set; }

        internal void ExpandValues(PropertyFormatter formatter)
        {
            if (ValueString != null)
            {
                return;
            }

            var paramBuilder = new StringBuilder(Values.Count*100);
            for (int k = 0; k < Values.Count; k++)
            {
                // quote and escape embedded quotes in expanded param value
                string vString = formatter.ToString(Mapping.Formatter, Values[k]);
                paramBuilder.Append("'");
                paramBuilder.Append(vString);
                paramBuilder.Replace("'", "''", paramBuilder.Length - vString.Length, vString.Length);
                paramBuilder.Append("'");
                if (k < Values.Count - 1)
                {
                    paramBuilder.Append(",");
                }
            }

            ValueString = paramBuilder.ToString();
        }

        internal void Reset()
        {
            Values.Clear();
            ValueString = null;
        }

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        /// A new object that is a copy of this instance.
        /// </returns>
        public object Clone()
        {
            var newCommand = (CommandParameter)MemberwiseClone();
            newCommand.Values = new List<object>();
            return newCommand;
        }
    }
}