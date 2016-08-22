/*
 *  Copyright 2008-2011 Ashley Tate
 *  Licensed under the GNU Library General Public License (LGPL) 2.1 
 *  
 *  License available at: http://simol.codeplex.com/license
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Coditate.Common.Util;
using Simol.Core;
using Simol.Formatters;

namespace Simol
{
    /// <summary>
    /// Encapsulates advanced options for making select requests.
    /// </summary>
    /// <remarks>
    /// This class supports standard ADO.NET named parameter syntax. Simply prefix 
    /// command parameters in your select statement with '@', and register a <see cref="CommandParameter"/>
    /// for each unique parameter in the statement. Here is an example that uses the 
    /// generic version of <see cref="SelectCommand{T}"/>:
    /// <code>
    ///     string commandText = "select * from Person where Name = @Name";
    ///     SelectCommand&lt;Person&gt; command = new SelectCommand&lt;Person&gt;(commandText);
    ///     command.AddParameter("@Name", "Kate");
    ///     SelectResults&lt;Person&gt; results = simol.Select(command);
    /// </code>
    /// <para>
    /// Parameters are formatted using the mapping rules defined for the related item type.
    /// Each command parameter <em>must</em> therefore be associated with a mapped property.
    /// This association is made automatically when the <c>CommandParameter</c> <see cref="CommandParameter.Name"/>
    /// matches a mapped property. When the parameter name <em>does not</em> match a mapped property
    /// you must set the <c>CommandParameter</c> <see cref="CommandParameter.PropertyName"/> to explicitly create the parameter-property
    /// association. In the example above, the <c>Name</c> parameter would automatically assume the 
    /// formatting rules of <c>Person.Name</c>.
    /// </para>
    /// <para>
    /// For example, the query
    /// <c>select * from Person where Birthday between @MinBirthday and @MaxBirthday</c>
    /// requires that we explicitly map the <c>MinBirthday</c> and <c>MaxBirthday</c> parameters to <c>Person.Birthday</c>. To provide this
    /// mapping you must set <c>CommandParameter.PropertyName"</c> to "Birthday" for both parameters.
    /// </para>
    /// <para>
    /// Other noteworthy considerations: 
    /// <list type="bullet">
    /// <item>
    ///     Parameter values are automatically wrapped by single-quotes and escaped when the command text is 
    ///     expanded (i.e. embedded single quotes are replaced by two single quotes--[O'Doul's] becomes ['O''Doul''s'])
    /// </item>
    /// <item>
    ///     Parameter names may contain only the following characters: a-z, A-Z, 0-9, '_', '-', and '.'
    /// </item>    
    /// </list>
    /// </para>
    /// </remarks>
    /// <seealso cref="SelectCommand{T}"/>
    /// <seealso cref="CommandParameter"/>
    public class SelectCommand : ICloneable
    {
        /// <summary>
        /// Regular expression for finding select parameters in the select query.
        /// </summary>
        internal const string ParameterFinderRegex = @"\B@([\w\.-]+)\b";

        private List<CommandParameter> parameters = new List<CommandParameter>();
        private string expandedCommandText;

        /// <summary>
        /// Initializes a new instance of the <see cref="SelectCommand"/> class.
        /// </summary>
        /// <param name="mapping">The item mapping.</param>
        /// <param name="commandText">The command text.</param>
        /// <param name="parameters">The parameters.</param>
        public SelectCommand(ItemMapping mapping, string commandText, params CommandParameter[] parameters)
        {
            Arg.CheckNull("mapping", mapping);
            Arg.CheckNullOrEmpty("commandText", commandText);
            Arg.CheckNull("parameters", parameters);

            Mapping = mapping;
            CommandText = commandText;
            foreach (CommandParameter param in parameters)
            {
                AddParameter(param);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SelectCommand"/> class.
        /// </summary>
        /// <param name="itemType">The item type.</param>
        /// <param name="commandText">The command text.</param>
        /// <param name="parameters">The parameters.</param>
        public SelectCommand(Type itemType, string commandText, params CommandParameter[] parameters)
            : this(TypeItemMapping.GetMapping(itemType), commandText, parameters)
        {
        }

        /// <summary>
        /// Gets the expanded select command text by replacing named parameters with their formatted values.
        /// </summary>
        /// <value>The expanded command text.</value>
        /// <returns>The expanded command text</returns>
        /// <remarks>
        /// This property may only be invoked after the comment is executed with <see cref="SimolClient"/>.
        /// </remarks>
        public string ExpandedCommandText
        {
            get
            {
                if (expandedCommandText == null)
                {
                    expandedCommandText = ExpandCommand();
                }
                return expandedCommandText;
            }
        }

        /// <summary>
        /// Gets or sets the command text.
        /// </summary>
        /// <value>The command text.</value>
        public string CommandText { get; private set; }

        /// <summary>
        /// Gets or sets the item mapping used for this command
        /// </summary>
        /// <value>The mapping.</value>
        public ItemMapping Mapping { get; private set; }

        /// <summary>
        /// Gets the command parameters.
        /// </summary>
        /// <value>The parameters.</value>
        public IList<CommandParameter> Parameters
        {
            get { return parameters.AsReadOnly(); }
        }

        /// <summary>
        /// Gets the parameter with the specified <see cref="CommandParameter.Name"/>.
        /// </summary>
        /// <param name="name">The parameter or null if no matching parameter is found.</param>
        /// <returns></returns>
        public CommandParameter GetParameter(string name)
        {
            return parameters.Where(p => p.Name == name).FirstOrDefault();
        }

        /// <summary>
        /// Gets or sets the pagination token.
        /// </summary>
        /// <value>The pagination token.</value>
        /// <remarks>
        /// Set to the value obtained from <see cref="SelectResults{T}.PaginationToken"/>
        /// to retrieve the next page of results.
        /// </remarks>
        public string PaginationToken { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of result pages to return.
        /// </summary>
        /// <value>The maximum pages of results.</value>
        /// <remarks>
        /// <para>By default SimpleDB limits the number of items returned for a select
        /// request to a maximum of 100 items. The "limit" keyword allows you to change the
        /// per-request maximum to any value between 1 and 2500. For example: "select * from Person limit 500"
        /// </para>
        /// 
        /// <para>
        /// Simol allows you to collect multiple pages of results in single call by reissuing 
        /// the select command query to SimpleDB until the desired number of results pages have been
        /// retrieved. The default value of <c>MaxResultPages</c> is zero, which will 
        /// return <em>all</em> available results.
        /// </para>
        /// </remarks>
        public int MaxResultPages { get; set; }

        internal bool IsCancelled { get; set; }
        internal PropertyFormatter Formatter { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this command is selecting all available attributes.
        /// </summary>
        internal bool IsCompleteSet { 
            get
            {
                return Regex.IsMatch(CommandText, @"select(\s)*\*", RegexOptions.IgnoreCase);
            } 
        }

        /// <summary>
        /// Resets this instance for reuse with new command parameter values or after cancellation.
        /// </summary>
        public void Reset()
        {
            expandedCommandText = null;
            PaginationToken = null;
            IsCancelled = false;
            foreach (CommandParameter parameter in Parameters)
            {
                parameter.Reset();
            }
        }

        internal void CheckNeedsReset()
        {
            if (expandedCommandText != null)
            {
                string message = "SelectCommand.Reset() must be invoked before command can be reused.";
                throw new InvalidOperationException(message);
            }
        }

        /// <summary>
        /// Cancels execution of this command.
        /// </summary>
        public void Cancel()
        {
            IsCancelled = true;
        }

        /// <summary>
        /// Adds a parameter to this command.
        /// </summary>
        /// <param name="parameter">The parameter to add.</param>
        /// <returns>This command instance.</returns>
        public SelectCommand AddParameter(CommandParameter parameter)
        {
            CommandParameter existing = parameters.Where(c => c.Name == parameter.Name).FirstOrDefault();
            if (existing != null)
            {
                string message =
                    string.Format(
                        "Unable to add command parameter named '{0}'. Multiple parameters with the same name are not allowed.",
                        parameter.Name);
                throw new InvalidOperationException(message);
            }
            AttributeMapping attributeMapping =
                Mapping.AttributeMappings.Where(p => p.PropertyName == parameter.PropertyName).FirstOrDefault();
            if (attributeMapping == null &&
                !string.Equals(Mapping.ItemNameMapping.PropertyName, parameter.PropertyName))
            {
                string message =
                    string.Format(
                        "Unable to add command parameter with attribute name of '{0}'. Command has no properties mapped to that attribute name.",
                        parameter.Name);
                throw new InvalidOperationException(message);
            }
            parameter.Mapping = attributeMapping ?? Mapping.ItemNameMapping;
            parameters.Add(parameter);

            return this;
        }

        /// <summary>
        /// Adds a parameter to this command.
        /// </summary>
        /// <param name="parameterName">Name of the parameter.</param>
        /// <param name="parameterValue">The parameter value.</param>
        /// <returns>This command instance.</returns>
        public SelectCommand AddParameter(string parameterName, object parameterValue)
        {
            var parameter = new CommandParameter(parameterName, parameterValue);
            return AddParameter(parameter);
        }

        private string ExpandCommand()
        {
            State.CheckPropertyNull("Formatter", this);

            foreach (CommandParameter parameter in parameters)
            {
                CheckParamValues(parameter);
            }

            string expandedCommand = Regex.Replace(CommandText, ParameterFinderRegex, ExpandParameter);

            List<string> unusedParamNames =
                Parameters.Where(p => p.ValueString == null).Select(p => "'" + p.Name + "'").ToList();
            if (unusedParamNames.Count > 0)
            {
                string message =
                    string.Format(
                        "Unable to expand command [{0}]. One or more registered {1}s were missing from the command text: {2}",
                        CommandText, typeof(CommandParameter).Name, StringUtils.Join(" ", unusedParamNames));
                throw new InvalidOperationException(message);
            }

            return expandedCommand;
        }

        private void CheckParamValues(CommandParameter parameter)
        {
            if (parameter.Values.Count == 0)
            {
                string message =
                    string.Format(
                        "Unable to expand command [{0}]. No values provided for command parameter '{1}'.",
                        CommandText, parameter.Name);
                throw new InvalidOperationException(message);
            }
            foreach (object value in parameter.Values)
            {
                CheckParamValue(parameter, value);
            }
        }

        private void CheckParamValue(CommandParameter parameter, object value)
        {
            if (value != null && !parameter.Mapping.ScalarType.IsAssignableFrom(value.GetType()))
            {
                string message =
                    string.Format("Parameter value type does not match the corresponding property type. " +
                                  "Parameter '{0}' value is '{1}' but property '{2}' is '{3}'.",
                                  parameter.Name, value.GetType().FullName,
                                  parameter.Mapping.FullPropertyName,
                                  parameter.Mapping.PropertyType);
                throw new InvalidOperationException(message);
            }
        }

        private string ExpandParameter(Match match)
        {
            string paramName = match.Value.Substring(1);
            CommandParameter parameter = parameters.Where(c => c.Name == paramName).FirstOrDefault();

            if (parameter == null)
            {
                string message =
                    string.Format(
                        "Unable to expand command [{0}]. No {1} was registered for parameter '{2}' found in command text.",
                        CommandText,
                        typeof (CommandParameter).Name,
                        match.Value);
                throw new InvalidOperationException(message);
            }

            parameter.ExpandValues(Formatter);
            return parameter.ValueString;
        }

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        /// A new object that is a copy of this instance.
        /// </returns>
        public object Clone()
        {
            var newCommand = (SelectCommand)MemberwiseClone();
            newCommand.parameters = new List<CommandParameter>();
            foreach (var parameter in Parameters)
            {
                var newParameter = (CommandParameter)parameter.Clone();
                newCommand.parameters.Add(newParameter);
            }
            return newCommand;
        }
    }
}