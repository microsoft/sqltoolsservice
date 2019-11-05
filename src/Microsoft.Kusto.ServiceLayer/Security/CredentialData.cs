//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Security;
using System.Text;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Diagnostics;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.Management;
using Microsoft.SqlTools.ServiceLayer.Security.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Security
{
    internal class CredentialData : IDisposable
    {
        #region Properties
        private string credentialName = string.Empty;
        public string CredentialName 
        { 
            get { return credentialName; } 
            set { credentialName = value; } 
        }

        private string credentialIdentity = string.Empty;
        public string CredentialIdentity 
        { 
            get { return credentialIdentity; } 
            set { credentialIdentity = value; } 
        }

        private SecureString securePassword;
        public SecureString SecurePassword 
        { 
            get { return securePassword; } 
            set 
            { 
                securePassword = value; 
                PasswordWasChanged = true; 
            } 
        }

        private bool isPropertiesMode = false;
        public bool IsPropertiesMode
        {
            get
            {
                return isPropertiesMode;
            }
        }

        private bool passwordWasChanged = false;
        public bool PasswordWasChanged 
        { 
            get { return passwordWasChanged; } 
            set { passwordWasChanged = value; } 
        }

        private bool isEncryptionByProvider = false;
        public bool IsEncryptionByProvider 
        { 
            get { return isEncryptionByProvider; } 
            set { isEncryptionByProvider = value; } 
        }

        private string providerName = string.Empty;
        public string ProviderName 
        { 
            get { return providerName; } 
            set { providerName = value; } 
        }

        public Microsoft.SqlServer.Management.Smo.Credential Credential
        {
            get
            {
                return !string.IsNullOrWhiteSpace(this.CredentialName)
                    ? this.Context.Server.Credentials[this.CredentialName]
                    : null;
            }
        }
        #endregion

        private const string ENUMERATOR_FIELD_IDENTITY = "Identity";
        private const string ENUMERATOR_FIELD_PROVIDER_NAME = "ProviderName";

        #region Constructor
        private CDataContainer context = null;
        private CDataContainer Context { get { return context; } set { context = value; } }
        private CredentialInfo credential;

        public CredentialData(CDataContainer context, CredentialInfo credential)
        {
            this.Context = context;
            this.credential = credential;
            LoadDataFromXmlContext();
        }
        #endregion

        #region Implementation: LoadDataFromXmlContext(), LoadDataFromServer(), SendDataToServer()

        public void Initialize()
        {
            LoadDataFromXmlContext();
            LoadDataFromServer();

            // apply CredentialInfo properties
            this.CredentialIdentity = this.credential.Identity;
            this.CredentialName = this.credential.Name;
            this.SecurePassword = CDataContainer.BuildSecureStringFromPassword(string.Empty);
   
            // need to update only during create time
            this.IsEncryptionByProvider = false;
            if (this.IsEncryptionByProvider)
            {
                this.ProviderName = string.Empty; // lookup provider here
            }
            else
            {
                this.ProviderName = string.Empty;
            }
        }

        /// <summary>
        /// LoadDataFromXmlContext
        /// 
        /// loads context information from xml - e.g. name of object
        /// </summary>
        private void LoadDataFromXmlContext()
        {
            this.CredentialName = this.Context.GetDocumentPropertyString("credential");
            this.isPropertiesMode = (this.CredentialName != null) && (this.CredentialName.Length > 0);
        }

        /// <summary>
        ///  LoadDataFromServer
        ///  
        ///  talks with enumerator an retrieves info that is not available in the xml context but on server
        /// </summary>
        private void LoadDataFromServer()
        {
            if (this.IsPropertiesMode == true)
            {
                bool isKatmaiAndNotMatrix = (this.context.Server.Version.Major >= 10);

                Urn urn = new Urn("Server/Credential[@Name='" + Urn.EscapeString(this.CredentialName) + "']");
                string [] fields;
                if (isKatmaiAndNotMatrix)
                {
                    fields = new string[] { ENUMERATOR_FIELD_IDENTITY, ENUMERATOR_FIELD_PROVIDER_NAME };
                }
                else
                {
                    fields = new string[] { ENUMERATOR_FIELD_IDENTITY };
                }
                Request r = new Request(urn, fields);
                System.Data.DataTable dataTable = Enumerator.GetData(this.Context.ConnectionInfo, r);

                if (dataTable.Rows.Count == 0)
                {
                    throw new Exception(SR.CredentialNoLongerExists);
                }

                System.Data.DataRow dataRow = dataTable.Rows[0];
                this.CredentialIdentity = Convert.ToString(dataRow[ENUMERATOR_FIELD_IDENTITY], System.Globalization.CultureInfo.InvariantCulture);

                if (isKatmaiAndNotMatrix)
                {
                    this.providerName = Convert.ToString(dataRow[ENUMERATOR_FIELD_PROVIDER_NAME], System.Globalization.CultureInfo.InvariantCulture);
                    this.isEncryptionByProvider = !string.IsNullOrEmpty(providerName);
                }
            }
            else
            {
                this.CredentialName = string.Empty;
                this.CredentialIdentity = string.Empty;
                this.providerName = string.Empty;
                this.isEncryptionByProvider = false;
            }

            this.SecurePassword = new SecureString();
            this.PasswordWasChanged = false;
        }

        /// <summary>
        /// SendDataToServer
        /// 
        /// here we talk with server via smo and do the actual data changing
        /// </summary>
        public void SendDataToServer()
        {
            if (this.IsPropertiesMode == true)
            {
                SendToServerAlterCredential();
            }
            else
            {
                SendToServerCreateCredential();
            }
        }

        /// <summary>
        /// create credential - create mode
        /// </summary>
        private void SendToServerCreateCredential()
        {
            Microsoft.SqlServer.Management.Smo.Credential smoCredential = new Microsoft.SqlServer.Management.Smo.Credential (
                this.Context.Server,
                this.CredentialName);
            if (this.isEncryptionByProvider)
            {
                smoCredential.MappedClassType = MappedClassType.CryptographicProvider;
                smoCredential.ProviderName = this.providerName;
            }
            smoCredential.Create(this.CredentialIdentity, this.SecurePassword.ToString());
            GC.Collect(); // this.SecurePassword.ToString() just created an immutable string that lives in memory
        }

        /// <summary>
        /// alter credential - properties mode
        /// </summary>
        private void SendToServerAlterCredential()
        {
            Microsoft.SqlServer.Management.Smo.Credential smoCredential = this.Context.Server.Credentials[this.CredentialName];

            if (smoCredential != null)
            {
                if (this.PasswordWasChanged == false)
                {
                    if (smoCredential.Identity != this.CredentialIdentity)
                    {
                        smoCredential.Alter(this.CredentialIdentity);
                    }
                }
                else
                {
                    smoCredential.Alter(this.CredentialIdentity, this.SecurePassword.ToString());
                    GC.Collect(); // this.SecurePassword.ToString() just created an immutable string that lives in memory
                }
            }
            else
            {
                throw new Exception(SR.CredentialNoLongerExists);
            }
        }
        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            this.SecurePassword.Dispose();
        }

        #endregion
    }
}
