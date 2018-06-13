//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Microsoft.SqlTools.ServiceLayer.Admin;
using Microsoft.SqlTools.ServiceLayer.Agent.Contracts;
using Microsoft.SqlTools.ServiceLayer.Management;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    /// <summary>
    /// JobActions provides basic SQL Server Agent Job configuration actions
    /// </summary>
    internal class JobActions : ManagementActionBase
    {
        private JobData data;
        private ConfigAction configAction;

        public JobActions(CDataContainer dataContainer, JobData data, ConfigAction configAction)
        {          
            this.DataContainer = dataContainer;
            this.data = data;
            this.configAction = configAction;
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

            //release the data object
            this.data = null;
        }

        /// <summary>
        /// called by ManagementActionBase.PreProcessExecution
        /// </summary>
        /// <returns>
        /// true if regular execution should take place, false if everything,
        /// has been done by this function
        /// </returns>
        protected override bool DoPreProcessExecution(RunType runType, out ExecutionMode executionResult)
        {
            base.DoPreProcessExecution(runType, out executionResult);

            if (this.configAction == ConfigAction.Drop)
            {
                if (this.data.Job != null)
                {
                    this.data.Job.DropIfExists();                    
                }
            }
            else
            {
                this.data.ApplyChanges(creating: this.configAction == ConfigAction.Create);
                if (!IsScripting(runType)) 
                {
                    this.DataContainer.SqlDialogSubject	= this.data.Job;
                }
            }
            return false;
        }
    }
}
