//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Text;
using System.Xml;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Diagnostics;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.Admin;
using Microsoft.SqlTools.ServiceLayer.Agent;

namespace Microsoft.SqlTools.ServiceLayer.Management
{
    /// <summary>
    /// base class that can be used to derived from for the main classes
    /// </summary>
    public class ManagementActionBase : IDisposable, IExecutionAwareManagementAction
    {
#region Members

        /// <summary>
        /// service provider of our host. We should direct all host-specific requests to the services
        /// implemented by this provider
        /// </summary>
        private IServiceProvider serviceProvider;

        /// <summary>
        /// execution mode by default for now is success
        /// </summary>
        private ExecutionMode m_executionMode = ExecutionMode.Success;

        /// <summary>
        /// data container with initialization-related information
        /// </summary>
        private CDataContainer dataContainer;
        //whether we assume complete ownership over it.
        //We set this member once the dataContainer is set to be non-null
        private bool ownDataContainer = true;

        //if derived class tries to call a protected method that relies on service provider,
        //and the service provider hasn't been set yet, we will cache the values and will
        //propagate them when we get the provider set
        //private System.Drawing.Icon cachedIcon = null;
        private string cachedCaption = null;

        //SMO Server connection that MUST be used for all enumerator calls
        //We'll get this object out of CDataContainer, that must be initialized
        //property by the initialization code
        private ServerConnection  serverConnection;

        private ExecutionHandlerDelegate cachedExecutionHandlerDelegate;

#endregion

#region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        public ManagementActionBase()
        {
        }

#endregion

#region IDisposable implementation

        void IDisposable.Dispose()
        {
            // BUGBUG - do we need finalizer
            Dispose(true); // call protected virtual method
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

            // allow to be sited only once
            if (this.serviceProvider == null)
            {                             
                // cache the service provider
                this.serviceProvider = sp;             

                // call protected virtual method to enable derived classes to do initialization
                // OnHosted();
            }
        }

#endregion

#region IExecutionAwareManagementAction implementation

        /// <summary>
        /// called before management action executes onRun method.
        /// If something fails inside this function and the execution should be aborted,
        /// it can either raise an exception [in which case the framework will fail with exception text] 
        /// or set executionResult out parameter to be ExecutionMode.Failure
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
            // we start from failure
            executionResult = ExecutionMode.Failure;

            // OK, we do server switching for scripting for SQL Server execution here
            RunType runType = executionInfo.RunType;
            if (IsScripting(runType))
            {
                if (!PreProcessScripting(executionInfo, out executionResult))
                {
                    return false;
                }
            }

            if (this.DataContainer != null)
            {
                // we take over execution here. We substitute the server here for SQL containers
                if (this.DataContainer.ContainerServerType == CDataContainer.ServerType.SQL)
                {                
                    ExecuteForSql(executionInfo, out executionResult);
                    return false; // execution of the entire action was done here
                }
            }

            // call virtual function to do regular execution
            return DoPreProcessExecution(executionInfo.RunType, out executionResult);
        }

        /// <summary>
        /// called after dialog's host executes actions on all panels in the dialog one by one
        /// NOTE: it might be called from worker thread
        /// </summary>
        /// <param name="executionMode">result of the execution</param>
        /// <param name="runType">type of execution</param>
        public void PostProcessExecution(RunType runType, ExecutionMode executionResult)
        {
            //delegate to the protected virtual method
            DoPostProcessExecution(runType, executionResult);
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
        public bool Cancel()
        {
            return true;
        }
#endregion


        /// <summary>
        /// whether we own our DataContainer or not. Depending on this value it will or won't be
        /// disposed in our Dispose method
        /// </summary>
        protected virtual bool OwnDataContainer
        {
            get
            {
                // by default we own it
                return true;
            }
        }

        /// <summary>
        /// called by IExecutionAwareManagementAction.PreProcessExecution to enable derived
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

        /// <summary>
        /// called after management action executes onRun method.
        /// </summary>
        /// <param name="executionMode">result of the execution</param>
        /// <param name="runType">type of execution</param>
        protected virtual void DoPostProcessExecution(RunType runType, ExecutionMode executionResult)
        {
            //nothing to do in the base class
        }

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

        /// <summary>
        /// SMO Server connection that MUST be used for all enumerator calls
        /// We'll get this object out of CDataContainer, that must be initialized
        /// property by the initialization code
        /// </summary>
        protected ServerConnection ServerConnection
        {
            get
            {
                if (this.serverConnection == null && this.dataContainer != null && 
                    this.dataContainer.ContainerServerType == CDataContainer.ServerType.SQL)
                {
                    this.serverConnection = this.dataContainer.ServerConnection;
                }

                //it CAN be null here if dataContainer hasn't been set or the container type is non SQL
                return this.serverConnection;
            }
        }

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
                // true return value means that we need to do execution ourselves
                executionResult = PanelExecutionHandler.Run(runType, this);
            }

            return executionResult;
        }

        /// <summary>
        /// determines whether we need to substitute SMO server objects with the
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
                // We may not have a valid dialog subject here (such as if the object hasn't been created yet)
                // so in that case we'll just ignore it as that's a normal scenario. 
            }

            StringCollection sc = GetServerConnectionForScript().CapturedSql.Text;
            // Scripting may happen on either the server ExecutionManager or the
            // ExecutionManager of the object itself. So we make sure to check
            // the subject text if the server ExecutionManager didn't have any
            // scripts after doing the scripting
            if (sc.Count == 0 && sqlDialogSubject != null)
            {
                sc = sqlDialogSubject.ExecutionManager.ConnectionContext.CapturedSql.Text;
            }

            StringBuilder script = new StringBuilder(4096);
            if (sc != null)
            {
                for (int i = 0; i < sc.Count; i ++)
                {
                    script.AppendFormat("{0}\r\nGO\r\n", sc[i].ToString());
                }
            }

            return script.ToString();
        }

        /// <summary>
        /// called when we need to script a Sql Server action
        /// </summary>
        /// <param name="executionInfo"></param>
        /// <param name="executionResult"></param>
        private void ExecuteForSql(PreProcessExecutionInfo executionInfo, out ExecutionMode executionResult) 
        {          
            Microsoft.SqlServer.Management.Smo.Server oldServer = null;
            if (this.NeedToSwitchServer)
            {
                // We use a new instance of the SMO Server object every time we script
                // so that any changes that are made to the SMO server while scripting are
                // not kept when the script operation is completed.
                oldServer = this.DataContainer.Server;

                // BUGBUG - see if we can use copy ctor instead
                this.DataContainer.Server = new Microsoft.SqlServer.Management.Smo.Server(DataContainer.ServerConnection);
            }

            string script = null;
            bool isScripting = IsScripting(executionInfo.RunType);
            var executionModeOriginal = GetServerConnectionForScript().SqlExecutionModes;
            // For Azure the ExecutionManager is different depending on which ExecutionManager
            // used - one at the Server level and one at the Database level. So to ensure we
            // don't use the wrong execution mode we need to set the mode for both (for on-prem
            // this will essentially be a no-op)
            SqlExecutionModes subjectExecutionModeOriginal = executionModeOriginal;
            SqlSmoObject sqlDialogSubject = null;
            try
            {
                sqlDialogSubject = this.DataContainer.SqlDialogSubject;
            }
            catch (System.Exception)
            {
                // We may not have a valid dialog subject here (such as if the object hasn't been created yet)
                // so in that case we'll just ignore it as that's a normal scenario. 
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

                // now, do the execution itself
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
                        script = BuildSqlScript();
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
                    this.DataContainer.Server = oldServer;
                }
            }

            if (isScripting)
            {
                executionInfo.Script = script;
            }
        }

         /// <summary>
        /// returns internal helper class that we delegate execution of the panels one by one when 
        /// we do it ourselves during scripting
        /// </summary>
        private ExecutionHandlerDelegate PanelExecutionHandler
        {
            get
            {
                if (this.cachedExecutionHandlerDelegate == null)
                {                   
                    this.cachedExecutionHandlerDelegate = new ExecutionHandlerDelegate(this);
                }
                return this.cachedExecutionHandlerDelegate;
            }
        }

        protected IServiceProvider ServiceProvider
        {
            get
            {
                if (this.serviceProvider == null)
                {

                    throw new InvalidOperationException();
                }
                return this.serviceProvider;
            }
        }

        /// <summary>
        /// execution mode accessor
        /// </summary>
        protected ExecutionMode ExecutionMode
        {
            get
            {
                return m_executionMode;
            }
            set
            {
                m_executionMode = value;
            }
        }

        public virtual ExecutionMode LastExecutionResult
        {
            get
            {
                return ExecutionMode;
            }
        }

        /// <summary>
        /// Overridable function that allow a derived class to implement
        /// a finalizing action after a RunNow or RunNowAndClose where sucesfully executed
        /// </summary>
        /// <param name="sender"></param>
        public virtual void OnTaskCompleted(object sender, ExecutionMode executionMode, RunType executionType)
        {
            //nothing
        }


        /// <summary>
        /// Overridable function that allow a derived class to implement its
        /// OnRunNow functionality
        /// </summary>
        /// <param name="sender"></param>
        public virtual void OnRunNow(object sender)
        {
            //nothing
        }

        /// <summary>
        /// Overridable function that allow a derived class to implement its
        /// OnScript functionality. 
        /// </summary>
        /// <param name="sender"></param>
        public virtual string OnScript(object sender)
        {
            // redirect to the single scripting virtual method by default
            return Script();
        }

        /// <summary>
        /// derived class should override this method if it does same action for all types of scripting,
        /// because all ILaunchFormHostedControl scripting methods implemented in this class simply
        /// call this method
        /// </summary>
        /// <returns></returns>
        protected virtual string Script()
        {
            // redirect to the RunNow method. Our host should be turning script capture on and off for
            // OLAP/SQL servers and composing the text of the resulting script by itself
            OnRunNow(this);

            // null is a special value. It means that we want to indicate that we didn't want to generate 
            // script text
            return null;
        }

        /// <summary>
        /// performs custom action wen user requests a cancel
        /// this is called from the UI thread and generally executes
        /// smoServer.Cancel() or amoServer.Cancel() causing
        /// the worker thread to inttrerupt its current action
        /// </summary>
        /// <param name="sender"></param>
        public virtual void OnCancel(object sender)
        {
            if (this.dataContainer == null)
            {
                return;
            }

            if (this.dataContainer.Server != null)
            {
                // TODO: uncomment next line when SMO server will have support for Cancel
                // this.dataContainer.Server.Cancel();
            }          
        }
    }
}
