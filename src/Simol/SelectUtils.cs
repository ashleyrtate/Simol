/*
 *  Copyright 2008-2011 Ashley Tate
 *  Licensed under the GNU Library General Public License (LGPL) 2.1 
 *  
 *  License available at: http://simol.codeplex.com/license
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Coditate.Common.Util;
using Simol.Async;

namespace Simol
{
    /// <summary>
    /// Contains methods for performing common, high-level select operations that don't belong on the main Simol API.
    /// </summary>
    public class SelectUtils
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SelectUtils"/> class.
        /// </summary>
        /// <param name="simol">The Simol instance to use.</param>
        public SelectUtils(ISimol simol)
        {
            Arg.CheckNull("simol", simol);
            Arg.CheckNull("simol.Config", simol.Config);

            Simol = simol;
        }

        internal ISimol Simol
        {
            get;
            private set;
        }

        // todo: add selectcountnexttoken methods that accept a SelectCommand

        /// <summary>
        /// Invokes a select COUNT query and returns the NextToken 
        /// from SimpleDb.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="selectStatement">The select statement.</param>
        /// <returns>The NextToken or null if no more results are available</returns>
        /// <remarks>
        /// Use this method when you need to skip over the first results returned by a select query, as 
        /// when displaying paginated data to the user. For example, to skip over the first 10000 items in the 
        /// Employee domain you could invoke this method with following query: "select count(*) from Employee limit 10000".
        /// The returned NextToken can then be used with a standard select query to retrieve the next n-Employees.
        /// </remarks>
        public string SelectCountNextToken<T>(string selectStatement)
        {
            var mapping = ItemMapping.Create(typeof(T));
            return SelectCountNextToken(mapping, selectStatement);
        }

        /// <summary>
        /// Invokes a select COUNT query and returns the NextToken 
        /// from SimpleDb.
        /// </summary>
        /// <param name="mapping">The item mapping.</param>
        /// <param name="selectStatement">The select statement.</param>
        /// <returns>The NextToken or null if no more results are available</returns>
        /// <seealso cref="SelectCountNextToken{T}"/>
        public string SelectCountNextToken(ItemMapping mapping, string selectStatement)
        {
            Arg.CheckNullOrEmpty("selectStatement", selectStatement);
            
            var idMapping = AttributeMapping.Create("Domain", typeof(string));
            var countMapping = ItemMapping.Create(mapping.DomainName, idMapping);
            countMapping.AttributeMappings.Add(AttributeMapping.Create("Count", typeof(uint)));

            var command = new SelectCommand(countMapping, selectStatement)
            {
                MaxResultPages = 1
            };
            uint count = 0;
            string nextToken = null;
            do {
                command.PaginationToken = nextToken;
                SelectResults<PropertyValues> results = Simol.SelectAttributes(command);
                count = (uint)results.Items[0]["Count"];
                nextToken = results.PaginationToken;
            } while(count == 0 && nextToken != null);
            return nextToken;
        }

        /// <summary>
        /// Invokes select queries that use parameter lists (with IN clauses) by splitting the parameter list
        /// across multiple invocations that are invoked in parallel.
        /// </summary>
        /// <typeparam name="T">The item type</typeparam>
        /// <typeparam name="P">The select parameter type</typeparam>
        /// <param name="command">The command.</param>
        /// <param name="paramValues">The param values.</param>
        /// <param name="paramName">Name of the param.</param>
        /// <returns></returns>
        /// <remarks>
        /// It's often necessary to select multiple items from SimpleDB using a list of keys. For example,
        /// "select * from Employee where State in ('GA','VA','WA')". But this is difficult in practice because
        /// the SimpleDB IN clause is limited to 20 values. 
        /// <para><c>SelectWithList</c> allows you to provide a parameterized <see cref="SelectCommand"/>
        /// with an unlimited list of param values. The command is replicated and each instance run in parallel 
        /// against SimpleDB. This effectively lets you use an IN clause with an unlimited 
        /// number of parameters, with just a couple lines of code.</para>
        /// </remarks>
        public List<T> SelectWithList<T, P>(SelectCommand<T> command, List<P> paramValues, string paramName)
        {
            var allValues = SelectAttributesWithList(command, paramValues, paramName);
            var typedValues = new List<T>();
            foreach (var values in allValues)
            {
                typedValues.Add((T)PropertyValues.CreateItem(typeof(T), values));
            }
            return typedValues;
        }

        /// <summary>
        /// Invokes select queries that use parameter lists (with IN clauses) by splitting the parameter list
        /// across multiple invocations that are invoked in parallel.
        /// </summary>
        /// <typeparam name="P">The select parameter type</typeparam>
        /// <param name="command">The command.</param>
        /// <param name="paramValues">The param values.</param>
        /// <param name="paramName">Name of the param.</param>
        /// <returns></returns>
        /// <seealso cref="SelectWithList"/>
        public List<PropertyValues> SelectAttributesWithList<P>(SelectCommand command, List<P> paramValues, string paramName)
        {
            Arg.CheckNull("command", command);
            Arg.CheckNull("paramValues", paramValues);
            Arg.CheckNullOrEmpty("paramName", paramName);

            var allValues = new List<PropertyValues>();
            if (paramValues.Count == 0)
            {
                return allValues;
            }

            var results = new List<IAsyncResult>();
            do
            {
                var currentParams = paramValues.Skip(results.Count * Simol.Config.MaxSelectComparisons).Take(Simol.Config.MaxSelectComparisons).ToList();
                if (!currentParams.Any())
                {
                    break;
                }
                // the formatter on the original command never gets set unless we set it here
                command.Formatter = Simol.Config.Formatter;
                var currentCommand = (SelectCommand)command.Clone();
                currentCommand.Reset();
                var parameter = currentCommand.GetParameter(paramName);
                parameter.AddValues(currentParams.Select(e => e).ToList());
                var result = Simol.BeginSelectAttributes(currentCommand, null, null);
                results.Add(result);
            } while (true);

            foreach (var result in results)
            {
                var values = Simol.EndSelectAttributes(result);
                allValues.AddRange(values);
            }

            return allValues;
        }
    }
}
