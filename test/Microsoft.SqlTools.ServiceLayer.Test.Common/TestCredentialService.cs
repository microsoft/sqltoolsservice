//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using System.Globalization;
using Microsoft.SqlTools.ServiceLayer.Credentials;
using Microsoft.SqlTools.ServiceLayer.Credentials.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Test.Common
{
    public class TestCredentialService
    {
        private CredentialService _credentialService = TestServiceProvider.Instance.CredentialService;

        private static TestCredentialService _instance = new TestCredentialService();

        /// <summary>
        /// The singleton instance of the service 
        /// </summary>
        public static TestCredentialService Instance
        {
            get
            {
                return _instance;
            }
        }

        private const string MSSQL_CRED_PREFIX = "Microsoft.SqlTools";
        private const string TEST_CRED_PREFIX = "SqlToolsTestInstance";
        private const string CRED_SEPARATOR = "|";
        private const string CRED_SERVER_PREFIX = "server:";
        private const string CRED_DB_PREFIX = "db:";
        private const string CRED_USER_PREFIX = "user:";
        private const string CRED_ITEMTYPE_PREFIX = "itemtype:";

        /// <summary>
        /// Read Credential for given instance Info. Tries the test credential id and if no password found
        /// will try the MSSQL credential id
        /// </summary>
        public Credential ReadCredential(InstanceInfo connectionProfile)
        {
            var credentialParams = new Credential();
            credentialParams.CredentialId = FormatCredentialIdForTest(connectionProfile);
            Credential credential = _credentialService.ReadCredential(credentialParams);
            if (credential == null || string.IsNullOrEmpty(credential.Password))
            {
                credentialParams.CredentialId = FormatCredentialIdForMsSql(connectionProfile);
                credential = _credentialService.ReadCredential(credentialParams);
            }

            return credential;
        }

        /// <summary>
        /// Stored the credential to credential store using the test prefix
        /// </summary>
        public bool SaveCredential(InstanceInfo connectionProfile)
        {
            Credential credential = new Credential(FormatCredentialIdForTest(connectionProfile), connectionProfile.Password);
            return _credentialService.SaveCredential(credential);
        }

        private string FormatCredentialIdForMsSql(InstanceInfo connectionProfile, string itemType = "Profile")
        {
            return FormatCredentialId(connectionProfile, itemType, MSSQL_CRED_PREFIX);
        }

        private string FormatCredentialIdForTest(InstanceInfo connectionProfile, string itemType = "Profile")
        {
            return FormatCredentialId(connectionProfile, itemType, TEST_CRED_PREFIX);
        }

        private string FormatCredentialId(InstanceInfo connectionProfile, string itemType = "Profile", string credPrefix = TEST_CRED_PREFIX)
        {
            if (!string.IsNullOrEmpty(connectionProfile.ServerName))
            {
                List<string> cred = new List<string>();
                cred.Add(credPrefix);
                AddToList(itemType, CRED_ITEMTYPE_PREFIX, cred);
                AddToList(connectionProfile.ServerName, CRED_SERVER_PREFIX, cred);
                AddToList(connectionProfile.Database, CRED_DB_PREFIX, cred);
                AddToList(connectionProfile.User, CRED_USER_PREFIX, cred);
                return string.Join(CRED_SEPARATOR, cred.ToArray());
            }
            return null;
        }

        private void AddToList(string item, string prefix, List<string> list)
        {
            if (!string.IsNullOrEmpty(item))
            {
                list.Add(string.Format(CultureInfo.InvariantCulture, "{0}{1}", prefix, item));
            }
        }
    }
}
