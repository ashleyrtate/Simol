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

namespace Simol.Security
{
    /// <summary>
    /// Contract for Simol encryption and decryption.
    /// </summary>
    /// <remarks>
    /// Implementations must be safe for use with multiple threads.
    /// </remarks>
    /// <seealso cref="AesEncryptor"/>
    public interface IEncryptor
    {
        /// <summary>
        /// Encrypts the specified plain text.
        /// </summary>
        /// <param name="plainText">The plain text.</param>
        /// <returns></returns>
        byte[] Encrypt(byte[] plainText);

        /// <summary>
        /// Decrypts the specified cypher text.
        /// </summary>
        /// <param name="cypherText">The cypher text.</param>
        /// <returns></returns>
        byte[] Decrypt(byte[] cypherText);
    }
}
