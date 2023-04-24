//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.Management;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    internal class AppRoleActions : ManagementActionBase
    {
        private ConfigAction configAction;

        private AppRolePrototype prototype;

        /// <summary>
        /// Handle login create and update actions
        /// </summary>        
        public AppRoleActions(CDataContainer dataContainer, ConfigAction configAction, AppRolePrototype prototype)
        {
            this.DataContainer = dataContainer;
            this.configAction = configAction;
            this.prototype = prototype;
        }

        /// <summary>
        /// called by the management actions framework to execute the action
        /// </summary>
        /// <param name="node"></param>
        public override void OnRunNow(object sender)
        {
            if (this.configAction != ConfigAction.Drop)
            {
                this.prototype.SendDataToServer();
            }
        }
    }
}