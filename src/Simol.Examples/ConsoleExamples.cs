using System;
using System.Collections.Generic;
using System.Configuration;

namespace Simol.Examples
{
    /// <summary>
    /// To run the Simol samples you must have an Amazon SimpleDB account. Set your Amazon Web Service account identifiers below. 
    /// 
    /// Visit http://aws.amazon.com/ to sign up for an AWS account if you don't already have one.
    /// </summary>
    public class ConsoleExamples
    {
        public static SimolClient Simol;

        private static void Main(string[] args)
        {
            string awsAccessKeyId = ConfigurationManager.AppSettings["AwsAccessKeyId"];
            string awsSecretAccessKey = ConfigurationManager.AppSettings["AwsSecretAccessKey"];

            // Configure Simol instance
            Simol = new SimolClient(awsAccessKeyId, awsSecretAccessKey);


            // add a list of Person items to SimpleDB
            List<PersonItem> people = GetPeople();
            foreach (PersonItem person in people)
            {
                Console.WriteLine("Saving person: " + person);
                Simol.Put(person);
            }
            Console.WriteLine("=========================");


            // Get each person by ItemName
            foreach (PersonItem person in people)
            {
                var firstPerson = Simol.Get<PersonItem>(person.Id);
                Console.WriteLine("Got person: " + firstPerson);
            }
            Console.WriteLine("=========================");


            // Select all people with Weight stored in Kilograms
            IList<PersonItem> kiloPeople =
                Simol.Select<PersonItem>("select * from Person where WeightUnit = @WeightUnit",
                                          new CommandParameter("WeightUnit", WeightUnit.Kilograms));
            Console.WriteLine("The following people have weight stored in Kilograms:");
            foreach (PersonItem person in kiloPeople)
            {
                Console.WriteLine("Person: " + person);
            }
            Console.WriteLine("=========================");


            // Select all people born in the 1980s
            var command =
                new SelectCommand<PersonItem>("select * from Person where BirthDate between @StartDate and @EndDate");
            command.AddParameter(new CommandParameter("StartDate", "BirthDate", new DateTime(1980, 1, 1)));
            command.AddParameter(new CommandParameter("EndDate", "BirthDate", new DateTime(1989, 12, 31)));

            SelectResults<PersonItem> eightiesPeople =
                Simol.Select(command);
            Console.WriteLine("The following people were born in the 1980s:");
            foreach (PersonItem person in eightiesPeople)
            {
                Console.WriteLine("Person: " + person);
            }
            Console.WriteLine("=========================");


            // Delete each person item
            foreach (PersonItem person in people)
            {
                Simol.Delete<PersonItem>(person.Id);
                Console.WriteLine("Deleted person with Id: " + person.Id);
            }
            Console.WriteLine("=========================");

            Console.WriteLine("Press a key to terminate");
            Console.Read();
        }

        private static List<PersonItem> GetPeople()
        {
            var people = new List<PersonItem>();

            people.Add(new PersonItem
                {
                    BirthDate = new DateTime(1972, 1, 15),
                    EmailAddress = "bob@example.com",
                    FirstName = "Bob",
                    Height = 72.5f,
                    HeightUnit = HeightUnit.Inches,
                    Id = Guid.NewGuid(),
                    LastName = "Smith",
                    Weight = 200,
                    WeightUnit = WeightUnit.Pounds
                });

            people.Add(new PersonItem
                {
                    BirthDate = new DateTime(1952, 4, 20),
                    EmailAddress = "mary@example.com",
                    FirstName = "Mary",
                    Height = 1.5f,
                    HeightUnit = HeightUnit.Meters,
                    Id = Guid.NewGuid(),
                    LastName = "Perkins",
                    Weight = 50,
                    WeightUnit = WeightUnit.Kilograms
                });

            people.Add(new PersonItem
                {
                    BirthDate = new DateTime(1980, 12, 25),
                    EmailAddress = "sally@example.com",
                    FirstName = "Sally",
                    Height = 62.5f,
                    HeightUnit = HeightUnit.Inches,
                    Id = Guid.NewGuid(),
                    LastName = "Sanderson",
                    Weight = 134,
                    WeightUnit = WeightUnit.Pounds
                });

            people.Add(new PersonItem
                {
                    BirthDate = new DateTime(1993, 11, 2),
                    EmailAddress = "mike@example.com",
                    FirstName = "Mike",
                    Height = 1.95f,
                    HeightUnit = HeightUnit.Meters,
                    Id = Guid.NewGuid(),
                    LastName = "Black",
                    Weight = 101,
                    WeightUnit = WeightUnit.Kilograms
                });

            return people;
        }
    }

    [DomainName("Person")]
    public class PersonItem
    {
        public PersonItem()
        {
            Id = Guid.NewGuid();
        }

        [ItemName]
        public Guid Id { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string EmailAddress { get; set; }

        public DateTime BirthDate { get; set; }

        public float Height { get; set; }

        public float Weight { get; set; }

        public WeightUnit WeightUnit { get; set; }

        public HeightUnit HeightUnit { get; set; }

        public override string ToString()
        {
            return string.Format("\n\r\tName: \t\t{0}, {1}\n\r\tEmailAddress: \t{2}\n\r\tId: \t\t{3}", LastName,
                                 FirstName,
                                 EmailAddress, Id);
        }
    }

    public enum WeightUnit
    {
        Pounds,
        Kilograms
    }

    public enum HeightUnit
    {
        Inches,
        Meters
    }
}
