//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Microsoft.Kusto.ServiceLayer.Agent.Contracts;
using Microsoft.Kusto.ServiceLayer.Management;

namespace Microsoft.Kusto.ServiceLayer.Agent
{
    /// <summary>
    /// Agent Operators management class
    /// </summary>
    internal class AgentOperatorActions : ManagementActionBase
    {
        private AgentOperatorInfo operatorInfo;
        private AgentOperatorsData operatorsData = null;
        private ConfigAction configAction;

        /// <summary>
        /// Constructor
        /// </summary>
        public AgentOperatorActions(
            CDataContainer dataContainer, 
            AgentOperatorInfo operatorInfo,
            ConfigAction configAction)
        {
            if (dataContainer == null)
            {
                throw new ArgumentNullException("dataContainer");
            }

            if (operatorInfo == null)
            {
                throw new ArgumentNullException("operatorInfo");
            }

            this.operatorInfo = operatorInfo;
            this.DataContainer = dataContainer;
            this.configAction = configAction;

            STParameters parameters = new STParameters();
            parameters.SetDocument(dataContainer.Document);

            string agentOperatorName = null;
            if (parameters.GetParam("operator", ref agentOperatorName))
            {
                this.operatorsData = new AgentOperatorsData(
                    dataContainer, 
                    agentOperatorName, 
                    createMode: configAction == ConfigAction.Create);
            }
            else
            {
                throw new ArgumentNullException("agentOperatorName");
            }
        }

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if(disposing)
            {    
            }
            base.Dispose(disposing);
        }

        // <summary>
        /// called by PreProcessExecution to enable derived classes to take over execution
        /// </summary>
        /// <param name="runType"></param>
        /// <param name="executionResult"></param>
        /// <returns>
        /// true if regular execution should take place, false if everything,
        /// has been done by this function
        /// </returns>
        protected override bool DoPreProcessExecution(RunType runType, out ExecutionMode executionResult)
        {
            base.DoPreProcessExecution(runType, out executionResult);

            if (this.configAction == ConfigAction.Drop)
            {
                var currentOperator = this.operatorsData.Operator;
                if (currentOperator != null)
                {
                    currentOperator.DropIfExists();
                }
            }
            else
            {
                this.operatorsData.ApplyChanges(this.operatorInfo);
            }
            return false;
        }
    }
}
