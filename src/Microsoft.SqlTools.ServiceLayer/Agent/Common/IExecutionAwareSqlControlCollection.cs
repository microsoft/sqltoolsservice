//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    public class PreProcessExecutionInfo
    {
        private RunType runType;
        private string script;
        private PreProcessExecutionInfo() {}

        internal PreProcessExecutionInfo(RunType runType)
        {
            this.runType = runType;
        }

        public RunType RunType
        {
            get
            {
                return this.runType;
            }
        }

        public string Script
        {
            get
            {
                return this.script;
            }

            set
            {
                this.script = value;
            }
        }
    }
    
    
    /// <summary>
	/// IExecutionAwareSqlControlCollection allows control's container to do pre and post
	/// processing of the execution commands
	/// </summary>
	public interface IExecutionAwareSqlControlCollection : ISqlControlCollection
	{
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
        bool PreProcessExecution(PreProcessExecutionInfo executionInfo, out ExecutionMode executionResult);

        /// <summary>
        /// called when the host received Cancel request. NOTE: this method can return while
        /// operation is still being canceled
        /// </summary>
        /// <returns>
        /// true if the host should do standard cancel for the currently running view or
        /// false if the Cancel operation was done entirely inside this method and there is nothing
        /// extra that should be done
        /// </returns>
        bool Cancel();

        /// <summary>
        /// called after dialog's host executes actions on all panels in the dialog one by one
        /// NOTE: it might be called from worker thread
        /// </summary>
        /// <param name="executionMode">result of the execution</param>
        /// <param name="runType">type of execution</param>
        void PostProcessExecution(RunType runType, ExecutionMode executionMode);

        /// <summary>
        /// called before dialog's host executes OnReset method on all panels in the dialog one by one
        /// NOTE: it might be called from worker thread
        /// </summary>
        /// <returns>
        /// true if regular execution should take place, false if everything
        /// has been done by this function
        /// </returns>
        bool PreProcessReset();

        /// <summary>
        /// called after dialog's host executes OnReset method on all panels in the dialog one by one
        /// NOTE: it might be called from worker thread
        /// </summary>
        void PostProcessReset();
	}
}
