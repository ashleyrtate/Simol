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
using System.Security.Cryptography;
using System.IO;
using Coditate.Common.Util;
using Coditate.Common.IO;

namespace Simol.Security
{
    /// <summary>
    /// The default encryptor implementation.
    /// </summary>
    /// <remarks>
    /// This class encrypts data using the AES algorithm with a default key size (128 bits) and block size. You must use the <see cref="GenerateKey"/>
    /// and <see cref="GenerateIV"/> static methods to generate base64 encoded values for use at application run-time.
    /// <para>
    /// All public members of this class are thread-safe.
    /// </para>
    /// </remarks>
    public class AesEncryptor : IEncryptor
    {
        [ThreadStatic]
        private AesCryptoServiceProvider aesProvider;

        /// <summary>
        /// The symmetric key used for encryption and decryption.
        /// </summary>
        /// <value>The key.</value>
        /// <remarks>
        /// Use <see cref="GenerateKey"/> to create a new key.
        /// </remarks>
        /// <seealso cref="SymmetricAlgorithm.Key"/>
        public string Key
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the initialization vector.
        /// </summary>
        /// <value>The initialization vector.</value>
        /// <remarks>
        /// Use <see cref="GenerateIV"/> to create a new IV.
        /// </remarks>
        /// <seealso cref="SymmetricAlgorithm.IV"/>
        public string IV
        {
            get;
            set;
        }

        /// <summary>
        /// Generates a new key.
        /// </summary>
        /// <returns></returns>
        public static string GenerateKey()
        {
            byte[] bytes = new AesCryptoServiceProvider().Key;
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Generates a new initialization vector.
        /// </summary>
        /// <returns></returns>
        public static string GenerateIV()
        {
            byte[] bytes = new AesCryptoServiceProvider().IV;
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Encrypts the specified plain text.
        /// </summary>
        /// <param name="plainText">The plain text.</param>
        /// <returns></returns>
        public byte[] Encrypt(byte[] plainText)
        {
            Arg.CheckNull("plainText", plainText);
            State.CheckPropertyNull("IV", this);
            State.CheckPropertyNull("Key", this);

            var ms = new MemoryStream((int)(plainText.Length * 1.1));
            var aesStream = new CryptoStream(ms, AesProvider.CreateEncryptor(), CryptoStreamMode.Write);
            aesStream.Write(plainText, 0, plainText.Length);
            aesStream.Flush();
            aesStream.Close();

            return ms.ToArray();
        }

        /// <summary>
        /// Decrypts the specified cypher text.
        /// </summary>
        /// <param name="cypherText">The cypher text.</param>
        /// <returns></returns>
        public byte[] Decrypt(byte[] cypherText)
        {
            Arg.CheckNull("cypherText", cypherText);
            State.CheckPropertyNull("IV", this);
            State.CheckPropertyNull("Key", this);

            var ms = new MemoryStream(cypherText);
            var aesStream = new CryptoStream(ms, AesProvider.CreateDecryptor(), CryptoStreamMode.Read);
            try
            {
                var ms2 = new MemoryStream((int)(cypherText.Length * 1.1));
                IOUtils.TransferData(aesStream, ms2);
                return ms2.ToArray();
            }
            finally
            {
                aesStream.Close();
            }
        }

        /// <summary>
        /// Gets the AES provider instance.
        /// </summary>
        /// <value>The AES provider.</value>
        /// <remarks>A new provider instance is created and returned for each thread which accesses this class.</remarks>
        public AesCryptoServiceProvider AesProvider
        {
            get
            {
                InitProvider();
                return aesProvider;
            }
        }

        private void InitProvider()
        {
            if (aesProvider != null)
            {
                return;
            }
            aesProvider = new AesCryptoServiceProvider();
            try
            {
                aesProvider.Key = Convert.FromBase64String(Key);
                aesProvider.IV = Convert.FromBase64String(IV);
            }
            catch (Exception ex)
            {
                string message = string.Format("Invalid Key or IV provided. Key = '{0}', IV = '{1}'", Key, IV);
                throw new SimolConfigurationException(message, ex);
            }
        }
    }
}
