//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Drawing;
using System.Threading;
using System.Runtime.InteropServices;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
	/// <summary>
	/// Enumeration for status of individual actions
	/// </summary>
	public enum ProgressStatus
	{
		Invalid = -1,
		NotStarted,			// Not started
		InProgress,			// In progress
		Success,			// Completed
		SuccessWithInfo,	// Completed, display additional info
		Warning,			// Completed with warning, display exceptions
		Error,				// Not completed because of error, display exceptions
		Aborted,			// Aborted
		RolledBack,			// Rolled back because of a subsequent error
		StatusCount			// = Number of status values - For validation only
	}

	/// <summary>
	/// Enumeration for status of the overall operation
	/// </summary>
	public enum OperationStatus
	{
		Invalid = -1,
		InProgress,				// In progress
		Success,				// Completed successfully
		CompletedWithErrors,	// Completed with non-fatal errors
		Error,					// Not completed because of error
		Aborted,				// Abort complete
		StatusCount				// = Number of status values - For validation only
	}

	/// <summary>
	/// Interface defining core functionality of a progress control container
	/// 
	/// NOTE: Refer to the comments for each method/property individually to determine 
	///       whether it is thread-safe and may be used from the worker thread. Also note
	///       that some members are asynchronous.      
	/// </summary>
	public interface IProgress
	{
		//-----------
		// Properties
		//-----------		

		/// <summary>
		/// The property that determines if the user should be allowed to abort the 
		/// operation. The default value is true.
		/// 
		/// NOTE: This property is not thread safe. Set from UI thread.
		/// </summary>
		bool AllowAbort
		{
			get;
			set;
		}

		/// <summary>
		/// The confirmation prompt to display if the user hits the Abort button.
		/// The abort prompt should be worded such that the abort is confirmed 
		/// if the user hits "Yes".
		/// 
		/// NOTE: This property is not thread safe. Set from UI thread.
		/// </summary>
		string AbortPrompt
		{
			get;
			set;
		}

		/// <summary>
		/// The ThreadStart delegate. The method referenced by this delegate is run by
		/// the worker thread to perform the operation.
		/// 
		/// NOTE: This property is not thread safe. Set from UI thread.
		/// </summary>
		ThreadStart WorkerThreadStart
		{
			get;
			set;
		}

		/// <summary>
		/// Aborted status of the operation. 
		/// 
		/// NOTE: This property is thread safe and may be called from the worker
		///       thread. Accessing this property may cause caller to block if UI
		///       is waiting for user confirmation of abort.
		/// </summary>
		bool IsAborted
		{
			get;
		}

		/// <summary>
		/// This property determines whether updates should be allowed for
		/// actions. If this is set to false, any calls that are made
		/// to add or update an action are ignored. The default value is true.
		/// 
		/// NOTE: This property is thread safe and may be called from the worker
		///       thread.
		/// </summary>
		bool ActionUpdateEnabled
		{
			get;
			set;
		}

		//--------
		// Methods
		//--------

		/// <summary>
		/// Add an action to the displayed list of actions. Actions are 
		/// displayed in the order they are added. An action can be referenced
		/// in future calls using the zero-based index that is returned.
		/// 
		/// The description must be a non-empty string.
		/// 
		/// NOTE: This method is not thread safe. Call from the UI thread. 
		/// </summary>
		/// <param name="description">Description of the action</param>
		/// <returns>The index of the newly added action.</returns>
		int AddAction(string description);

		/// <summary>
		/// Add an action to the displayed list of actions. This is meant
		/// to be called from the worker thread to add an action after
		/// the operation is already in progress.
		/// 
		/// Actions are displayed in the order they are added. Use the 
		/// zero-based index of the action based on past actions added
		/// to reference it in future calls. The description must be a 
		/// non-empty string.
		///
		/// NOTE: This method is thread safe and asynchronous. It may be
		///       called from the worker thread. 
		/// </summary>
		/// <param name="description">Description of the action</param>
		void AddActionDynamic(string description);

		/// <summary>
		/// Update the description of an action 
		/// 
  		/// The description must be a non-empty string.
  		/// 
		/// NOTE: This method is thread safe and asynchronous. It may be
		///       called from the worker thread.
		/// </summary>
		/// <param name="actionIndex">Index of the action</param>
		/// <param name="description">New description of the action</param>
		void UpdateActionDescription(int actionIndex, string description);

		/// <summary>
		/// Update the status of an action
		/// 
		/// NOTE: This method is thread safe and asynchronous. It may be
		///       called from the worker thread.
		/// </summary>
 		/// <param name="actionIndex">Index of the action</param>
		/// <param name="status">New status of the action</param>
		void UpdateActionStatus(int actionIndex, ProgressStatus status);

		/// <summary>
		/// Update the progress of an action in terms of percentage complete
		/// 
		/// NOTE: This method is thread safe and asynchronous. It may be
		///       called from the worker thread.
		/// </summary>
		/// <param name="actionIndex">Index of the action</param>
		/// <param name="percentComplete">Percentage of the action that is complete (0-100)</param>
		void UpdateActionProgress(int actionIndex, int percentComplete);

		/// <summary>
		/// Update the progress of an action with a text description of 
		/// the progress
		/// 
  		/// The description must be a non-empty string.
  		/// 
		/// NOTE: This method is thread safe and asynchronous. It may be
		///       called from the worker thread.
		/// </summary>
 		/// <param name="actionIndex">Index of the action</param>
		/// <param name="description">Description of progress</param>
		void UpdateActionProgress(int actionIndex, string description);

		/// <summary>
		/// Add an exception to an action
		/// 
		/// Exceptions are displayed in the action grid only for actions 
		/// with "Error" or "Warning" status.
		/// 
		/// NOTE: This method is thread safe and asynchronous. It may be
		///       called from the worker thread.
		/// </summary>
 		/// <param name="actionIndex">Index of the action</param>
		/// <param name="e">Exception to be added</param>
		void AddActionException(int actionIndex, Exception e);

		/// <summary>
		/// Add an info string to an action in the progress report control
		///
		/// Information strings are displayed in the action grid only for 
		/// actions with "SuccessWithInfo" status. The info string must 
		/// be a non-empty string. It should not be formatted or contain
		/// newline characters.
		///
		/// NOTE: This method is thread safe and asynchronous. It may be
		///       called from the worker thread.
		/// </summary>
 		/// <param name="actionIndex">Index of the action</param>
		/// <param name="infoString">Information string to be added</param>
		void AddActionInfoString(int actionIndex, string infoString);

		/// <summary>
		/// Call this method when the worker thread performing the operation
		/// is about to exit. The final result of the operation is supplied in
		/// the form of a OperationStatus value.
		/// 
		/// NOTE: This method is thread safe and asynchronous. It may be
		///       called from the worker thread.
		/// </summary>
		/// <param name="result">Result of the operation</param>
		void WorkerThreadExiting(OperationStatus result);
	}

	/// <summary>
	/// Enumeration for status of the progress report control w.r.t the operation
	/// </summary>
	[System.Runtime.InteropServices.ComVisible(false)]
	public enum ProgressCtrlStatus
	{
		Invalid = -1,
		InProgress,				// In progress
		Success,				// Completed successfully
		CompletedWithErrors,	// Completed with non-fatal errors
		Error,					// Not completed because of error
		Aborting,				// User clicked "Abort", aborting operation
		Aborted,				// Abort complete
		Closed,					// User clicked "Close"
		StatusCount				// = Number of status values - For validation only
	}

	/// <summary>
	/// Delegate used with ProgressCtrlStatusChanged event.
	/// </summary>
	public delegate void ProgressCtrlStatusChangedEventHandler(object source, ProgressCtrlStatusChangedEventArgs e);

	/// <summary>
	/// EventArgs class for use with ProgressCtrlStatusChanged event
	/// </summary>
	sealed public class ProgressCtrlStatusChangedEventArgs : EventArgs 
	{
		//------------------
		// Public Properties
		//------------------		
		public ProgressCtrlStatus Status
		{
			get { return m_status; }
			set { m_status = value; }
		}

		//-------------
		// Private Data
		//-------------
		private ProgressCtrlStatus m_status = ProgressCtrlStatus.Invalid;
	}

	// Enumeration for progress action grid columns
	internal enum ProgressActionColumn
	{
		Invalid = -1,
		ActionStatusBitmap,
		ActionDescription,
		ActionStatusText,
		ActionMessage,
		ActionColumnCount	// = Number of columns - For validation only
	}
	
	// Enumeration for progress action display filter
	internal enum ProgressActionDisplayMode
	{
		Invalid = -1,
		DisplayAllActions,
		DisplayErrors,
		DisplaySuccess,
		DisplayWarnings,
		ActionDisplayModeCount	// = Number of display modes - For validation only
	}	
}
