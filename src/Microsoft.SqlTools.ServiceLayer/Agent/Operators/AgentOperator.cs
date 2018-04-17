using Microsoft.SqlServer.Management.Sdk.Sfc;
#region using
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
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo.Agent;
using Microsoft.SqlTools.ServiceLayer.Admin;


#endregion

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    #region AgentOperators class

    /// <summary>
    /// Agent operators management dialog
    /// BUGBUG - plush - get rid of the corresponding resx file as it is not needed
    /// </summary>
    internal class AgentOperators : AgentControlBase
    {
        #region Member variables

        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.Container components = null;
        /// <summary>
        /// Agent operator general properties
        /// </summary>
        // private AgentOperatorsGeneral agentOperatorsGeneral = null;
        // /// <summary>
        // /// Agent operator notifications properties
        // /// </summary>
        // private AgentOperatorsNotifications agentOperatorsNotifications = null;
        // /// <summary>
        // /// Agent operator history properties
        // /// </summary>
        // private AgentOperatorsHistory agentOperatorsHistory = null;
        // /// <summary>
        // /// Tree root node
        // /// </summary>
        // private PanelTreeNode rootNode = null;
        /// <summary>
        /// Proxy that performs data manipulation for all tabs.
        /// </summary>
        AgentOperatorsData operatorsData = null;
        #endregion

        #region Constructors
        /// <summary>
        /// Default public constructor
        /// </summary>
        public AgentOperators()
        {
        }
        /// <summary>
        /// Default constructor that will be used to create dialog
        /// </summary>
        /// <param name="dataContainer"></param>
        public AgentOperators(CDataContainer dataContainer)
            : this()
        {
            try
            {
                if(dataContainer == null)
                    throw new ArgumentNullException("dataContainer");

                DataContainer = dataContainer;

                STParameters parameters = new STParameters();

                parameters.SetDocument(dataContainer.Document);

                // this.rootNode = new PanelTreeNode();
                // this.rootNode.Type = eNodeType.Folder;
                // this.rootNode.Tag = 1;

                string agentOperatorName = null;

                if(parameters.GetParam("operator", ref agentOperatorName) == false)
                {
                    this.operatorsData = new AgentOperatorsData(dataContainer);
                }
                else
                {
                    this.operatorsData = new AgentOperatorsData(dataContainer, agentOperatorName);
                }

                // AddGeneralPage();
                // AddNotificationsPage();
                // AddHistoryPage();

                // if(this.operatorsData.Creating)
                // {
                //     this.Text = AgentOperatorsSR.NewOperatorProperties;
                // }
                // else
                // {
                //     this.Text = AgentOperatorsSR.OperatorProperties(agentOperatorName);
                // }

                // this.rootNode.Text = this.Text;

                // AddNode(this.rootNode);
            }
            catch(Exception e)
            {
                throw new ApplicationException("AgentOperatorsSR.FailedToCreateInitializeAgentOperatorDialog", e);
            }
        }

        #endregion

        #region Overrides
        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if(disposing)
            {
                if(components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        #endregion

        #region implementation of the execution logic

        /// <summary>
        /// called by IExecutionAwareSqlControlCollection.PreProcessExecution to enable derived
        /// classes to take over execution of the dialog and do entire execution in this method
        /// rather than having the framework to execute dialog views one by one.
        /// 
        /// NOTE: it might be called from non-UI thread
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

            this.operatorsData.ApplyChanges();

            return false;// do not call RunNow on every panel
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

        #endregion

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

    #endregion
}









