//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

using Microsoft.SqlServer.Management.Diagnostics;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
	/// <summary>
	/// This class is responsible for executing panels one by one.
	/// It is reused by ViewSwitcherControlsManager and treepanelform classes
	/// </summary>
	internal class PanelExecutionHandler
	{
        private ISqlControlCollection viewsHolder;
        private object cancelCriticalSection = new object();
        // private IPanelExecutionHandlerHost ourHost;
        
        private int currentlyRunningPanel = 0;
        private bool continueWithNextPanel = true;
        
        private PanelExecutionHandler() {}
        
        public PanelExecutionHandler(ISqlControlCollection viewsHolder, IServiceProvider sp)
		{


            this.viewsHolder = viewsHolder;

            // this.ourHost = (IPanelExecutionHandlerHost)sp.GetService(typeof(IPanelExecutionHandlerHost));
            // if (this.ourHost == null)
            // {
            //     //BUGBUG - do we need to have special exception class for internal exceptions that should never happen?
            //     throw new ArgumentException("SRError.CannotInit", "sp");
            // }
        }

       
        /// <summary>
        /// </summary>
        /// <param name="runType"></param>
        /// <param name="sender"></param>
        /// <returns>execution result</returns>
        public ExecutionMode Run(RunType runType, object sender)
        {
            // STrace.Params(SqlMgmtDiag.TName, "PanelExecutionHandler.Run", "runType = {0}", runType);
            // //NOTE: this method either lets exception thrown by a panel to fly out or propagates the
            // //final execution result in its return value
            // STrace.Assert(this.ourHost != null);

            // this.continueWithNextPanel = true;
            // int	count = NoOfPanels;
            // for(int i = 0; i < count; i ++)
            // {
            //     lock(this.cancelCriticalSection)
            //     {
            //         if(this.continueWithNextPanel == false) // can be set to false by ui thread
            //         {
            //             return ExecutionMode.Cancel;
            //         }
            //         CurrentlyRunningPanel = i;
            //     }

            //     ViewInfo vi = this.viewsHolder.GetViewInfo(i);
            //     if (!vi.Initialized)
            //     {
            //         continue; // skip uninitialized panels and views without a panel
            //     }

            //     ILaunchFormHostedControl panelForm = (ILaunchFormHostedControl)vi.PanelForm;//MUST support this interface
            //     //dispatch the call to the right method
            //     switch (runType)
            //     {
            //         case RunType.RunNow:
            //         case RunType.RunNowAndExit:
            //             panelForm.OnRunNow(sender);
            //             break;

            //         case RunType.ScriptToClipboard:
            //             this.ourHost.OnCurrentPanelScriptAvailable(panelForm.OnScriptToClipboard(sender));
            //             break;

            //         case RunType.ScriptToFile:
            //             this.ourHost.OnCurrentPanelScriptAvailable(panelForm.OnScriptToFile(sender));
            //             break;

            //         case RunType.ScriptToWindow:
            //             this.ourHost.OnCurrentPanelScriptAvailable(panelForm.OnScriptToWindow(sender));
            //             break;

            //         case RunType.ScriptToJob:
            //             this.ourHost.OnCurrentPanelScriptAvailable(panelForm.OnScriptToJob(sender));
            //             break;

            //         default:
            //             STrace.Assert(false, "Unexpected run type = " + runType.ToString());
            //             STrace.LogExThrow();
            //             //BUGBUG - should we have special internal exception type?
            //             throw new InvalidOperationException(SRError.UnexpectedRunType);
            //     }
                    
            //     if((panelForm.LastExecutionResult == ExecutionMode.Failure) || 
            //        (panelForm.LastExecutionResult == ExecutionMode.Cancel))
            //     {
            //         return panelForm.LastExecutionResult;
            //     }
            // }

            //if we're here, everything went fine
            return ExecutionMode.Success;
        }

        /// <summary>
        /// performs custom action wen user requests a cancel
        /// this is called from the UI thread 
        /// </summary>
        /// <param name="sender"></param>
        public void Cancel(object sender)
        {
            // STrace.Params(SqlMgmtDiag.TName, "PanelExecutionHandler.Cancel", "", null);
            // // and give a chance to current control to execute some cancel work from ui thread
            // STrace.Assert(this.cancelCriticalSection != null);
            // lock (this.cancelCriticalSection)
            // {
            //     // mark the flag that tells to stop processing next panels
            //     if (this.continueWithNextPanel)//prevent from doing it more than once
            //     {
            //         this.continueWithNextPanel = false;

            //         STrace.Assert(CurrentlyRunningPanel < NoOfPanels);
            //         if (CurrentlyRunningPanel < NoOfPanels)
            //         {
            //             ViewInfo vi = this.viewsHolder.GetViewInfo(CurrentlyRunningPanel);
            //             if (vi.Initialized)
            //             {
            //                 ILaunchFormHostedControl panelForm = (ILaunchFormHostedControl)vi.PanelForm;//MUST support this interface
            //                 panelForm.OnCancel(sender);
            //             }
            //         }
            //     }
            // }
        }

        /// <summary>
        /// resets all initialized views
        /// </summary>
        public void Reset(object sender)
        {
            // STrace.Params(SqlMgmtDiag.TName, "PanelExecutionHandler.Reset", "", null);

            // //Reset panels one by one
            // int count = NoOfPanels;
            // for(int i = 0; i < count; i ++)
            // {
            //     ViewInfo vi = this.viewsHolder.GetViewInfo(i);
            //     if (!vi.Initialized)
            //     {
            //         continue; // skip uninitialized panels
            //     }

            //     ILaunchFormHostedControl panelForm = (ILaunchFormHostedControl)vi.PanelForm;//MUST support this interface
            //     panelForm.OnReset(sender);
            // }
        }


        #region private helpers

        /// <summary>
        /// get number of panels
        /// </summary>
        private int NoOfPanels
        {
            get
            {
                return this.viewsHolder.ViewsCount;
            }
        }

        /// <summary>
        /// index of the panel that is currently executing
        /// </summary>
        private int CurrentlyRunningPanel
        {
            get
            {
                return this.currentlyRunningPanel;
            }
            set
            {
                //let our host know
                //this.ourHost.OnCurrentPanelChanged(value);

                this.currentlyRunningPanel = value;
            }
        }


        #endregion
	}
}
