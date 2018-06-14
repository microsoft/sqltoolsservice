//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Microsoft.SqlTools.ServiceLayer.Management;
using Microsoft.SqlTools.ServiceLayer.Security.Contracts;

namespace  Microsoft.SqlTools.ServiceLayer.Security
{
    internal class CredentialActions : ManagementActionBase
    {

#region Constants
        private const int MAX_SQL_SYS_NAME_LENGTH = 128; // max sql sys name length
#endregion

#region Variables
        private CredentialData credentialData = null;
        private CredentialInfo credential;
        private ConfigAction configAction;
#endregion

#region Constructors / Dispose
        /// <summary>
        /// required when loading from Object Explorer context
        /// </summary>
        /// <param name="context"></param>
        public CredentialActions(
            CDataContainer context,
            CredentialInfo credential,
            ConfigAction configAction)
        {
            this.DataContainer = context;
            this.credential = credential;
            this.configAction = configAction;

            this.credentialData = new CredentialData(context, credential);
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
            if (this.configAction == ConfigAction.Drop)
            {
                if (this.credentialData.Credential != null)
                {
                    this.credentialData.Credential.DropIfExists();
                }
            }
            else
            {
                this.credentialData.SendDataToServer();
            }
        }
    }
}
