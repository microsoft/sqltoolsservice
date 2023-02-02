//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.Management;
using Microsoft.SqlTools.ServiceLayer.Security.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Security
{
    internal class UserActions : ManagementActionBase
    {
#region Variables
        private UserPrototypeData userData;
        private UserInfo user;
        private ConfigAction configAction;
#endregion

#region Constructors / Dispose
        /// <summary>
        /// required when loading from Object Explorer context
        /// </summary>
        /// <param name="context"></param>
        public UserActions(
            CDataContainer context,
            UserInfo user,
            ConfigAction configAction)
        {
            this.DataContainer = context;
            this.user = user;
            this.configAction = configAction;

            this.userData = new UserPrototypeData(context);

            // this.credentialData = new CredentialData(context, credential);
            // this.credentialData.Initialize();
        }

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose( bool disposing )
        {
            base.Dispose( disposing );
            if (disposing == true)
            {
                // if (this.userData != null)
                // {
                //     this.userData.Dispose();
                // }
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
                // if (this.credentialData.Credential != null)
                // {
                //     this.credentialData.Credential.DropIfExists();
                // }
            }
            else
            {
                //this.credentialData.SendDataToServer();
            }
        }
    }
}
