using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading;
using Amazon.SimpleDB;
using Simol.Async;
using Simol.Consistency;
using Simol.Formatters;
using Simol.Indexing;
using System.Collections;
using Simol.Security;

namespace Simol.Examples
{
    /// <summary>
    /// WARNING: This file contains code examples created for the Simol documentation wiki.
    /// 
    /// These examples are maintained only to ensure the correctness of wiki examples and are not meant 
    /// to be run by new developers attempting to understand Simol. Instead you should run Simol.Examples.ConsoleExamples.
    /// </summary>
    public class DocExamples
    {
        private static readonly string awsAccessKeyId = ConfigurationManager.AppSettings["AwsAccessKeyId"];
        private static readonly string awsSecretAccessKey = ConfigurationManager.AppSettings["AwsSecretAccessKey"];

        private static void Main(string[] args)
        {
            //GettingStarted();
            //DefiningMappings();
            //TypeLessAndPartialOperations();
            //AsyncOperations();
            SelectOperations();
            //StoringLargeProperties();
            //FullTextIndexing();
            //VersioningAndConsistency();
        }

        public static void GettingStarted()
        {
            var simol = new SimolClient(awsAccessKeyId, awsSecretAccessKey);
            var person = new Person {Name = "Jack Frost"};
            simol.Put(person);

            {
                var customer = new Customer
                    {Name = "Frank Berry", PhoneNumbers = new List<string> {"770-555-1234", "678-555-5678"}};
                simol.Put(customer);
            }
            {
                var jackId = new Guid("cf5e2f47-99d0-4bdd-86ff-d02d7aaa9e92");
                var frankId = new Guid("50a60862-09a2-450a-8b7d-5d585662990b");
                var jack = simol.Get<Person>(jackId);
                var frank1 = simol.Get<Customer>(frankId);
            }

            {
                var frankId = new Guid("50a60862-09a2-450a-8b7d-5d585662990b");
                var frank2 = simol.Get<Person>(frankId);
            }
            {
                var parameter = new CommandParameter("PhoneNumbers", "678-555-5678");
                string selectQuery = "select * from Person where PhoneNumbers = @PhoneNumbers";
                List<Customer> customers = simol.Select<Customer>(selectQuery, parameter);
            }
            {
                var phoneNumbers = new List<string> { "678-555-5678", "770-555-1234" };
                var parameter = new CommandParameter("PhoneNumbers", phoneNumbers);
                string selectQuery = "select * from Person where PhoneNumbers in (@PhoneNumbers)";
                List<Customer> customers = simol.Select<Customer>(selectQuery, parameter);
            }
            {
                string selectQuery =
                    "select count(*) from Person where Zipcode = @Zipcode and DOB between @StartDate and @EndDate";
                var zipParam = new CommandParameter("Zipcode", "30005");
                var startDateParam = new CommandParameter("StartDate", "BirthDate", new DateTime(2010, 1, 1));
                var endDateParam = new CommandParameter("EndDate", "BirthDate", new DateTime(2010, 2, 1));

                var count = (int) simol.SelectScalar<Customer>(selectQuery, zipParam, startDateParam, endDateParam);
            }
        }

        public static void DefiningMappings()
        {
            var simol = new SimolClient(awsAccessKeyId, awsSecretAccessKey);
            {
                simol.Put(new FeatherWeight {Tons = .00012345});
                simol.Put(new Appointment {});
                simol.Put(new Employee {Email = "abc@123.com"});
            }
            {
                PropertyFormatter formatter = simol.Config.Formatter;
                ITypeFormatter dateFormatter = new MyCustomDateFormatter();
                formatter.SetFormatter(typeof (DateTime), dateFormatter);
            }
            {
                PropertyFormatter formatter = simol.Config.Formatter;
                ITypeFormatter dateFormatter = formatter.GetFormatter(typeof (DateTime));
                string date = dateFormatter.ToString(DateTime.Now);
            }
        }

        public static void TypeLessAndPartialOperations()
        {
            var config = new SimolConfig
                {
                    Cache = null
                };
            var simol = new SimolClient(awsAccessKeyId, awsSecretAccessKey, config);
            {
                var employeeId = new Guid("6c0a37b4-9c59-49ce-95af-d01a66f9605d");
                DateTime hireDate;

                // Load the entire employee object and get the HireDate property
                var e = simol.Get<Employee>(employeeId);
                hireDate = e.HireDate;

                // Load just the HireDate using a partial-object operation
                PropertyValues values1 = simol.GetAttributes<Employee>(employeeId, "HireDate");
                hireDate = (DateTime) values1["HireDate"];

                // Load just the HireDate using a typeless operation
                AttributeMapping idMapping = AttributeMapping.Create("Id", typeof (Guid));
                AttributeMapping hireDateMapping = AttributeMapping.Create("HireDate", typeof (DateTime));
                ItemMapping mapping = ItemMapping.Create("Employee", idMapping);
                mapping.AttributeMappings.Add(hireDateMapping);

                PropertyValues values2 = simol.GetAttributes(mapping, employeeId, "HireDate");
                hireDate = (DateTime) values2["HireDate"];
            }
            {
                var employeeId = new Guid("6c0a37b4-9c59-49ce-95af-d01a66f9605d");
                ItemMapping mapping = ItemMapping.Create(typeof (Employee));
                PropertyValues values = simol.GetAttributes(mapping, employeeId, "HireDate");
            }
            {
                var employeeId = new Guid("6c0a37b4-9c59-49ce-95af-d01a66f9605d");
                ItemMapping mapping = ItemMapping.Create(typeof (Employee));
                PropertyValues values = simol.GetAttributes(mapping, employeeId);
                var e = (Employee) PropertyValues.CreateItem(typeof (Employee), values);
            }
        }

        public static void AsyncOperations()
        {
            var simol = new SimolClient(awsAccessKeyId, awsSecretAccessKey);
            {
                var employeeId = new Guid("6c0a37b4-9c59-49ce-95af-d01a66f9605d");
                IAsyncResult result = simol.BeginGet<Employee>(employeeId, null, null);

                // do something else useful
                var e = simol.EndGet<Employee>(result);
            }
            {
                var employee = new Employee
                    {
                        Id = Guid.NewGuid(),
                        Email = "jed@example.com"
                    };
                IAsyncResult result = simol.BeginPut(new List<Employee>() {employee}, null, null);

                // do something else useful
                simol.EndPut(result);
            }
            {
                AsyncCallback callback = delegate(IAsyncResult result) { simol.EndPut(result); };

                simol.BeginPut(new List<Appointment> {new Appointment()}, callback, null);
                simol.BeginPut(new List<Customer> {new Customer()}, callback, null);
                simol.BeginPut(new List<Employee> {new Employee()}, callback, null);

                // do something else useful
            }
        }

        public static void SelectOperations()
        {
            var simol = new SimolClient(awsAccessKeyId, awsSecretAccessKey);
            {
                var count = (int)simol.SelectScalar<Employee>("select count(*) from Employee");
            }
            {
                string selectText = "select HireDate from Employee where HireDate < @HireDate order by HireDate desc";
                var hireDate =
                    (DateTime)
                    simol.SelectScalar<Employee>(selectText, new CommandParameter("HireDate", DateTime.Today));
            }
            {
                List<Employee> employees = simol.Select<Employee>("select * from Employee");
            }
            {
                List<Employee> employees = simol.Select<Employee>("select * from Employee limit 50");
            }
            {
                var command = new SelectCommand<Employee>("select * from Employee limit 50")
                    {
                        MaxResultPages = 1
                    };
                SelectResults<Employee> results = simol.Select(command);
            }
            {
                var command = new SelectCommand<Employee>("select * from Employee limit 50")
                    {
                        MaxResultPages = 1
                    };
                SelectResults<Employee> results;
                do
                {
                    results = simol.Select(command);
                    command.PaginationToken = results.PaginationToken;

                    // do something important with the results
                } while (results.PaginationToken != null);
            }
            // select count for NextToken
            {
                var selectUtils = new SelectUtils(simol);
                var nextToken = selectUtils.SelectCountNextToken<Employee>("select count(*) from Employee limit 10");

                // IMPORTANT: If there are less than 10 employees the nextToken will be null and the following code should be skipped
                var command = new SelectCommand<Employee>("select * from Employee limit 10")
                {
                    PaginationToken = nextToken
                };
                var employees = simol.Select<Employee>(command);
            }
            // select list of items with list of attributes
            {
                // build list of 1000 employees
                var employees1 = new List<Employee>();
                for (int k = 0; k < 1000; k++)
                {
                    var employee = new Employee
                    {
                        Email = k + "@example.com",
                        Id = Guid.NewGuid()
                    };
                    employees1.Add(employee);
                }

                // put the 1000 employees into SimpleDB and copy their emails into a list
                simol.Put<Employee>(employees1);
                var emails = employees1.Select(e => e.Email).ToList();

                // select all 1000 employees in a single call using the list of emails
                var selectUtils = new SelectUtils(simol);
                var command = new SelectCommand<Employee>("select * from Employee where Email in (@Email)");
                command.AddParameter("Email", emails);
                var employees2 = selectUtils.SelectWithList<Employee, string>(command, emails, "Email");
            }
            // select null value
            {
                var person = new Person
                {
                    Name = null
                };
                simol.Put(person);
                List<Person> peopleWithNullNames = simol.Select<Person>("select * from Person where Name = @Name",
                    new CommandParameter("Name", (object)null));
            }
        }

        public static void StoringLargeProperties()
        {
            var simol = new SimolClient(awsAccessKeyId, awsSecretAccessKey);
            // encryption
            {
                // generate a key and initialization vector
                var key = AesEncryptor.GenerateKey();
                var iv = AesEncryptor.GenerateIV();

                // configure the default encryptor
                var encryptor = (AesEncryptor)simol.Config.Encryptor;
                encryptor.Key = key;
                encryptor.IV = iv;

                // put an encrypted object
                var book = new Book();
                simol.Put(book);
            }
            {
                string content = File.ReadAllText(@"Resources\Acts.txt");
                var book = new Book
                    {
                        Content = content
                    };
                simol.Put(book);
            }
        }

        public static void FullTextIndexing()
        {
            var simol = new SimolClient(awsAccessKeyId, awsSecretAccessKey);
            {
                simol.Config.Indexer.IndexRootPath = @"C:\MySimpleDBIndexes";
                ItemMapping addressMapping = ItemMapping.Create(typeof (Address));

                var builder = new IndexBuilder(simol)
                    {
                        UpdateInterval = TimeSpan.FromSeconds(1)
                    };
                builder.Register(addressMapping);
                builder.Start();
            }
            {
                var address1 = new Address
                    {
                        Id = Guid.NewGuid(),
                        City = "Atlanta",
                        State = "Georgia",
                        ModifiedTime = DateTime.UtcNow,
                        StreetAddresses = new List<string> {"12345 Peachtree Road", "Suite 400"},
                        Zipcode = "30301"
                    };
                simol.Put(address1);

                // sleep while the indexer runs
                Thread.Sleep(TimeSpan.FromSeconds(5));

                List<Address> addresses = simol.Find<Address>(@"StreetAddresses: ""Peachtree""", 0, 1, null);
            }
        }

        public static void VersioningAndConsistency()
        {
            var simol = new SimolClient(awsAccessKeyId, awsSecretAccessKey);

            {
                using (new ConsistentReadScope())
                {
                    var employeeId = new Guid("6c0a37b4-9c59-49ce-95af-d01a66f9605d");
                    var e = simol.Get<Employee>(employeeId);

                    List<Employee> allEmployees = simol.Select<Employee>("select * from Employee");
                }

                List<Employee> mostEmployees = simol.Select<Employee>("select * from Employee");
            }
            {
                var monitor = new WriteMonitor(simol);
                monitor.Start();

                using (var writeScope = new ReliableWriteScope(monitor))
                {
                    var personId = new Guid("6c0a37b4-9c59-49ce-95af-d01a66f9605d");
                    var employee = new Employee();
                    var address = new Address();

                    simol.Put(employee);
                    simol.Put(address);
                    simol.Delete<Person>(personId);

                    writeScope.Commit();
                }
            }
            {
                var appointment = new Appointment
                    {
                        DayOfWeek = 20
                    };
                try
                {
                    simol.Put(appointment);
                }
                catch (InvalidOperationException ex)
                {
                    // prints "'DayOfWeek' value must be in the range 1-7"
                    Console.WriteLine(ex.Message);
                }
            }
            {
                simol.Delete<Account>(new Guid("50b156b6-33cb-4566-930e-0dd8e3f466de"));

                var account1 = new Account
                    {
                        Balance = 1000,
                        Id = new Guid("50b156b6-33cb-4566-930e-0dd8e3f466de")
                    };
                simol.Put(account1);

                var account2 = new Account
                    {
                        Id = account1.Id,
                        Balance = 100
                    };
                try
                {
                    simol.Put(account2);
                }
                catch (AmazonSimpleDBException ex)
                {
                    // prints "ConditionalCheckFailed"
                    Console.WriteLine(ex.ErrorCode);
                }
            }
            {
                var accountId = new Guid("50b156b6-33cb-4566-930e-0dd8e3f466de");
                var account3 = simol.Get<Account>(accountId);

                account3.Balance = 100;
                simol.Put(account3);
            }
        }

        public class Account
        {
            [ItemName]
            public Guid Id { get; set; }

            [Version(VersioningBehavior.AutoIncrementAndConditionallyUpdate)]
            public DateTime ModifiedAt { get; set; }

            public decimal Balance { get; set; }
        }

        public class Address
        {
            [ItemName]
            public Guid Id { get; set; }

            [Index]
            public List<string> StreetAddresses { get; set; }

            [Index]
            public string Zipcode { get; set; }

            [Index]
            public string State { get; set; }

            [Index]
            public string City { get; set; }

            [Version]
            public DateTime ModifiedTime { get; set; }
        }

        [Constraint(typeof (AppointmentConstraint))]
        public class Appointment
        {
            [ItemName]
            public Guid Id { get; set; }

            [NumberFormat(1, 0, false)]
            public byte DayOfWeek { get; set; }
        }

        public class AppointmentConstraint : DomainConstraintBase
        {
            public override void BeforeSave(PropertyValues values)
            {
                var day = (byte) values["DayOfWeek"];
                if (day < 1 || day > 7)
                {
                    throw new InvalidOperationException("'DayOfWeek' value must be in the range 1-7");
                }
            }
        }

        public class Book
        {
            [ItemName]
            public Guid Id { get; set; }

            [Span(true, true)]
            public string Content { get; set; }
        }

        [DomainName("Person")]
        public class Customer : Person
        {
            public List<string> PhoneNumbers { get; set; }

            [AttributeName("DOB")]
            public DateTime? BirthDate { get; set; }

            public string Zipcode { get; set; }

            [SimolExclude]
            public TimeSpan Age
            {
                get { return DateTime.Now - (BirthDate ?? DateTime.Now); }
            }
        }

        public class Employee
        {
            [ItemName]
            public Guid Id { get; set; }

            [CustomFormat("yyyy-MM-dd")]
            public DateTime HireDate { get; set; }

            [CustomFormat(typeof (LowerCaseFormatter))]
            public string Email { get; set; }
        }

        public class FeatherWeight
        {
            [ItemName]
            public Guid Id { get; set; }

            [NumberFormat(0, 12, false)]
            public double Tons { get; set; }
        }

        public class LowerCaseFormatter : ITypeFormatter
        {
            public string ToString(object value)
            {
                return value.ToString().ToLower();
            }

            public object ToType(string valueString, Type expected)
            {
                return valueString;
            }
        }

        public class MyCustomDateFormatter : ITypeFormatter
        {
            public string ToString(object value)
            {
                return value.ToString();
            }

            public object ToType(string valueString, Type expected)
            {
                return DateTime.Parse(valueString);
            }
        }

        public class Person
        {
            public Person()
            {
                Id = Guid.NewGuid();
            }

            public string Name { get; set; }

            [ItemName]
            public Guid Id { get; set; }
        }
    }
}