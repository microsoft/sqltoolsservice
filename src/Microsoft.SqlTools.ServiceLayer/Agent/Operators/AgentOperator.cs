using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Threading;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Diagnostics;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo.Agent;
using Microsoft.SqlTools.ServiceLayer.Admin;
using Microsoft.SqlTools.ServiceLayer.Agent.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    /// <summary>
    /// Agent operators management
    /// </summary>
    internal class AgentOperator : AgentConfigurationBase
    {
        private AgentOperatorInfo operatorInfo;

        AgentOperatorsData operatorsData = null;

        /// <summary>
        /// Constructor
        /// </summary>
        public AgentOperator(CDataContainer dataContainer, AgentOperatorInfo operatorInfo)
        {
            try
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

                STParameters parameters = new STParameters();
                parameters.SetDocument(dataContainer.Document);

                string agentOperatorName = null;
                if (parameters.GetParam("operator", ref agentOperatorName))
                {
                    this.operatorsData = new AgentOperatorsData(dataContainer, agentOperatorName);
                }
                else
                {
                    throw new ArgumentNullException("agentOperatorName");
                }
            }
            catch(Exception e)
            {
                throw new ApplicationException("AgentOperatorsSR.FailedToCreateInitializeAgentOperatorDialog", e);
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

        public bool CreateOrUpdate()
        {
            this.operatorsData.ApplyChanges(this.operatorInfo);
            return true;
        }

        /// <summary>
        /// called before dialog's host executes OnReset method on all panels in the dialog one by one
        /// NOTE: it might be called from worker thread
        /// </summary>
        /// <returns>
        /// true if regular execution should take place, false if everything
        /// has been done by this function
        /// </returns>
        /// <returns></returns>
        protected override bool DoPreProcessReset()
        {
            base.DoPreProcessReset();
            try
            {
                if(!this.operatorsData.Creating)
                {
                    DataContainer.Server.JobServer.Operators.Refresh();
                }

                this.operatorsData.Reset();

                // call Reset on all panels
                return true;
            }
            catch(Exception e)
            {
                throw new ApplicationException("AgentOperatorsSR.CannotResetOperator", e);
            }
        }

        #region Private helpers

        /// <summary>
        /// Adds general page to the dialog
        /// </summary>
        // void AddGeneralPage()
        // {
        //     try
        //     {
        //         PanelTreeNode panelTreeNode;

        //         this.agentOperatorsGeneral = new AgentOperatorsGeneral(DataContainer, this.operatorsData);

        //         panelTreeNode = new PanelTreeNode();
        //         panelTreeNode.Text = AgentOperatorsSR.General;
        //         panelTreeNode.Tag = 1;
        //         panelTreeNode.Type = eNodeType.Item;
        //         this.rootNode.Nodes.Add(panelTreeNode);

        //         AddView(this.agentOperatorsGeneral);

        //         SelectNode(panelTreeNode);
        //     }
        //     catch(Exception e)
        //     {
        //         // Wrap it up and go ...
        //         throw new ApplicationException(AgentOperatorsSR.CannotCreateInitializeGeneralPage, e);
        //     }
        // }

        // /// <summary>
        // /// Adds notifications page to the dialog
        // /// </summary>
        // void AddNotificationsPage()
        // {
        //     try
        //     {
        //         PanelTreeNode panelTreeNode;

        //         this.agentOperatorsNotifications = new AgentOperatorsNotifications(DataContainer, this.operatorsData);

        //         panelTreeNode = new PanelTreeNode();
        //         panelTreeNode.Text = AgentOperatorsSR.Notifications;
        //         panelTreeNode.Tag = 2;
        //         panelTreeNode.Type = eNodeType.Item;
        //         rootNode.Nodes.Add(panelTreeNode);

        //         AddView(this.agentOperatorsNotifications);
        //     }
        //     catch(Exception e)
        //     {
        //         // Wrap it up and go ...
        //         throw new ApplicationException(AgentOperatorsSR.CannotCreateInitializeNotificationsPage, e);
        //     }
        // }

        // /// <summary>
        // /// Adds history page to the dialog
        // /// </summary>
        // /// <param name="agentOperator">Agent operator or null if there is no operator</param>
        // void AddHistoryPage()
        // {
        //     try
        //     {
        //         if(this.operatorsData.Creating)
        //             return; // History page can be created only if operator is available

        //         PanelTreeNode panelTreeNode;

        //         this.agentOperatorsHistory = new AgentOperatorsHistory(DataContainer, this.operatorsData);

        //         panelTreeNode = new PanelTreeNode();
        //         panelTreeNode.Text = AgentOperatorsSR.History;
        //         panelTreeNode.Tag = 3;
        //         panelTreeNode.Type = eNodeType.Item;
        //         rootNode.Nodes.Add(panelTreeNode);

        //         AddView(this.agentOperatorsHistory);
        //     }
        //     catch(Exception e)
        //     {
        //         // Wrap it up and go ...
        //         throw new ApplicationException(AgentOperatorsSR.CannotCreateInitializeHistoryPage, e);
        //     }
        // }

        #endregion
    }
}









