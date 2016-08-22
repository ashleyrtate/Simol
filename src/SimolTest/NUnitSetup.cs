using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.Reflection;

namespace SimolTest
{
    /// <summary>
    /// Performs setup operations required by all fixtures in the test assembly.
    /// </summary>
    [SetUpFixture]
    public class NUnitSetup
    {
        [SetUp]
        public void SetUp()
        {
            Console.WriteLine("Setting up assembly: " + Assembly.GetExecutingAssembly());
            // wrap in try block because errors here are not always logged by NUnit
            try
            {
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }

        [TearDown]
        public void TearDown()
        {
            Console.WriteLine("Tearing down assembly: " + Assembly.GetExecutingAssembly());
        }
    }
}
