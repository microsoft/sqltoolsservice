using System;
using System.Collections;
using System.Text;
using Microsoft.SqlServer.Management.Diagnostics;
using Microsoft.SqlTools.ServiceLayer.Agent;

namespace Microsoft.SqlTools.ServiceLayer.Management
{
    /// <summary>
    /// defines mandatory interface that an action must implement
    /// </summary>
    public interface IManagementAction
    {
        ExecutionMode LastExecutionResult { get; }

        /// <summary>
        /// performs custom action when user requests to cancel execution.
        /// </summary>
        /// <param name="sender"></param>
        void OnCancel(object sender);
      
        /// <summary>
        /// Overridable function that allow a derived class to implement its
        /// OnScript functionality. 
        /// </summary>
        /// <param name="sender"></param>
        /// <returns>text of the generated script</returns>
        string OnScript(object sender);

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
                    throw new InvalidOperationException(SR.UnexpectedRunType);
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
	internal sealed class ExecutonHandler : IDisposable
	{
        /// <summary>
        /// handler that we delegate execution related tasks to
        /// </summary>
        private ExecutionHandlerDelegate executionHandlerDelegate;

        /// <summary>
        /// class that describes available views that is also aware of execution
        /// </summary>
        private IExecutionAwareManagementAction managementAction;

        /// <summary>
        /// result of the last execution
        /// </summary>
        private ExecutionMode executionResult;

        /// <summary>
        /// exception that caused execution failure
        /// </summary>
        private Exception executionFailureException;

        /// <summary>
        /// text of the generated script if RunNow method was called last time with scripting option
        /// </summary>
        private StringBuilder script; 

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
        public ExecutonHandler(IExecutionAwareManagementAction managementAction)
        {     
            this.managementAction = managementAction;
            this.executionHandlerDelegate = new ExecutionHandlerDelegate(managementAction);
        }

        #region public interface

        public ExecutionMode ExecutionResult
        {
            get
            {
                return this.executionResult;
            }
        }

        /// <summary>
        /// exception that caused execution failure
        /// </summary>
        public Exception ExecutionFailureException
        {
            get
            {
                return this.executionFailureException;
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
        /// we call the run now implementaion of the management action.
        /// If any exception is generated we stop the execution and we set the execution mode flag to failure. 
        /// </summary>
        /// <param name="sender"></param>
        public void RunNow(RunType runType, object sender)
        {
            try
            {
                // reset some internal vars
                this.executionResult = ExecutionMode.Failure;

                // ensure that we have valid StringBulder for scripting
                if (IsScripting(runType))
                {
                    EnsureValidScriptBuilder();
                }

                // do preprocess action. It is possible to do entire execution from inside this method
                if (this.managementAction != null)
                {
                    PreProcessExecutionInfo preProcessInfo = new PreProcessExecutionInfo(runType);
                    if (!this.managementAction.PreProcessExecution(preProcessInfo, out this.executionResult))
                    {                       
                        // In case of scripting preProcessInfo.Script must contain text of the script
                        if (executionResult == ExecutionMode.Success && IsScripting(runType) && preProcessInfo.Script != null)
                        {
                            this.script.Append(preProcessInfo.Script);
                        }

                        return; // result of execution is in executionResult
                    }
                }
                // NOTE: post process action is done in finally block below

                // start executing
                this.executionResult = this.executionHandlerDelegate.Run(runType, sender);
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
                ProcessExceptionDuringExecution(e);

                return;
            } 
            finally
            {
                //do postprocess action
                if (this.managementAction != null)            
                {
                    this.managementAction.PostProcessExecution(runType, this.executionResult);
                }
            }

            #endregion
        }

        /// <summary>
        /// Kicks off Cancel operation
        /// </summary>
        /// <param name="sender"></param>
        public void InitiateCancel(object sender)
        {
            if (this.managementAction != null)
            {
                if (!this.managementAction.Cancel())
                {
                    //everything was done inside this method
                    this.executionResult = ExecutionMode.Cancel;
                    return;
                }
            }

            //otherwise do cancel ourselves
            // if everything goes OK, Run() method will return with Cancel result
            this.executionHandlerDelegate.Cancel(sender);
        }

        /// <summary>
        /// is called by the host to do post execution actions
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="executionMode"></param>
        /// <param name="executionType"></param>
        public void OnTaskCompleted(object sender, ExecutionMode executionResult, RunType executionType)
        {         
        }

        /// <summary>
        /// enables deterministic cleanup
        /// </summary>
        public void Dispose()
        {
            IDisposable managementActionAsDisposable = this.managementAction as IDisposable;
            if (managementActionAsDisposable != null)
            {
                managementActionAsDisposable.Dispose();
            }
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
        private void ProcessExceptionDuringExecution(Exception ex)
        {
            // show the error
            this.executionResult = ExecutionMode.Failure; 
            this.executionFailureException = ex;
        }
        #endregion
	}
}
