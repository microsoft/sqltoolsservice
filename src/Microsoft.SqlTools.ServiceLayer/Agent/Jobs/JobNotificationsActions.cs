//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.ServiceLayer.Management;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    /// <summary>
    /// Summary description for JobNotifications.
    /// </summary>
    internal class JobNotificationsActions : ManagementActionBase
    {
        private JobData data;

        public JobNotificationsActions(CDataContainer dataContainer, JobData data)
        {
            this.DataContainer = dataContainer;
            this.data = data;
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
            base.Dispose(disposing);
        }
    }
}
