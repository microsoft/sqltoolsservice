using System;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Text;
using System.Collections;
using System.Drawing;
using Microsoft.SqlServer.Management.Diagnostics;
using Microsoft.SqlTools.ServiceLayer.Agent;

namespace Microsoft.SqlTools.ServiceLayer.Management
{
    /// <summary>
    /// the container of the user control should implement 
    /// ISupportStatusNotifications in its service provider
    /// if it is interested in receiving notifications about task status -
    /// this might be needed when the RunNow tasks is performend in an worker thread
    /// and the ui thread wants to get updated with worker's status
    ///
    /// When control have ISupportStatusNotifications from its host, it will notifiy 
    /// the host about our current state (running, cancel processed, done, etc...)
    /// It will do notifications by
    /// directly calling methods of the interface from the thread that control runs on. 
    /// It is responsibility of the service implementation to marshal the calls
    /// into UI thread if/when needed
    ///
    /// if the ISupportStatusNotifications is not implemented in the service provider 
    /// then it means that the container is not interesed in such notifications 
    /// (posibly RunNow was called from UI thread)
    /// </summary>
    public interface ISupportStatusNotifications
	{
        /// <summary>
        /// Called to report that current control executed a percentage of the user requested actions
        /// </summary>
        /// <param name="runningState"></param>
        /// <param name="percentDone"></param>
        void Update(object runningState, int percentDone);
	}
    
    /// <summary>
    /// defines mandatory interface that a control that wants to be hosted
    /// inside the launch form MUST implement. The LaunchForm class will refuse
    /// hosting anything that doesn't implement this interface
    /// </summary>
    public interface IManagementAction
    {
        ExecutionMode LastExecutionResult { get; }

        /// <summary>
        /// override this method and implement it by gathering information from UI
        /// this methods is guarateed to be called from UI thread so all this gathering will be ok
        /// 
        /// gathering UI infromation in other methods - like OnRunNow will not be recommanded
        /// since other methods (like OnRunNow/OnScriptToXXX) can be called as part of a non-ui thread
        /// 
        /// this should be called allways from the ui thread and
        /// we get underlying control a chance to do some ui interactions
        /// before executing (in a worker thread) OnRunNow/OnScriptToWindow/OnScriptToFile
        ///
        /// otherwise if the control still wants to do ui actions from
        /// the OnRunNow() in order to be a good citizen when OnRunNow()
        /// is executed by a worker thread he must use delegates for every ui action
        /// </summary>
        /// <param name="runType"></param>
        //void OnGatherUiInformation(RunType runType);

        /// <summary>
        /// performs custom action when user requests to cancel execution.
        /// </summary>
        /// <param name="sender"></param>
        void OnCancel(object sender);

        /// <summary>
        /// Overridable function that allow a derived class to implement its
        /// OnScriptToClipboard functionality. 
        /// </summary>
        /// <param name="sender"></param>
        /// <returns>text of the generated script</returns>
        //string OnScriptToClipboard(object sender);

        /// <summary>
        /// Overridable function that allow a derived class to implement its
        /// OnScriptToWindow functionality. 
        /// for a save as dialog
        /// </summary>
        /// <param name="sender"></param>
        /// <returns>text of the generated script</returns>
        //string OnScriptToFile(object sender);

        /// <summary>
        /// Overridable function that allow a derived class to implement its
        /// OnScriptToWindow functionality. 
        /// </summary>
        /// <param name="sender"></param>
        /// <returns>text of the generated script</returns>
        //string OnScriptToWindow(object sender);
        string OnScript(object sender);

        /// <summary>
        /// Overridable function that allow a derived class to implement its
        /// OnScriptToJob functionality. 
        /// </summary>
        /// <param name="sender"></param>
        /// <returns>text of the generated script</returns>
        //string OnScriptToJob(object sender);

        /// <summary>
        /// Overridable function that allow a derived class to implement its
        /// OnReset functionality
        /// </summary>
        /// <param name="sender"></param>
        //void OnReset(object sender);

        /// <summary>
        /// Overridable function that allow a derived class to implement its
        /// OnRunNow functionality
        /// </summary>
        /// <param name="sender"></param>
        void OnRunNow(object sender);

        /// <summary>
        /// Overridable function that allow a derived class to implement
        /// a finalizing action after a RunNow or RunNowAndClose were sucesfully executed
        /// NOTE: same as OnGatherUiInformation, this method is always called from UI thread
        /// </summary>
        /// <param name="sender"></param>
        void OnTaskCompleted(object sender, ExecutionMode executionMode, RunType executionType);        
    }

    	/// <summary>
	/// This class is responsible for executing panels one by one.
	/// It is reused by ViewSwitcherControlsManager and treepanelform classes
	/// </summary>
	internal class ExecutionHandlerDelegate
	{
        private ISqlControlCollection viewsHolder;
        private object cancelCriticalSection = new object();
        private IManagementAction managementAction;

        public ExecutionHandlerDelegate(IManagementAction managementAction)
		{        
            this.managementAction = managementAction;      
        }
       
        /// <summary>
        /// </summary>
        /// <param name="runType"></param>
        /// <param name="sender"></param>
        /// <returns>execution result</returns>
        public ExecutionMode Run(RunType runType, object sender)
        {
            //IManagementAction panelForm = (IManagementAction)vi.PanelForm;//MUST support this interface
            //dispatch the call to the right method
            switch (runType)
            {
                case RunType.RunNow:
                    this.managementAction.OnRunNow(sender);
                    break;

                case RunType.ScriptToWindow:
                    this.managementAction.OnScript(sender);
                    break;

                default:
                    throw new InvalidOperationException("SRError.UnexpectedRunType");
            }
                
            if((this.managementAction.LastExecutionResult == ExecutionMode.Failure) || 
                (this.managementAction.LastExecutionResult == ExecutionMode.Cancel))
            {
                return this.managementAction.LastExecutionResult;
            }

            // if we're here, everything went fine
            return ExecutionMode.Success;
        }

        /// <summary>
        /// performs custom action wen user requests a cancel
        /// this is called from the UI thread 
        /// </summary>
        /// <param name="sender"></param>
        public void Cancel(object sender)
        {
            lock (this.cancelCriticalSection)
            {
                this.managementAction.OnCancel(sender);                     
            }
        }
	}

    /// <summary>
	/// manager that hooks up tree view with the individual views
	/// </summary>
	internal sealed class ExecutonHandler : IDisposable, ISupportStatusNotifications
	{
        //service provider whom we can delegate actions as needed
        private IServiceProvider parentServiceProvider;
        private ServiceContainer serviceContainer;

        /// <summary>
        /// handler that we delegate execution related tasks to
        /// </summary>
        private ExecutionHandlerDelegate panelExecutionHandler;

        /// <summary>
        /// class that describes available views
        /// </summary>
        private ISqlControlCollection viewsHolder;

        /// <summary>
        /// class that describes available views that is also aware of execution
        /// </summary>
        private IExecutionAwareManagementAction execAwareViewsHolder;

        /// <summary>
        /// result of the last execution
        /// </summary>
        private ExecutionMode executionResult;

        /// <summary>
        /// text of the generated script if RunNow method was called last time with scripting option
        /// </summary>
        private StringBuilder script; 

        /// <summary>
        /// index of the panel that is being executed
        /// </summary>
        private int currentlyExecutingPanelIndex;

        private IManagementAction managementAction;

        /// <summary>
        /// creates instance of the class and returns service provider that aggregates the provider
        /// provider with extra services
        /// </summary>
        /// <param name="serviceProvider">service provider from the host</param>
        /// <param name="aggregatedProvider">
        /// aggregates service provider that is derived from the host service provider and
        /// is extended with extra services and/or overriden services. The host should be
        /// using this provider whenever it has to specify an IServiceProvider to a component
        /// that will be managed by this class
        /// </param>
        public ExecutonHandler(IManagementAction managementAction)
        {     
            this.managementAction = managementAction;
            this.panelExecutionHandler = new ExecutionHandlerDelegate(managementAction);
        }

        #region ISupportStatusNotifications implementation
        
        /// <summary>
        /// accept status report from the current panel and aggregate it, taking into account
        /// total # of panels. After doing that report to the host that adjusted percentage
        /// </summary>
        /// <param name="runningState"></param>
        /// <param name="percentDone"></param>
        public void Update(object runningState, int percentDone)
        {         
            // if (StatusReport == null)
            // {
            //     return;
            // }
            
            // // [0,100] please
            // if (percentDone < 0 || percentDone > 100)
            // {
            //     throw new ArgumentException("percentDone");
            // }

            // //report to the host's service provider
            // StatusReport.Update(runningState, percentDone);
        }

        #endregion

        #region public interface

        public ExecutionMode ExecutionResult
        {
            get
            {
                return this.executionResult;
            }
        }

        
        /// <summary>
        /// text of the generated script if RunNow method was called last time with scripting option
        /// </summary>
        public string ScriptTextFromLastRun
        {
            get
            {
                if (this.script != null)
                {
                    return this.script.ToString();                    
                } 
                else
                {
                    return string.Empty;
                }
            }
        }


        /// <summary>
        /// should be used to check whether given run type can be performed with hosted control
        /// in its current state
        /// </summary>
        /// <param name="runType"></param>
        /// <returns></returns>
        // public bool IsRunTypeEnabled(RunType runType)
        // {
        //     STrace.Params("SqlMgmtDiag.TName", "ViewSwitcherControlsManager.IsRunTypeEnabled", "runType = {0}", runType);

        //     //if at least one panel indicates that an action should be disabled - it is disabled
        //     //otherwise, it is enabled
        //     ViewInfo vi;
        //     for (int i = 0; i < NoOfPanels; i++)
        //     {
        //         vi = this.viewsHolder.GetViewInfo(i);
        //         if (!vi.Initialized)
        //         {
        //             continue;
        //         }
                
        //         if (!vi.PanelForm.IsRunTypeEnabled(runType))
        //         {
        //             STrace.Trace(SqlMgmtDiag.TName, SqlMgmtDiag.NormalTrace, "ViewSwitcherControlsManager.IsRunTypeEnabled: panel #{0} cannot do {1}", i, runType);
        //             return false;
        //         }
        //     }

        //     STrace.Trace(SqlMgmtDiag.TName, SqlMgmtDiag.NormalTrace, "ViewSwitcherControlsManager.IsRunTypeEnabled: all panels can do {0}", runType);
        //     return true;
        
        // }


        // /// <summary>
        // /// main initialization that should be done after construction of the object
        // /// This method will initialize control's container, hook up with tree view etc.
        // /// The LaunchForm must not be shown until this call was done
        // /// </summary>
        // /// <param name="treeView"></param>
        // /// <param name="viewsHolder"></param>
        // /// <param name="rightPane"></param>
        // /// <returns></returns>
        // public void InitializeUI(ViewSwitcherTreeView treeView, 
        //     ISqlControlCollection viewsHolder, Panel rightPane)
        // {

        //     //create and init execution class
        //     this.panelExecutionHandler = new PanelExecutionHandler(this.viewsHolder, this.serviceContainer);
        // }

        // /// <summary>
        // /// validates the current state of the control and returns true if it is valid, false otherwise
        // /// It is OK to show UI with error message while validating from inside this method
        // /// </summary>
        // /// <returns></returns>
        // public bool Validate()
        // {
        //     STrace.Params("validation", "ViewSwitcherControlsManager.Validate", "", null);
        //     //validate the current view
        //     ISupportValidation curValidatingView = CurrentControlAsHostedControl as ISupportValidation;
        //     if (curValidatingView != null)
        //     {
        //         STrace.Trace("validation", SqlMgmtDiag.LowTrace, "ViewSwitcherControlsManager.Validate: validating current view");
        //         return curValidatingView.Validate();
        //     }

        //     return true;
        // }


        /// <summary>
        /// we call the run now implementaion of each panel form view in there order from views array. 
        /// If any exception is generated or one panel execution results in an exception we stop the 
        /// execution and we set the execution mode flag to failure. Otherwise if all panels execution 
        /// was successfull the execution of the tree panel form will be successfull. Only the OnRunNow 
        /// of the panels that are initialized are called
        /// 
        /// this method can be called by an worker thread, so user controls shouldnt perform ui operations
        /// on their ILaunchFormHostedControl.OnRunNow (Windows guidelines)
        /// and all these ui operations should have been already performed ILaunchFormHostedControl.OnGatherUiInformation()
        /// </summary>
        /// <param name="sender"></param>
        public void RunNow(RunType runType, object sender)
        {
            try
            {
                // reset some internal vars
                this.executionResult = ExecutionMode.Failure;
                this.currentlyExecutingPanelIndex = -1; // will become 0 if we're executing on view by view basis

                // ensure that we have valid StringBulder for scripting
                if (IsScripting(runType))
                {
                    EnsureValidScriptBuilder();
                }

                //do preprocess action. It is possible to do entire execution from inside this method
                if (this.execAwareViewsHolder != null)
                {
                    PreProcessExecutionInfo preProcessInfo = new PreProcessExecutionInfo(runType);
                    if (!this.execAwareViewsHolder.PreProcessExecution(preProcessInfo, out this.executionResult))
                    {                       
                        //In case of scripting preProcessInfo.Script must contain text of the script
                        if (executionResult == ExecutionMode.Success && IsScripting(runType) && preProcessInfo.Script != null)
                        {
                            this.script.Append(preProcessInfo.Script);
                        }

                        return; //result of execution is in executionResult
                    }
                }
                //NOTE: post process action is done in finally block below

                //start executing
                this.executionResult = this.panelExecutionHandler.Run(runType, sender);
            }
            #region error handling

            catch (OutOfMemoryException)
            {
                throw;
            }
            catch (System.Threading.ThreadAbortException)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                this.executionResult = ExecutionMode.Cancel;
            }
            catch (Exception e)
            {
                ProcessExceptionDuringExecution(e, this.currentlyExecutingPanelIndex);

                return;
            } 
            finally
            {
                //do postprocess action
                if (this.execAwareViewsHolder != null)            
                {
                    this.execAwareViewsHolder.PostProcessExecution(runType, this.executionResult);
                }
            }

            #endregion
        }


        /// <summary>
        /// We call the reset implementaion of each panel form view in there order from views array. 
        /// If any exception is generated or one panel execution results in an exception we stop the 
        /// reset
        /// Only the OnReset of the panels that are initialized is called
        /// </summary>
        /// <param name="sender"></param>
        // public void Reset(object sender)
        // {
        //     STrace.Params(SqlMgmtDiag.TName, "ViewSwitcherControlsManager.Reset", "", null);

        //     //NOTE: because currently Reset is done on UI thread without progress reporting, we
        //     //don't have any error handling here - the LaunchForm does
            
        //     //do preprocess action
        //     if (this.execAwareViewsHolder != null)
        //     {
        //         STrace.Trace(SqlMgmtDiag.TName, SqlMgmtDiag.NormalTrace, "ViewSwitcherControlsManager.Reset: calling PreProcessReset");
        //         if (!this.execAwareViewsHolder.PreProcessReset())
        //         {
        //             //false -> entire Reset operation was done by this method
        //             STrace.Trace(SqlMgmtDiag.TName, SqlMgmtDiag.LowTrace, "ViewSwitcherControlsManager.Reset: entire Reset operation was done by PreProcessReset");
        //             return;
        //         }
        //     }

        //     //delegate the main work
        //     this.panelExecutionHandler.Reset(sender);

        //     //do post process action
        //     if (this.execAwareViewsHolder != null)            
        //     {
        //         STrace.Trace(SqlMgmtDiag.TName, SqlMgmtDiag.NormalTrace, "ViewSwitcherControlsManager.Reset: calling PostProcessReset");
        //         this.execAwareViewsHolder.PostProcessReset();;
        //     }
        // }


        // /// <summary>
        // /// Kicks off Cancel operation
        // /// </summary>
        // /// <param name="sender"></param>
        // public void InitiateCancel(object sender)
        // {
        //     STrace.Params(SqlMgmtDiag.TName, "ViewSwitcherControlsManager.InitiateCancel", "", null);
            
        //     if (this.execAwareViewsHolder != null)
        //     {
        //         if (!this.execAwareViewsHolder.Cancel())
        //         {
        //             //everything was done inside this method
        //             this.executionResult = ExecutionMode.Cancel;
        //             return;
        //         }
        //     }

        //     //otherwise do cancel ourselves
        //     STrace.Assert(this.panelExecutionHandler != null);
        //     this.panelExecutionHandler.Cancel(sender);//if everything goes OK, Run() method will return with Cancel result
        // }

		
        /// <summary>
        /// This methods is guarateed to be called from UI thread so all this gathering will be ok
        /// 
        /// gathering UI infromation in other methods - like OnRunNow will not be recommanded
        /// since other methods (like OnRunNow/OnScriptToXXX) can be called as part of a non-ui thread
        /// 
        /// this should be called allways from the ui thread and
        /// we get underlying control a chance to do some ui interactions
        /// before executing (in a worker thread) OnRunNow/OnScriptToWindow/OnScriptToFile
        ///
        /// otherwise if the control still wants to do ui actions from
        /// the RunNow() in order to be a good citizen when RunNow()
        /// is executed by a worker thread he must use delegates for every ui action
        /// </summary>
        /// <param name="runType"></param>
        /// <returns>value of LastExecutionResult of the views</returns>
        // public ExecutionMode OnGatherUiInformation(RunType runType)
        // {
        //     STrace.Params(SqlMgmtDiag.TName, "ViewSwitcherControlsManager.OnGatherUiInformation", "runType = {0}", runType);

        //     int currentlyRunningView = -1;
        //     try
        //     {
        //         // move focus to the tree control - (important bugfix) - this forces validation of current ctrol habving the focus (maybe an up-down control)
        //         // in order for this to work we need to have OnGatherUiInformation() called before disabling the panel
        //         this.treeView.Focus();

        //         int count = NoOfPanels;
        //         for(currentlyRunningView = 0; currentlyRunningView < count; currentlyRunningView++)
        //         {
        //             ViewInfo vi = this.viewsHolder.GetViewInfo(currentlyRunningView);
        //             if (!vi.Initialized)
        //             {
        //                 continue; // skip uninitialized uc
        //             }

        //             ILaunchFormHostedControl panelForm = (ILaunchFormHostedControl)vi.PanelForm;//MUST support this interface
        //             panelForm.OnGatherUiInformation(runType);

        //             if (panelForm.LastExecutionResult == ExecutionMode.Cancel || panelForm.LastExecutionResult == ExecutionMode.Failure)
        //             {
        //                 return panelForm.LastExecutionResult;
        //             }
        //         }

        //         return ExecutionMode.Success;
        //     }
        //     #region error handling

        //    catch (OutOfMemoryException) 
        //     {
        //         throw;
        //     }
        //     catch (System.Threading.ThreadAbortException) 
        //     {
        //         throw;
        //     }
        //     catch(Exception e)
        //     {
        //         STrace.LogExCatch(e);

        //         ProcessExceptionDuringExecution(e, currentlyRunningView);
                
        //         return ExecutionMode.Failure;//halt the execution
        //     } 
        //     #endregion
        // }



        /// <summary>
        /// is called by the host to do post execution actions
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="executionMode"></param>
        /// <param name="executionType"></param>
        public void OnTaskCompleted(object sender, ExecutionMode executionResult, RunType executionType)
        {
            // STrace.Params(SqlMgmtDiag.TName, "ViewSwitcherControlsManager.OnTaskCompleted", 
            //     "executionResult = {0}, executionType = {1}", executionResult, executionType);

            // try
            // {
            //     //propagate to all initialized panels
            //     STrace.Trace(SqlMgmtDiag.TName, SqlMgmtDiag.NormalTrace, "ViewSwitcherControlsManager.OnTaskCompleted: processing panels");
            //     int count = NoOfPanels;
            //     for(int i = 0; i < count; i ++)
            //     {
            //         ViewInfo vi = this.viewsHolder.GetViewInfo(i);
            //         if (!vi.Initialized)
            //         {
            //             continue; // skip uninitialized panels
            //         }

            //         ILaunchFormHostedControl panelForm = (ILaunchFormHostedControl)vi.PanelForm;//MUST support this interface
            //         panelForm.OnTaskCompleted(sender, executionResult, executionType);
            //     }
            // }
            // #region Error handling

            // catch (OutOfMemoryException) 
            // {
            //     throw;
            // }
            // catch (System.Threading.ThreadAbortException) 
            // {
            //     throw;
            // }
            // catch (Exception e)
            // {
            //     DisplayExceptionMessage(e);
            // }		
            // #endregion
        }


        /// <summary>
        /// enables deterministic cleanup
        /// </summary>
        public void Dispose()
        {
         

            // IDisposable viewHolderAsDisposable = this.viewsHolder as IDisposable;
            // if (viewHolderAsDisposable != null)
            // {
            //     viewHolderAsDisposable.Dispose();
            // }
        }
        
        #endregion

        #region private helpers


        /// <summary>
        /// determines whether given run type corresponds to scripting or not
        /// </summary>
        /// <param name="runType"></param>
        /// <returns></returns>
        private bool IsScripting(RunType runType)
        {
            return (runType == RunType.ScriptToClipboard || 
                    runType == RunType.ScriptToFile || 
                    runType == RunType.ScriptToWindow ||
                    runType == RunType.ScriptToJob);
        }

        
        /// <summary>
        /// ensure that we have valid StringBulder for scripting
        /// </summary>
        private void EnsureValidScriptBuilder()
        {
            if (this.script == null)
            {
                this.script = new StringBuilder(256);
            } 
            else
            {
                this.script.Length = 0;
            }
        }


        /// <summary>
        /// helper function that is called when we caught an exception during execution
        /// </summary>
        /// <param name="e"></param>
        /// <param name="failedViewIndex">-1 indicates that we don't know</param>
        private void ProcessExceptionDuringExecution(Exception e, int failedViewIndex)
        {        
            //NOTE: change of plans. We no longer try switching the view in the framework. Dialogs
            //can do it themselves by using ILaunchForm.SelectView
#if false
            //NOTE: per PM, we should do the following: if it is the framework that shows error message,
            //then it should switch to the failed view first, and show error after that.
            //If the dialog intercepted the error and presented it to the user, we don't
            //switch the view at all
                
            //see if we need to switch the view

            STrace.Trace(SqlMgmtDiag.TName, SqlMgmtDiag.LowTrace, "ViewSwitcherControlsManager.RunNow: execution failed. Checking if need to switch to the failed view");
            //we keep current view index in this.currentlyExecutingPanelIndex
            if (failedViewIndex >= 0 && failedViewIndex < NoOfPanels)
            {
                if (failedViewIndex != this.currentView)
                {
                    STrace.Trace(SqlMgmtDiag.TName, SqlMgmtDiag.LowTrace, 
                        "ViewSwitcherControlsManager.RunNow: execution failed. switching into the failed view with index = {0}", this.currentlyExecutingPanelIndex);
                    TreeNode failedViewNode = FindLastNodeReferencingPage(failedViewIndex + 1);//this method works with 1 based indices
                    STrace.Assert(failedViewNode != null);
                    STrace.Assert(this.viewsHolder.GetViewInfo(failedViewIndex).Initialized);
                    if (failedViewNode != null)
                    {
                        this.treeView.SelectedNode = failedViewNode; 
                    }
                }
            }
#endif

            //show the error
            this.executionResult = ExecutionMode.Failure;
        }

        #endregion

	}
}
