using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Simol.Formatters;
using Simol.TestSupport;
using NUnit.Framework;
using Coditate.TestSupport;

namespace Simol
{
    [TestFixture]
    public class SelectCommandTest
    {
        private SelectCommand<A> command;
        private PropertyFormatter formatter;
        private CommandParameter parameter;

        [SetUp]
        public void Setup()
        {
            formatter = new PropertyFormatter(new SimolConfig());
            parameter = new CommandParameter("BooleanValue", true);
            command = new SelectCommand<A>("Select from A where BooleanValue = @BooleanValue", parameter);
            command.Formatter = formatter;
        }

        [Test]
        public void ExpandCommand()
        {
            var command1 =
                new SelectCommand<A>(
                    "select from A where IntValue = @IntValue and BooleanValue != @BooleanValue and ShortValue = @CustomName or StringValue = @StringValue");
            command1.AddParameter("IntValue", 1);
            command1.AddParameter(new CommandParameter("BooleanValue", true));
            command1.AddParameter(new CommandParameter("CustomName", "ShortValue", (short)1));
            command1.AddParameter(new CommandParameter("StringValue", "L'l Tim"));
            command1.Formatter = formatter;

            string expanded = command1.ExpandedCommandText;

            Assert.AreEqual(
                "select from A where IntValue = '10000000001' and BooleanValue != 'True' and ShortValue = '100001' or StringValue = 'L''l Tim'",
                expanded);
        }

        /// <summary>
        /// Verifies that command parameter is properly bound to AttributeMapping.PropertyName
        /// when both the SimpleDB attribute name and parameter name have been customized.
        /// </summary>
        [Test]
        public void ExpandCommand_CustomAttributeAndParamName()
        {
            var command1 = new SelectCommand<B>(
                "select from B where CustomAttributeNameInt = @MyParam");
            command1.AddParameter(new CommandParameter("MyParam", "RenamedIntValue", 1));
            command1.Formatter = formatter;

            string expanded = command1.ExpandedCommandText;

            Assert.AreEqual("select from B where CustomAttributeNameInt = '1'", expanded);
        }

        [Test]
        public void ExpandParameter_Cached()
        {
            var parameter = new CommandParameter("test", "value");

            // verify that value string is not expanded unless null
            parameter.ValueString = "abc";
            parameter.ExpandValues(formatter);
            Assert.AreEqual(parameter.ValueString, "abc");
        }

        [Test]
        public void ExpandCommand_MultipleValues()
        {
            parameter.Reset();
            parameter.AddValues(new List<object> { true, false });
            string expanded = command.ExpandedCommandText;

            Assert.AreEqual(
                "Select from A where BooleanValue = 'True','False'",
                expanded);
        }

        [Test]
        public void ExpandCommand_NullValue()
        {
            parameter.Reset();
            parameter.Values.Add(null);
            string expanded = command.ExpandedCommandText;

            Assert.AreEqual(
                "Select from A where BooleanValue = '\0'",
                expanded);
        }

        [Test]
        public void ExpandCommand_ListPropertyWithScalarValue()
        {
            var command1 =
                new SelectCommand<A>(
                    "select from A where IntGenericList = @IntGenericList");
            command1.AddParameter("IntGenericList", 1);
            command1.Formatter = formatter;

            string expanded = command1.ExpandedCommandText;

            Assert.AreEqual(
                "select from A where IntGenericList = '10000000001'",
                expanded);
        }

        [Test]
        public void Clone()
        {
            var command2 = (SelectCommand)command.Clone();

            var result = PropertyMatcher.AreEqual(command, command2);
            Assert.IsTrue(result.Equal, result.Message);
            result = PropertyMatcher.AreEqual(command.Parameters, command2.Parameters, "Values");
            Assert.IsTrue(result.Equal, result.Message);

            // the clone should be an exact copy except for parameter values
            result = PropertyMatcher.AreEqual(command.Parameters[0].Values, command2.Parameters[0].Values);
            Assert.IsFalse(result.Equal);

            // verify that new parameter values lists were created
            Assert.AreEqual(1, command.Parameters[0].Values.Count);
            command2.Parameters[0].Values.Add(true);
            command.Parameters[0].Values.Clear();
            Assert.AreEqual(1, command2.Parameters[0].Values.Count);
        }

        [Test]
        public void RegexMatches()
        {
            string regex = SelectCommand.ParameterFinderRegex;
            string[] inputs = {
                                  "blah @_.0-param1, @param2",
                                  "blah =@param1,@param2 blah",
                                  "blah >@param1,  @param2, @param3",
                                  "blah between @param1 and @param2",
                                  @"blah @param1
                              , @param2, 
                                @param3"
                              };
            string[] expectedOutputs = {
                                           "blah zzz, zzz",
                                           "blah =zzz,zzz blah",
                                           "blah >zzz,  zzz, zzz",
                                           "blah between zzz and zzz",
                                           @"blah zzz
                              , zzz, 
                                zzz"
                                       };

            var outputs = new List<string>();
            for (int k = 0; k < inputs.Length; k++)
            {
                string output = Regex.Replace(inputs[k], regex, "zzz");
                outputs.Add(output);
            }


            for (int k = 0; k < inputs.Length; k++)
            {
                Assert.AreEqual(expectedOutputs[k], outputs[k]);
            }
        }

        [Test]
        public void GetParameters()
        {
            Assert.IsTrue(command.Parameters.Contains(parameter));
        }

        [Test]
        public void Reset()
        {
            Assert.IsTrue(command.ExpandedCommandText.Contains("True"));
            Assert.IsFalse(command.ExpandedCommandText.Contains("False"));

            command.Cancel();

            Assert.IsTrue(command.IsCancelled);

            command.Reset();

            parameter.Values.Add(false);

            Assert.IsTrue(command.ExpandedCommandText.Contains("False"));
            Assert.IsFalse(command.ExpandedCommandText.Contains("True"));
            Assert.IsFalse(command.IsCancelled);
        }

        [Test]
        public void IsCompleteSet()
        {
            command = new SelectCommand<A>("select * from MyDomain");
            Assert.IsTrue(command.IsCompleteSet);

            command = new SelectCommand<A>("  select * from MyDomain");
            Assert.IsTrue(command.IsCompleteSet);

            command = new SelectCommand<A>("select   * from MyDomain");
            Assert.IsTrue(command.IsCompleteSet);

            command = new SelectCommand<A>("select*from MyDomain");
            Assert.IsTrue(command.IsCompleteSet);

            command = new SelectCommand<A>("select MyAttribute from MyDomain");
            Assert.IsFalse(command.IsCompleteSet);

            command = new SelectCommand<A>("select count(*) from MyDomain");
            Assert.IsFalse(command.IsCompleteSet);
        }

        /// <summary>
        /// Tests ability to add a command paramater that references the item name.
        /// </summary>
        [Test]
        public void AddParameterForItemName()
        {
            command = new SelectCommand<A>("Select from A where Id = @ItemName");
            command.AddParameter("ItemName", Guid.Empty);
            command.Formatter = formatter;

            Assert.AreEqual(command.ExpandedCommandText,
                            "Select from A where Id = '00000000-0000-0000-0000-000000000000'");
        }

        [Test,
         ExpectedException(typeof(InvalidOperationException),
             ExpectedMessage =
                 "Unable to add command parameter named 'BooleanValue'. Multiple parameters with the same name are not allowed."
             )]
        public void AddParameter_DuplicateName()
        {
            command.AddParameter(new CommandParameter("BooleanValue", null));
        }

        [Test,
         ExpectedException(typeof(InvalidOperationException),
             ExpectedMessage =
                 "Unable to add command parameter with attribute name of 'BadAttribute'. Command has no properties mapped to that attribute name."
             )]
        public void AddParameter_MissingAttribute()
        {
            command.AddParameter(new CommandParameter("BadAttribute", null));
        }

        [Test, ExpectedException(typeof(InvalidOperationException),
            ExpectedMessage = "Unable to expand command [Select from A where BooleanValue = @BooleanValue]. No values provided for command parameter 'BooleanValue'."
            )]
        public void ExpandCommand_NoParamValues()
        {
            parameter.Values.Clear();
            string expanded = command.ExpandedCommandText;
        }

        [Test,
         ExpectedException(typeof(InvalidOperationException),
             ExpectedMessage =
                 "Unable to expand command [Select from A where BooleanValue = @BooleanValue]. No CommandParameter was registered for parameter '@BooleanValue' found in command text."
             )]
        public void ExpandCommand_MissingParameter()
        {
            var command1 = new SelectCommand<A>("Select from A where BooleanValue = @BooleanValue");
            command1.Formatter = command.Formatter;

            string expanded = command1.ExpandedCommandText;
        }

        [Test,
         ExpectedException(typeof(InvalidOperationException),
             ExpectedMessage =
                 "Unable to expand command [Select from A]. One or more registered CommandParameters were missing from the command text: 'MyParam'"
             )]
        public void ExpandCommand_ExtraParameter()
        {
            var command1 = new SelectCommand<A>("Select from A");
            command1.Formatter = command.Formatter;
            command1.AddParameter(new CommandParameter("MyParam", "BooleanValue", true));

            string expanded = command1.ExpandedCommandText;
        }

        [Test,
         ExpectedException(typeof(InvalidOperationException),
             ExpectedMessage =
                 "Parameter value type does not match the corresponding property type. Parameter 'BooleanValue' value is 'System.String' but property 'Simol.TestSupport.A.BooleanValue' is 'System.Boolean'."
             )]
        public void ExpandCommand_InvalidParameterValue()
        {
            parameter.Values.Add("abc");
            string expanded = command.ExpandedCommandText;
        }
    }
}