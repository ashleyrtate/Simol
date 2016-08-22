using System;
using System.Configuration;
using Amazon.SimpleDB;
using Coditate.Common.Util;
using Coditate.TestSupport;
using NUnit.Framework;
using Simol.Security;

namespace Simol.System
{
    public class SystemTestUtils
    {
        public static double RandomDouble()
        {
            double d = RandomData.Double();
            // constrain doubleValue to ensure it fits in default number of whole and decimal digits
            d = Math.Min(d, 9000000);
            d = Math.Round(d, 8);
            d *= RandomData.Bool() ? -1 : 1;
            return d;
        }

        public static int RandomInt()
        {
            int i = RandomData.Int();
            i *= RandomData.Bool() ? -1 : 1;
            return i;
        }

        public static DateTime RandomDate()
        {
            DateTime dt = RandomData.DateTime(DateTime.Now.AddYears(-100), DateTime.Now.AddYears(100));
            // constrain dateValue to supported precision
            dt = DateUtils.Round(dt, DateRounding.Second);
            return dt;
        }

        public static void AssertEqual(SystemTestItem item1, SystemTestItem item2)
        {
            var result = PropertyMatcher.AreEqual(item1, item2);
            Assert.IsTrue(result.Equal, result.Message);
        }

        public static SimolClient GetSimol()
        {
            string awsAccessKeyId = ConfigurationManager.AppSettings["AwsAccessKeyId"];
            string awsSecretAccessKey = ConfigurationManager.AppSettings["AwsSecretAccessKey"];

            var config = new SimolConfig
            {
                Cache = null
            };
            var encryptor = config.Encryptor as AesEncryptor;
            encryptor.IV = "ATh3Rd41s2XrOdE0sA0Pwg==";
            encryptor.Key = "Jqwq/kd5liS/Be2nvpHBdHvj7w8kvJ19sy99Zj9kItU=";
            var aConfig = new AmazonSimpleDBConfig
            {
                ServiceURL = "http://sdb.amazonaws.com",
                //ServiceURL = "http://sdb.us-west-1.amazonaws.com"
            };
            var simpleDb = new AmazonSimpleDBClient(awsAccessKeyId, awsSecretAccessKey, aConfig);
            SimolClient simol = new SimolClient(simpleDb, config);
            return simol;
        }

        public static void Log(string message, ref DateTime start)
        {
            DateTime end = DateTime.Now;

            Console.WriteLine(message);
            Console.WriteLine("\t" + (end - start).TotalSeconds + " sec");
            start = DateTime.Now;
        }
    }
}