/*
 *  Copyright 2008-2011 Ashley Tate
 *  Licensed under the GNU Library General Public License (LGPL) 2.1 
 *  
 *  License available at: http://simol.codeplex.com/license
 */
using System;
using System.IO;
using System.Net;
using Microsoft.Win32;

namespace Simol.Data
{
    /// <summary>
    /// Base class for all data types stored in the SimolSystem domain for use by Simol itself.
    /// </summary>
    [DomainName("SimolSystem")]
    internal abstract class SystemData
    {
        private static Guid? machineGuid;

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemData"/> class.
        /// </summary>
        protected SystemData()
        {
            Id = Guid.NewGuid();
            MachineGuid = GetMachineGuid();
            HostName = GetHostName();
        }

        /// <summary>
        /// Gets or sets the item id.
        /// </summary>
        /// <value>The id.</value>
        [ItemName]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the SimolSystem data type.
        /// </summary>
        /// <value>The data type.</value>
        public string DataType
        {
            get { return GetType().Name; }
            set
            {
                // ignore
            }
        }

        /// <summary>
        /// Gets or sets the MachineGuid of the server where Simol is running.
        /// </summary>
        /// <value>The machine GUID.</value>
        public Guid MachineGuid { get; set; }

        /// <summary>
        /// Gets or sets the hostname of the server where Simol is running.
        /// </summary>
        /// <value>The name of the host.</value>
        public string HostName { get; set; }

        /// <summary>
        /// Gets or sets the date and time when this record was last modified.
        /// </summary>
        /// <value>The modified at.</value>
        [Version(VersioningBehavior.AutoIncrement)]
        public DateTime ModifiedAt { get; set; }

        /// <summary>
        /// Gets the MachineGuid for the current host.
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// This method returns the value stored in the registry at 
        /// "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Cryptography\MachineGuid"
        /// </remarks>
        public static Guid GetMachineGuid()
        {
            if (machineGuid != null)
            {
                return machineGuid.Value;
            }

            string keyName = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Cryptography";
            string valueName = "MachineGuid";
            object value = Registry.GetValue(keyName, valueName, null) ?? "";
            try
            {
                machineGuid = new Guid(value.ToString());
            }
            catch
            {
                // ignore
            }
            if (machineGuid == null)
            {
                string message =
                    string.Format(
                        @"Unable to retrieve host identifier from registry key {0}\{1}. This is usually due to running a 32-bit (x86) process on a 64-bit OS. 
                        You must either run an x64 process or copy the registry key to the corresponding 32-bit shadow registry location 
                        at HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Cryptography\MachineGuid.",
                        keyName, valueName);
                throw new IOException(message);
            }
            return machineGuid.Value;
        }

        /// <summary>
        /// Gets the hostname of the current host.
        /// </summary>
        /// <returns></returns>
        public static string GetHostName()
        {
            return Dns.GetHostName();
        }
    }
}