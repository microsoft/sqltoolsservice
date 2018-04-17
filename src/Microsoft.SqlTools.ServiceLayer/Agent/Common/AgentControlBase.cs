//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Drawing;
using System.Collections;
using System.Text;
using System.ComponentModel;
using System.Data;
using System.Xml;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Collections.Specialized;

using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Diagnostics;
using Microsoft.SqlTools.ServiceLayer.Admin;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    /// <summary>
    /// base class that can be used to derived from for the main classes [containers] of the dialogs
    /// </summary>
    public class AgentControlBase : IDisposable, ISqlControlCollection
    {
#region Members

        /// <summary>
        /// arrays of panels added to the tree panel form
        /// </summary>
        private ArrayList   viewsArray;

        /// <summary>
        /// array of tree nodes. Their Tag property is supposed to specify index of thr tree panel in viewsArray
        /// </summary>
        private ArrayList   nodesArray;

        /// <summary>
        /// selected node as specified to SelectNode method
        /// </summary>
        //private TreeNode    selectedNode;

        /// <summary>
        /// service provider of our host. We should direct all host-specific requests to the services
        /// implemented by this provider
        /// </summary>
        private IServiceProvider serviceProvider;

        /// <summary>
        /// data container with initialization-related information
        /// </summary>
        private CDataContainer dataContainer;
        //whether we assume complete ownership over it.
        //We set this member once the dataContainer is set to be non-null
        private bool ownDataContainer = true;


        //we cache these interfaces for performance reasons
        // private ILaunchForm cachedLaunchForm = null;
        // private IMessageBoxProvider messageBoxProvider = null;

        //if derived class tries to call a protected method that relies on service provider,
        //and the service provider hasn't been set yet, we will cache the values and will
        //propagate them when we get the provider set
        //private System.Drawing.Icon cachedIcon = null;
        private string cachedCaption = null;

        private PanelExecutionHandler cachedPanelExecutionHandler;


        //SMO Server connection that MUST be used for all enumerator calls
        //We'll get this object out of CDataContainer, that must be initialized
        //property by the initialization code
        private ServerConnection  serverConnection;


#endregion

#region Constructors

        /// <summary>
        /// SqlMgmtTreeViewControl constructor loads the tree image lists sets the last view to zero
        /// and creates the panel view array
        /// </summary>
        public AgentControlBase()
        {
            this.viewsArray     = new ArrayList();
            this.nodesArray     = new ArrayList();
            //this.selectedNode   = null;
        }

#endregion

#region IDisposable implementation

        void IDisposable.Dispose()
        {
            //BUGBUG - do we need finalizer
            Dispose(true);//call protected virtual method
        }

        /// <summary>
        /// do the deterministic cleanup
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            //dispose CDataContainer if needed
            if (this.dataContainer != null)
            {
                if (this.ownDataContainer)
                {
                    this.dataContainer.Dispose();
                }
                this.dataContainer = null;
            }
        }

#endregion

#region IObjectWithSite implementation

        public virtual void SetSite(IServiceProvider sp)
        {         
            if (sp == null)
            {
                throw new ArgumentNullException("sp");
            }

            //allow to be sited only once
            if (this.serviceProvider == null)
            {                             
                //cache the service provider
                this.serviceProvider = sp;             

                //call protected virtual method to enable derived classes to do initialization
                //OnHosted();
            }
        }

#endregion

#region ISqlControlCollection implementation

        // /// <summary>
        // /// accessor to the view info with the given index
        // /// </summary>
        // ViewInfo ISqlControlCollection.GetViewInfo(int index)
        // {
        //     STrace.Params(SqlMgmtDiag.TName, "SqlMgmtTreeViewControl.ISqlControlCollection.GetViewInfo", "index = {0}", index);
        //     return(ViewInfo)this.viewsArray[index];
        // }


        // /// <summary>
        // /// accessor to the tree node with the given index
        // /// </summary>
        // TreeNode ISqlControlCollection.GetTreeNode(int index)
        // {
        //     STrace.Params(SqlMgmtDiag.TName, "SqlMgmtTreeViewControl.ISqlControlCollection.GetTreeNode", "index = {0}", index);
        //     return(TreeNode)this.nodesArray[index];
        // }

        // TreeNode ISqlControlCollection.SelectedNode
        // {
        //     get
        //     {
        //         STrace.Params(SqlMgmtDiag.TName, "SqlMgmtTreeViewControl.ISqlControlCollection.GetSelectedNode", "", null);
        //         return this.selectedNode;
        //     }
        // }


        /// <summary>
        /// How many controls in the collection
        /// </summary>
        // int ISqlControlCollection.ViewsCount
        // {
        //     get
        //     {
        //         STrace.Params(SqlMgmtDiag.TName, "SqlMgmtTreeViewControl.ISqlControlCollection.ViewsCount", 
        //                       "current count = {0}", this.viewsArray.Count);
        //         return this.viewsArray.Count;
        //     }
        // }

        // /// <summary>
        // /// How many TreeNodes are in the collection
        // /// </summary>
        // int ISqlControlCollection.NodesCount
        // {
        //     get
        //     {
        //         STrace.Params(SqlMgmtDiag.TName, "SqlMgmtTreeViewControl.ISqlControlCollection.NodesCount", 
        //                       "current count = {0}", this.nodesArray.Count);
        //         return this.nodesArray.Count;
        //     }
        // }          


#endregion

#region IExecutionAwareSqlControlCollection implementation

        /// <summary>
        /// called before dialog's host executes actions on all panels in the dialog one by one.
        /// If something fails inside this function and the execution should be aborted,
        /// it can either raise an exception [in which case the framework will show message box with exception text] 
        /// or set executionResult out parameter to be ExecutionMode.Failure
        /// NOTE: it might be called from worker thread
        /// </summary>
        /// <param name="executionInfo">information about execution action</param>
        /// <param name="executionResult">result of the execution</param>
        /// <returns>
        /// true if regular execution should take place, false if everything
        /// has been done by this function
        /// NOTE: in case of returning false during scripting operation 
        /// PreProcessExecutionInfo.Script property of executionInfo parameter 
        /// MUST be set by this function [if execution result is success]
        /// </returns>
        public bool PreProcessExecution(PreProcessExecutionInfo executionInfo, out ExecutionMode executionResult)
        {
            //we start from failure
            executionResult = ExecutionMode.Failure;

            //OK, we do server switching for scripting for SQL/Analysis Server execution here
            RunType runType = executionInfo.RunType;
            if (IsScripting(runType))
            {
                if (!PreProcessScripting(executionInfo, out executionResult))
                {
                    return false;
                }
            }

            if (DataContainer != null)
            {
                //we take over execution here. We substitute the server here for AMO and SQL
                //dialogs
                if (DataContainer.ContainerServerType == CDataContainer.ServerType.SQL)
                {                
                    ExecuteForSql(executionInfo, out executionResult);
                    return false;//execution of the entire control was done here
                }
                // else if (Utils.AreExtendedFeaturesAvailable() &&
                //         (DataContainer.ContainerServerType == CDataContainer.ServerType.OLAP))
                // {
                //     ExecuteForOlap(executionInfo, out executionResult);
                //     return false;//execution of the entire control was done here
                // }
            }


            // call virtual function to do regular execution
            return DoPreProcessExecution(executionInfo.RunType, out executionResult);
        }


        /// <summary>
        /// called when the host received Cancel request. NOTE: this method can return while
        /// operation is still being canceled
        /// </summary>
        /// <returns>
        /// true if the host should do standard cancel for the currently running view or
        /// false if the Cancel operation was done entirely inside this method and there is nothing
        /// extra that should be done
        /// </returns>
        // bool IExecutionAwareSqlControlCollection.Cancel()
        // {
        //     STrace.Params(SqlMgmtDiag.TName, "SqlMgmtTreeViewControl.IExecutionAwareSqlControlCollection.Cancel", "", null);

        //     //first, allow derived classes to take over
        //     bool shouldDoStdCancel = DoCancel();

        //     if (shouldDoStdCancel)
        //     {
        //         //we handle cancelling for SQL and OLAP dialogs here
        //         if (DataContainer != null)
        //         {
        //             if ((!Utils.AreExtendedFeaturesAvailable() &&
        //                  DataContainer.ContainerServerType == CDataContainer.ServerType.SQL)
        //                 ||
        //                 (Utils.AreExtendedFeaturesAvailable() &&
        //                 (DataContainer.ContainerServerType == CDataContainer.ServerType.SQL ||
        //                  DataContainer.ContainerServerType == CDataContainer.ServerType.OLAP))
        //                )
        //             {
        //                 STrace.Trace(SqlMgmtDiag.TName, SqlMgmtDiag.LowTrace, 
        //                              "SqlMgmtTreeViewControl.Cancel: detected {0} server type", DataContainer.ContainerServerType);

        //                 //if everything goes OK, Run() method will return with Cancel result
        //                 PanelExecutionHandler.Cancel(this);
        //                 return false;//execution of the entire control was done here
        //             }
        //         }
        //         return true;
        //     }
        //     else
        //     {
        //         return false;
        //     }
        // }


        /// <summary>
        /// called after dialog's host executes actions on all panels in the dialog one by one
        /// NOTE: it might be called from worker thread
        /// </summary>
        /// <param name="executionMode">result of the execution</param>
        /// <param name="runType">type of execution</param>
        // void IExecutionAwareSqlControlCollection.PostProcessExecution(RunType runType, ExecutionMode executionResult)
        // {
        //     STrace.Params(SqlMgmtDiag.TName, "SqlMgmtTreeViewControl.IExecutionAwareSqlControlCollection.PostProcessExecution", 
        //                   "runType = {0}, executionResult = {1}", runType, executionResult);

        //     //delegate to the protected virtual method
        //     DoPostProcessExecution(runType, executionResult);
        // }

        /// <summary>
        /// called before dialog's host executes OnReset method on all panels in the dialog one by one
        /// NOTE: it might be called from worker thread
        /// </summary>
        /// <returns>
        /// true if regular execution should take place, false if everything
        /// has been done by this function
        /// </returns>
        // bool IExecutionAwareSqlControlCollection.PreProcessReset()
        // {
        //     STrace.Params(SqlMgmtDiag.TName, "SqlMgmtTreeViewControl.IExecutionAwareSqlControlCollection.PreProcessReset", "", null);
        //     return DoPreProcessReset();//delegate to the protected virtual method
        // }

        // /// <summary>
        // /// called after dialog's host executes OnReset method on all panels in the dialog one by one
        // /// NOTE: it might be called from worker thread
        // /// </summary>
        // void IExecutionAwareSqlControlCollection.PostProcessReset()
        // {
        //     STrace.Params(SqlMgmtDiag.TName, "SqlMgmtTreeViewControl.IExecutionAwareSqlControlCollection.PostProcessReset", "", null);
        //     DoPostProcessReset();//delegate to the protected virtual method
        // }

#endregion

#region IConnectionInfoProvider implementation

        /// <summary>
        /// should return 2 strings that correspond for server name and extra connection
        /// information for the dialog. 
        /// </summary>
        /// <param name="serverName">server name</param>
        /// <param name="connectionInfo"></param>
        /// <returns></returns>
        // public virtual void GetTextForConnectionInfo(out string serverName, out string connectionInfo)
        // {
        //     STrace.Params(SqlMgmtDiag.TName, "SqlMgmtTreeViewControl.GetTextForConnectionInfo", "", null);

        //     serverName = "";
        //     connectionInfo = "";
        //     if (DataContainer == null)
        //     {
        //         STrace.Assert(false, "SqlMgmtTreeViewControl.GetTextForConnectionInfo: cannot work without DataContainer");
        //         return;
        //     }

        //     if (CDataContainer.ServerType.SQLCE == DataContainer.ContainerServerType)
        //     {
        //         serverName = SR.ConnectionInfoSqlCEServer(DataContainer.SqlCeFileName);
        //         connectionInfo = SR.ConnectionInfoConnectionString1(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}\\{1}", 
        //                                                                           System.Environment.UserDomainName, System.Environment.UserName));
        //     }
        //     else
        //     {
        //         if (DataContainer.Server != null)
        //         {
        //             // sql
        //             serverName = SR.ConnectionInfoServer(DataContainer.ServerName);
        //         }
        //         else if (Utils.AreExtendedFeaturesAvailable() &&
        //             (DataContainer.OlapServerName != null))
        //         {
        //             // olap
        //             serverName = SR.ConnectionInfoServer(DataContainer.OlapServerName);
        //         }

        //         if (DataContainer.ConnectionInfo.UseIntegratedSecurity)
        //         {
        //             connectionInfo = SR.ConnectionInfoConnectionString1(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}\\{1}", 
        //                                                                               System.Environment.UserDomainName, System.Environment.UserName));
        //         }
        //         else
        //         {
        //             connectionInfo = SR.ConnectionInfoConnectionString1(DataContainer.ConnectionInfo.UserName);
        //         }
        //     }
        // }

        // /// <summary>
        // /// called when dialog should show connection information dialog
        // /// </summary>
        // public virtual void DisplayConnectionProperties(System.Windows.Forms.IWin32Window parentWindow)
        // {
        //     STrace.Params(SqlMgmtDiag.TName, "SqlMgmtTreeViewControl.DisplayConnectionProperties", "", null);

        //     if (DataContainer == null)
        //     {
        //         STrace.Assert(false, "SqlMgmtTreeViewControl.DisplayConnectionProperties is called, but DataContainer is null");
        //         return;
        //     }

        //     using (ConnectionProperties cp = new ConnectionProperties(DataContainer))
        //     {
        //         //propagate the background color
        //         if (this.cachedLaunchForm != null)
        //         {
        //             cp.BackColor = this.cachedLaunchForm.BackColor;
        //             cp.Font = this.cachedLaunchForm.Font;
        //         }
        //         cp.SetSite(ServiceProvider);
        //         cp.ShowDialog(parentWindow);
        //     }
        // }
#endregion



    //    #region IDatabaseEngineTypeProvider implementation

        /// <summary>
        /// If connected to Single Server instance , it will return the DatabaseEngineType. Unknown if not connected
        /// </summary>
        /// <returns>DatabaseEngineType</returns>
//         public virtual DatabaseEngineType ConnectedDatabaseEngineType()
//         {
//             if (this.DataContainer == null)
//             {
//                 STrace.Assert(false, "SqlMgmtTreeViewControl.ConnectedDatabaseEngineType is called, but DataContainer is null");
//                 return DatabaseEngineType.Unknown;
//             }
//             if (this.DataContainer.Server != null)
//             {
//                 //Sql
//                 return (this.DataContainer.Server.ConnectionContext.DatabaseEngineType);
//             }
//             return DatabaseEngineType.Unknown;
//         }

//         /// <summary>
//         /// If connected to Single Server instance , it will return the DatabaseEngineEdition. Unknown if not connected
//         /// </summary>
//         /// <returns></returns>
//         public virtual DatabaseEngineEdition ConnectedDatabaseEngineEdition()
//         {
//             if (this.DataContainer == null)
//             {
//                 STrace.Assert(false, "SqlMgmtTreeViewControl.ConnectedDatabaseEngineEdition is called, but DataContainer is null");
//                 return DatabaseEngineEdition.Unknown;
//             }
//             if (this.DataContainer.Server != null)
//             {
//                 //Sql
//                 return (this.DataContainer.Server.ConnectionContext.DatabaseEngineEdition);
//             }
//             return DatabaseEngineEdition.Unknown;
//         }
// #endregion

// #region ICustomAttributeProvider

//         object[] System.Reflection.ICustomAttributeProvider.GetCustomAttributes(bool inherit)
//         {
//             //we merge attributes from 2 sources: type attributes and the derived classes, giving preference
//             //to the derived classes
//             return GetMergedArray(DoGetCustomAttributes(inherit), GetType().GetCustomAttributes(inherit));
//         }

//         object[] System.Reflection.ICustomAttributeProvider.GetCustomAttributes(Type attributeType, bool inherit)
//         {
//             //we merge attributes from 2 sources: type attributes and the derived classes, giving preference
//             //to the derived classes
//             return GetMergedArray(DoGetCustomAttributes(attributeType, inherit), 
//                                   GetType().GetCustomAttributes(attributeType, inherit));
//         }

//         bool System.Reflection.ICustomAttributeProvider.IsDefined(Type attributeType, bool inherit)
//         {
//             //we merge attributes from 2 sources: type attributes and the derived classes, giving preference
//             //to the derived classes
//             if (!DoIsDefined(attributeType, inherit))
//             {
//                 return GetType().IsDefined(attributeType, inherit);
//             }
//             else
//             {
//                 return true;
//             }
//         }


// #endregion

// #region protected virtual methods and properties

        /// <summary>
        /// whether we own our DataContainer or not. Depending on this value it will or won't be
        /// disposed in our Dispose method
        /// </summary>
        protected virtual bool OwnDataContainer
        {
            get
            {
                //by default we own it
                return true;
            }
        }


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
        protected virtual bool DoPreProcessExecution(RunType runType, out ExecutionMode executionResult)
        {
            //ask the framework to do normal execution by calling OnRunNOw methods
            //of the views one by one
            executionResult = ExecutionMode.Success;
            return true; 
        }

//         /// <summary>
//         /// called when the host received Cancel request. NOTE: this method can return while
//         /// operation is still being canceled
//         /// </summary>
//         /// <returns>
//         /// true if the host should do standard cancel for the currently running view or
//         /// false if the Cancel operation was done entirely inside this method and there is nothing
//         /// extra that should be done
//         /// </returns>
//         protected virtual bool DoCancel()
//         {
//             //this class knows nothing about it - derived classes should override it if they
//             //can cancel execution that they do from inside PreProcessExecution
//             return true;
//         }


//         /// <summary>
//         /// called after dialog's host executes actions on all panels in the dialog one by one
//         /// NOTE: it might be called from worker thread
//         /// </summary>
//         /// <param name="executionMode">result of the execution</param>
//         /// <param name="runType">type of execution</param>
//         protected virtual void DoPostProcessExecution(RunType runType, ExecutionMode executionResult)
//         {
//             //nothing to do in the base class
//         }


        /// <summary>
        /// called before dialog's host executes OnReset method on all panels in the dialog one by one
        /// NOTE: it might be called from worker thread
        /// </summary>
        /// <returns>
        /// true if regular execution should take place, false if everything
        /// has been done by this function
        /// </returns>
        /// <returns></returns>
        protected virtual bool DoPreProcessReset()
        {
            if ((this.dataContainer != null) && this.dataContainer.IsNewObject)
            {
                this.dataContainer.Reset();
            }

            return true;
        }

        /// <summary>
        /// called after dialog's host executes OnReset method on all panels in the dialog one by one
        /// NOTE: it might be called from worker thread
        /// </summary>
        protected virtual void DoPostProcessReset()
        {
            //nothing in the base class
        }

//         /// <summary>
//         /// called from IObjectWithSite.SetSite method after we have been initialized with the
//         /// service provider. It is safe to assume for derived classes that all services
//         /// are available while overriding this method
//         /// </summary>
//         protected virtual void OnHosted()
//         {
//             //nothing
//         }

        /// <summary>
        /// Called to intercept scripting operation
        /// </summary>
        /// <param name="executionInfo"></param>
        /// <param name="executionResult"></param>
        /// <returns>
        /// true if regular execution should take place, false the script
        /// has been created by this function
        /// </returns>
        protected virtual bool PreProcessScripting(PreProcessExecutionInfo executionInfo, out ExecutionMode executionResult)
        {
            //we don't do anything here, but we enable derived classes to do something...
            executionResult = ExecutionMode.Success;
            return true;
        }


// #region ICustomAttributeProvider helpers

//         protected virtual object[] DoGetCustomAttributes(bool inherit)
//         {
//             return GetMergedArray(DoGetCustomAttributes(typeof(ScriptTypeAttribute), inherit), 
//                                   DoGetCustomAttributes(typeof(DialogScriptableAttribute), inherit));
//         }

//         protected virtual object[] DoGetCustomAttributes(Type attributeType, bool inherit)
//         {
//             //if the type specifies this attribute, we don't bother - it overrides
//             //our behavior
//             object[] typeAttribs = GetType().GetCustomAttributes(attributeType, inherit);
//             if (typeAttribs != null && typeAttribs.Length > 0)
//             {
//                 return null;
//             }

//             //we expose default custom attribute for script type
//             if (attributeType.Equals(typeof(ScriptTypeAttribute)))
//             {
//                 string scriptType = ScriptType;
//                 if (scriptType != null)
//                 {
//                     return new object[] {new ScriptTypeAttribute(scriptType)};
//                 }
//                 else
//                 {
//                     return null;
//                 }
//             }
//             else if (attributeType.Equals(typeof(DialogScriptableAttribute)))
//             {
//                 bool canScriptToWindow = true;
//                 bool canScriptToFile = true;
//                 bool canScriptToClipboard = true;
//                 bool canScriptToJob = true;

//                 GetScriptableOptions(out canScriptToWindow, 
//                                      out canScriptToFile, 
//                                      out canScriptToClipboard,
//                                      out canScriptToJob);

//                 return new object[] {new DialogScriptableAttribute(canScriptToWindow, 
//                                                                    canScriptToFile, 
//                                                                    canScriptToClipboard,
//                                                                    canScriptToJob)};
//             }

//             return null;
//         }

//         protected virtual bool DoIsDefined(Type attributeType, bool inherit)
//         {
//             return false;
//         }


//         /// <summary>
//         /// detects whether script types are applicable for this dlg or not. By default 
//         /// the framework relies on DialogScriptableAttribute set on the dlg class and won't  
//         /// call this method if the attribute is specified
//         /// By default we assume that all script types are enabled
//         /// </summary>
//         /// <param name="?"></param>
//         /// <param name="?"></param>
//         /// <param name="?"></param>
//         protected virtual void GetScriptableOptions(out bool canScriptToWindow, 
//                                                     out bool canScriptToFile, 
//                                                     out bool canScriptToClipboard,
//                                                     out bool canScriptToJob)
//         {
//             canScriptToWindow = canScriptToFile = canScriptToClipboard = canScriptToJob = true;
//         }


// #endregion

// #endregion

// #region protected methods

        /// <summary>
        /// CDataContainer accessor
        /// </summary>
        protected CDataContainer DataContainer
        {
            get
            {
                return this.dataContainer;
            }
            set
            {
                this.dataContainer = value;
                this.ownDataContainer = OwnDataContainer; //cache the value
            }
        }


//         /// <summary>
//         /// SMO Server connection that MUST be used for all enumerator calls
//         /// We'll get this object out of CDataContainer, that must be initialized
//         /// property by the initialization code
//         /// </summary>
//         protected ServerConnection ServerConnection
//         {
//             get
//             {
//                 if (this.serverConnection == null && this.dataContainer != null && 
//                     this.dataContainer.ContainerServerType == CDataContainer.ServerType.SQL)
//                 {
//                     this.serverConnection = this.dataContainer.ServerConnection;
//                     STrace.Assert(this.serverConnection != null);
//                 }

//                 //it CAN be null here if dataContainer hasn't been set or the container type is non SQL
//                 return this.serverConnection;
//             }
//         }




//         protected IServiceProvider ServiceProvider
//         {
//             get
//             {
//                 if (this.serviceProvider == null)
//                 {
//                     STrace.Assert(false, "Cannot work without service provider!");
//                     STrace.LogExThrow();
//                     //BUGBUG - should we have our own exception here?
//                     throw new InvalidOperationException();
//                 }

//                 return this.serviceProvider;
//             }
//         }

//         /// <summary>
//         /// should be used when a dialog needs to show another dialog that has
//         /// to show the message box. In this case this interface should be
//         /// passed in for showing it
//         /// </summary>
//         protected IMessageBoxProvider MessageBoxProvider 
//         {
//             get 
//             {
//                 if (this.messageBoxProvider == null)
//                 {
//                     this.messageBoxProvider = (IMessageBoxProvider)this.serviceProvider.GetService(typeof(IMessageBoxProvider));
//                 }
//                 STrace.Assert(this.messageBoxProvider != null);
//                 return this.messageBoxProvider;
//             }
//         }


//         /// <summary>
//         /// synonim to Text property
//         /// </summary>
//         protected string Title
//         {
//             get
//             {
//                 return Text;
//             }

//             set
//             {
//                 Text = value;
//             }
//         }


//         /// <summary>
//         /// gets/sets text for the dialog that we aggregate information about
//         /// </summary>
//         protected string Text
//         {
//             get
//             {
//                 if (this.cachedLaunchForm != null)
//                 {
//                     return this.cachedLaunchForm.Caption;
//                 }
//                 else
//                 {
//                     if (this.cachedCaption != null)
//                     {
//                         return this.cachedCaption;
//                     }
//                     else
//                     {
//                         STrace.Assert(false, "SqlMgmtTreeViewControl.Text getter must be called only after its setter has been called");
//                         return "";
//                     }
//                 }
//             }

//             set
//             {
//                 if (this.cachedLaunchForm != null)
//                 {
//                     this.cachedLaunchForm.Caption = value;
//                 }
//                 else
//                 {
//                     //cache it and propagate when the service provider becomes available
//                     this.cachedCaption = value;
//                 }
//             }
//         }

//         /// <summary>
//         /// top most container icon to be used.
//         /// </summary>
//         protected Icon Icon
//         {
//             get
//             {
//                 if (this.cachedLaunchForm != null)
//                 {
//                     return this.cachedLaunchForm.Icon;
//                 }
//                 else
//                 {
//                     STrace.Assert(false, "SqlMgmtTreeViewControl.Icon getter must be called after service provider becomes available");
//                     return null;
//                 }
//             }
//             set
//             {
//                 if (this.cachedLaunchForm != null)
//                 {
//                     this.cachedLaunchForm.Icon = value;
//                 }
//                 else
//                 {
//                     //cache it and propagate when the service provider becomes available
//                     this.cachedIcon = value;
//                 }
//             }
//         }


//         /// <summary>
//         /// retrieves the panels view count
//         /// </summary>
//         /// <returns></returns>
//         protected int GetViewCount()
//         {
//             return this.viewsArray.Count;
//         }


//         /// <summary>
//         /// Sets the initialized flag for a given view. Usually only tree panel form should do this, 
//         /// however for performance reasons a panel could call it also if needed
//         /// </summary>
//         /// <param name="Index"></param>
//         protected void SetViewInitialized(int Index)
//         {
//             ViewInfo            vi; 

//             vi = (ViewInfo) this.viewsArray[Index];
//             vi.Initialized = true;
//         }


//         /// <summary>
//         /// returns the panel form interface corresponding to a given view index
//         /// </summary>
//         /// <param name="Index"></param>
//         /// <returns></returns>
//         protected IPanelForm GetViewPanelForm(int Index)
//         {
//             return((ViewInfo)this.viewsArray[Index]).PanelForm;
//         }



//         /// <summary>
//         /// Selects a node in the tree, usually the initial node
//         /// </summary>
//         /// <param name="node"></param>
//         protected void SelectNode(TreeNode node)
//         {
//             this.selectedNode = node;
//         }


//         /// <summary>
//         /// adds a node to the tree usually is done at initialization time when
//         /// the treepanel form is build
//         /// </summary>
//         /// <param name="node"></param>
//         protected void AddNode(TreeNode node)
//         {
//             this.nodesArray.Add(node);  
//         }


//         /// <summary>
//         /// adds a panel form user control to views collection. 
//         /// </summary>
//         /// <param name="uc"></param>
//         protected void AddView(UserControl uc)
//         {
//             this.viewsArray.Add(new ViewInfo(uc));
//         }

//         /// <summary>
//         /// display an exception message box as result of a generated exception
//         /// </summary>
//         /// <param name="e"></param>
//         /// <returns></returns>
//         protected DialogResult DisplayExceptionMessage(Exception e)
//         {
//             return MessageBoxProvider.ShowMessage(e, null, ExceptionMessageBoxButtons.OK, ExceptionMessageBoxSymbol.Error, null);
//         }


//         //BUGBUG - remove it. It was doing nothing...
//         protected void InitFormLayout() {}


//         /// <summary>
//         /// returns combination of the given 2 arrays
//         /// </summary>
//         /// <param name="array1"></param>
//         /// <param name="array2"></param>
//         /// <returns></returns>
//         protected object[] GetMergedArray(object[] array1, object[] array2)
//         {
//             if (array1 == null)
//             {
//                 return array2;
//             }
//             else if (array2 == null)
//             {
//                 return array1;
//             }
//             else
//             {
//                 object[] finalReturnValue = new object[array1.Length + array2.Length];
//                 array1.CopyTo(finalReturnValue, 0);
//                 array2.CopyTo(finalReturnValue, array1.Length);

//                 return finalReturnValue;
//             }
//         }


        /// <summary>
        /// checks whether given run time represents one of scripting options
        /// </summary>
        /// <param name="runType"></param>
        /// <returns></returns>
        protected static bool IsScripting(RunType runType)
        {
            return(runType != RunType.RunNow && runType != RunType.RunNowAndExit);
        }


        /// <summary>
        /// calls DoPreProcessExecution and if it returns false, then it will execute all initialized views
        /// one by one. 
        /// This method allows derived classes to do both preprocessing and normal execution in a way
        /// that the framework would normally do. Use this method for special cases when derived class
        /// should really handle entire execution while doing exactly the same actions as the framework
        /// would do
        /// </summary>
        /// <param name="runType"></param>
        /// <returns>
        /// result of the execution. It will let exception fly out if it was raised during execution
        /// </returns>
        protected ExecutionMode DoPreProcessExecutionAndRunViews(RunType runType)
        {          
            ExecutionMode executionResult;
            if (DoPreProcessExecution(runType, out executionResult))
            {
                //true return value means that we need to do execution ourselves
                executionResult = PanelExecutionHandler.Run(runType, this);
            }

            return executionResult;
        }


// #endregion

// #region private methods

        /// <summary>
        /// returns internal helper class that we delegate execution of the panels one by one when 
        /// we do it ourselves during scripting
        /// </summary>
        private PanelExecutionHandler PanelExecutionHandler
        {
            get
            {
                if (this.cachedPanelExecutionHandler == null)
                {
                    if (this.serviceProvider == null)
                    {                   
                        throw new InvalidOperationException("SRError.UnableToExecute");
                    }                  
                    this.cachedPanelExecutionHandler = new PanelExecutionHandler(this, this.serviceProvider);
                }
                return this.cachedPanelExecutionHandler;
            }
        }


//         /// <summary>
//         /// returns string that we can specify into ILaunchFormHost.ScriptTo* methods
//         /// We use DataContainer to decide whether it is SQL or XMLA. In all other cases
//         /// we return null and derived classes should do it on their own
//         /// </summary>
//         private string ScriptType
//         {
//             get
//             {
//                 if (DataContainer == null)
//                 {
//                     return null;
//                 }


//                 if (DataContainer.ContainerServerType == CDataContainer.ServerType.SQL)
//                 {
//                     STrace.Trace(SqlMgmtDiag.TName, SqlMgmtDiag.LowTrace, "SqlMgmtTreeViewControl.ScriptType: returning sql");
//                     return "sql";
//                 }
//                 else if (Utils.AreExtendedFeaturesAvailable() &&
//                     (DataContainer.ContainerServerType == CDataContainer.ServerType.OLAP))
//                 {
//                     STrace.Trace(SqlMgmtDiag.TName, SqlMgmtDiag.LowTrace, "SqlMgmtTreeViewControl.ScriptType: returning xmla");
//                     return "xmla";
//                 }
//                 else
//                 {
//                     STrace.Trace(SqlMgmtDiag.TName, SqlMgmtDiag.LowTrace, "SqlMgmtTreeViewControl.ScriptType: returning null");
//                     return null;
//                 }
//             }

//         }



        /// <summary>
        /// determines whether we need to substitute SMO/AMO server objects with the
        /// temporary ones while doing scripting
        /// </summary>
        /// <returns></returns>
        private bool NeedToSwitchServer
        {
            get
            {
                ServerSwitchingAttribute switchingAttrib = 
                    Utils.GetCustomAttribute(this, typeof(ServerSwitchingAttribute)) as ServerSwitchingAttribute;

                if (DataContainer == null)
                {
                    if (switchingAttrib != null)
                    {
                        return switchingAttrib.NeedToSwtichServerObject;
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }
                }

                if (DataContainer.ContainerServerType == CDataContainer.ServerType.SQL)
                {
                    if (switchingAttrib != null)
                    {
                        return switchingAttrib.NeedToSwtichServerObject;
                    }
                    else
                    {
                        return true;//switch by default in SQL case
                    }
                }               
                else
                {
                    throw new InvalidOperationException();
                }
            }
        }

        public int ViewsCount => throw new NotImplementedException();

        public int NodesCount => throw new NotImplementedException();

        protected virtual ServerConnection GetServerConnectionForScript()
        {
            return this.dataContainer.Server.ConnectionContext;
        }

        /// <summary>
        /// builds a script string from SMO string collections. This function should
        /// probably be moved to SMO. Also we need a way to specify the tsql batch
        /// separator which for now is GO
        /// </summary>
        /// <returns></returns>
        private string BuildSqlScript()
        {
            SqlSmoObject sqlDialogSubject = null;
            try
            {
                sqlDialogSubject = this.DataContainer.SqlDialogSubject;
            }
            catch (System.Exception)
            {
                //We may not have a valid dialog subject here (such as if the object hasn't been created yet)
                //so in that case we'll just ignore it as that's a normal scenario. 
            }

            StringCollection sc = GetServerConnectionForScript().CapturedSql.Text;
            //Scripting may happen on either the server ExecutionManager or the
            //ExecutionManager of the object itself. So we make sure to check
            //the subject text if the server ExecutionManager didn't have any
            //scripts after doing the scripting
            if (sc.Count == 0 && sqlDialogSubject != null)
            {
                sc = sqlDialogSubject.ExecutionManager.ConnectionContext.CapturedSql.Text;
            }
            int                 i;
            StringBuilder       script  = new StringBuilder(4096);  

            if (sc != null)
            {
                for (i = 0; i < sc.Count; i ++)
                {
                    script.AppendFormat("{0}\r\nGO\r\n", sc[i].ToString());
                }
            }

            return script.ToString();
        }


        /// <summary>
        /// called when we need to script a Sql server dlg.
        /// </summary>
        /// <param name="executionInfo"></param>
        /// <param name="executionResult"></param>
        private void ExecuteForSql(PreProcessExecutionInfo executionInfo, out ExecutionMode executionResult) 
        {          
            Microsoft.SqlServer.Management.Smo.Server oldServer = null;
            if (NeedToSwitchServer)
            {
                // We use a new instance of the SMO Server object every time we script
                // so that any changes that are made to the SMO server while scripting are
                // not kept when the script operation is completed.
                oldServer = DataContainer.Server;

                //BUGBUG - see if we can use copy ctor instead
                DataContainer.Server = new Microsoft.SqlServer.Management.Smo.Server(DataContainer.ServerConnection);
            }

            String szScript = null;
            bool isScripting = IsScripting(executionInfo.RunType);
            var executionModeOriginal = GetServerConnectionForScript().SqlExecutionModes;
            //For Azure the ExecutionManager is different depending on which ExecutionManager
            //used - one at the Server level and one at the Database level. So to ensure we
            //don't use the wrong execution mode we need to set the mode for both (for on-prem
            //this will essentially be a no-op)
            SqlExecutionModes subjectExecutionModeOriginal = executionModeOriginal;
            SqlSmoObject sqlDialogSubject = null;
            try
            {
                sqlDialogSubject = this.DataContainer.SqlDialogSubject;
            }
            catch (System.Exception)
            {
                //We may not have a valid dialog subject here (such as if the object hasn't been created yet)
                //so in that case we'll just ignore it as that's a normal scenario. 
            }

            if (sqlDialogSubject != null)
            {
                subjectExecutionModeOriginal = sqlDialogSubject.ExecutionManager.ConnectionContext.SqlExecutionModes;
            }
            try
            {
                SqlExecutionModes newMode = isScripting
                    ? SqlExecutionModes.CaptureSql
                    : SqlExecutionModes.ExecuteSql;
                //now, do the execution itself
                GetServerConnectionForScript().SqlExecutionModes = newMode;
                if (sqlDialogSubject != null)
                {
                    sqlDialogSubject.ExecutionManager.ConnectionContext.SqlExecutionModes = newMode;
                }

                executionResult = DoPreProcessExecutionAndRunViews(executionInfo.RunType);

                if (isScripting)
                {
                    if (executionResult == ExecutionMode.Success)
                    {
                        szScript = BuildSqlScript();
                    }
                }

            }
            finally
            {
                GetServerConnectionForScript().SqlExecutionModes = executionModeOriginal;

                if (isScripting)
                {                    
                    GetServerConnectionForScript().CapturedSql.Clear();
                }

                if (sqlDialogSubject != null)
                {
                    sqlDialogSubject.ExecutionManager.ConnectionContext.SqlExecutionModes = subjectExecutionModeOriginal;
                    if (isScripting)
                    {
                        sqlDialogSubject.ExecutionManager.ConnectionContext.CapturedSql.Clear();
                    }
                }

                //see if we need to restore the server
                if (oldServer != null)
                {
                    DataContainer.Server = oldServer;
                }
            }

            if (isScripting)
            {
                executionInfo.Script = szScript;
            }
        }
    }
}
