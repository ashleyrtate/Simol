using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Coditate.Common.Util;
using Simol.TestSupport;
using Coditate.TestSupport;
using NUnit.Framework;
using Simol.Security;
using System.Text;
using System.Diagnostics;

namespace Simol.Cache
{
    [TestFixture]
    public class AesEncryptorTest
    {
        private AesEncryptor encryptor;

        [SetUp]
        public void SetUp()
        {
            string key = AesEncryptor.GenerateKey();
            string iv = AesEncryptor.GenerateIV();

            encryptor = new AesEncryptor()
            {
                Key = key,
                IV = iv
            };
        }

        [Test]
        public void EncryptDecrypt()
        {
            EncryptDecrypt(10000);
        }

        private void EncryptDecrypt(int size)
        {
            string data1 = null;
            lock (this)
            {
                data1 = RandomData.AlphaNumericString(size, true);
            }
            byte[] plainBytes = Encoding.UTF8.GetBytes(data1);

            byte[] cryptoBytes = encryptor.Encrypt(plainBytes);
            plainBytes = encryptor.Decrypt(cryptoBytes);

            string data2 = Encoding.UTF8.GetString(plainBytes);

            Assert.AreEqual(data1, data2);
        }

        [Test]
        public void EncryptDecrypt_MultiThreaded()
        {
            int count = 100;
            int threads = 3;
            Exception error = null;
            ParameterizedThreadStart threadStart = delegate
            {
                for (int k = 0; k < count / threads; k++)
                {
                    try
                    {
                        EncryptDecrypt(100);
                    }
                    catch (Exception ex)
                    {
                        error = ex;
                        break;
                    }
                }
            };

            var testRunner = new TestThreadRunner();
            testRunner.AddThreads(threadStart, null, threads);
            testRunner.Run();

            Assert.IsNull(error);
        }

        [Test, ExpectedException(typeof(SimolConfigurationException), ExpectedMessage = "Invalid Key or IV provided. Key = ''", MatchType = MessageMatch.Contains)]
        public void Encrypt_InvalidKey()
        {
            encryptor.Key = "";
            encryptor.Encrypt(new byte[100]);
        }
    }
}