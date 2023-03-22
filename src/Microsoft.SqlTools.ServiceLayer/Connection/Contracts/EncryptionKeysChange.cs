//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Connection.Contracts
{

    /// <summary>
    /// Parameters for the MSAL cache encryption key notification.
    /// </summary>
    public class EncryptionKeysChangeParams
    {
        /// <summary>
        /// Buffer encoded IV string for MSAL cache encryption
        /// </summary>
        public string Iv { get; set; }

        /// <summary>
        /// Buffer encoded Key string for MSAL cache encryption
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Default constructor to resolve nullables
        /// </summary>
        /// <param name="key">Key for MSAL cache encryption</param>
        /// <param name="iv">Iv for MSAL cache encryption</param>
        public EncryptionKeysChangeParams(string key, string iv) {
            this.Iv = iv;
            this.Key = key;
        }
    }

    /// <summary>
    /// Defines an event that is sent from the client to notify the encryption keys are changed.
    /// </summary>
    public class EncryptionKeysChangedNotification
    {
        public static readonly
            EventType<EncryptionKeysChangeParams> Type =
            EventType<EncryptionKeysChangeParams>.Create("connection/encryptionKeysChanged");
    }
}
