//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Xml;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Diagnostics;
using Microsoft.SqlTools.ServiceLayer.Management;
using Microsoft.SqlTools.ServiceLayer.Admin;

namespace  Microsoft.SqlTools.ServiceLayer.Security
{
    internal class Credential : ManagementActionBase
    {

#region Constants
        private const int MAX_SQL_SYS_NAME_LENGTH = 128; // max sql sys name length
#endregion

#region Variables
        private CredentialData credentialData = null;
#endregion

#region Constructors / Dispose
        /// <summary>
        /// required when loading from Object Explorer context
        /// </summary>
        /// <param name="context"></param>
        public Credential(CDataContainer context)
        {
            this.DataContainer = context;
            this.credentialData = new CredentialData(context);
            this.credentialData.Initialize();
        }

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose( bool disposing )
        {
            base.Dispose( disposing );
            if (disposing == true)
            {
                if (this.credentialData != null)
                {
                    this.credentialData.Dispose();
                }
            }
        }
#endregion

        /// <summary>
        /// called on background thread by the framework to execute the action
        /// </summary>
        /// <param name="node"></param>
        public override void OnRunNow(object sender)
        {
            this.credentialData.SendDataToServer();

        }

        /// <summary>
        /// update logic layer based on content of user interface
        /// </summary>
        private void UpdateLogicLayer()
        {
    
            this.credentialData.CredentialName = "this.textBoxCredentialName.Text";
            this.credentialData.CredentialIdentity = "this.textBoxIdentity.Text";


            this.credentialData.SecurePassword = CDataContainer.BuildSecureStringFromPassword("password");
            this.credentialData.SecurePasswordConfirm = CDataContainer.BuildSecureStringFromPassword("password");

            if (this.ServerConnection.ServerVersion.Major >= 10)
            {
                // need to update only during create time
                this.credentialData.IsEncryptionByProvider = true; //this.checkBoxUseProvider.Checked;
                if (this.credentialData.IsEncryptionByProvider)
                {
                    this.credentialData.ProviderName = "this.comboBoxProviderName.SelectedItem.ToString()";
                }
                else
                {
                    this.credentialData.ProviderName = string.Empty;
                }
            }
        }
    }
}
