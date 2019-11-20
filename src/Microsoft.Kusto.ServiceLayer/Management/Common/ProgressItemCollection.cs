//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Globalization;
using System.Collections;
using System.Threading;
using System.Text;

namespace Microsoft.Kusto.ServiceLayer.Management
{
	/// <summary>
	/// Allows for the mapping of objects that implement IProgressItem to individual items in the
	/// progress dialog.
	/// </summary>
	public class ProgressItemCollection : ICollection
	{
		#region internal helper classes
		/// <summary>
		/// Allows us to map an action to its index in the progress dialog.
		/// </summary>
		public class ActionIndexMap
		{
			/// <summary>
			/// action
			/// </summary>
			public IProgressItem Action;
			/// <summary>
			/// index
			/// </summary>
			public int Index;

			public ActionIndexMap(IProgressItem action)
			{
				this.Action = action;
				// index isn't known yet
				this.Index = -1;
			}
		}
		#endregion

		#region private data members
		/// <summary>
		/// list of actions we will perform.
		/// </summary>
		private ArrayList actions = new ArrayList();
		#endregion

		#region construction
		public ProgressItemCollection()
		{
		}
		#endregion

		#region properties

		private bool closeOnUserCancel = false;
		/// <summary>
		/// Indicates whether to close the dialog immediately if the user cancels an operation
		/// </summary>
		public bool CloseOnUserCancel
		{
			get
			{
				return closeOnUserCancel;
			}
			set
			{
				closeOnUserCancel = value;
			}
		}

		private bool automaticClose = false;
		/// <summary>
		/// Indicates whether to automatically close the dialog when all actions are complete
		/// successfully.
		/// </summary>
		public bool CloseOnSuccessfulCompletion
		{
			get
			{
				return automaticClose;
			}
			set
			{
				automaticClose = value;
			}
		}

		private bool quitOnError = false;
		/// <summary>
		/// Indicates whether the operation should be terminated if any individual step fails.
		/// </summary>
		public bool QuitOnError
		{
			get
			{
				return this.quitOnError;
			}
			set
			{
				this.quitOnError = value;
			}
		}
		private OperationStatus operationStatus = OperationStatus.Invalid;
		/// <summary>
		/// Indicates the status of the operation.
		/// </summary>
		public OperationStatus OperationStatus
		{
			get
			{
				return this.operationStatus;
			}
		}
		/// <summary>
		/// Progress object this action collection will work with
		/// </summary>
		private IProgress progress = null;
		public IProgress Progress
		{
			get
			{
				return this.progress;
			}
			set
			{
				if (this.progress != value)
				{
					this.progress = value;
					if (this.progress != null)
					{
						// add the actions to the progress dialog, and
						// fixup our event handler
						FixUpActionsToProgress();
					}
				}
			}
		}
		#endregion

		#region public overrides
		/// <summary>
		/// Generate a string representaion of this object. It will convert all of it's IProgressItem members
		/// to strings in a new line.
		/// </summary>
		/// <returns>string description of the actions this object contains</returns>
		public override string ToString()
		{
			// if there are no actions then just return the default ToString
			if (this.actions == null || this.actions.Count == 0)
			{
				return base.ToString();
			}
			else
			{
				// convert all of the actions to strings on their own line
				StringBuilder sb = new StringBuilder(((ActionIndexMap)actions[0]).Action.ToString());
				for (int i = 1; i < this.actions.Count; i++)
				{
					sb.AppendFormat(CultureInfo.InvariantCulture, "\r\n{0}", ((ActionIndexMap)actions[i]).Action.ToString());
				}
				return sb.ToString();
			}
		}
		#endregion

		#region ICollection implementation
		/// <summary>
		/// Gets the number of actions in this collection
		/// </summary>
		public int Count
		{
			get
			{
				return this.actions.Count;
			}
		}
		/// <summary>
		/// not supported
		/// </summary>
		public bool IsSynchronized
		{
			get
			{
				throw new NotSupportedException();
			}
		}
		/// <summary>
		/// not supported
		/// </summary>
		public object SyncRoot
		{
			get
			{
				throw new NotSupportedException();
			}
		}
		public void CopyTo(IProgressItem[] array, int start)
		{
			this.actions.CopyTo(array, start);
		}
		public void CopyTo(Array array, int start)
		{
			this.actions.CopyTo(array, start);
		}
		public IEnumerator GetEnumerator()
		{
			return this.actions.GetEnumerator();
		}
		#endregion

		#region public methods
		/// <summary>
		/// Add an action to the collection
		/// </summary>
		/// <param name="action">action to be added</param>
		public void AddAction(IProgressItem action)
		{
			ActionIndexMap map = new ActionIndexMap(action);
			this.actions.Add(map);
		}		

		#endregion

		#region internal implementation
		/// <summary>
		/// delegate called when the progress dialog wants us to perform work on a new thread.
		/// </summary>
		private void DoWorkOnThread()
		{
			if (this.Progress == null)
			{
				return;
			}

			try
			{
				System.Threading.Thread.CurrentThread.Name = "Worker thread for " + progress.GetType();
			}
			catch (InvalidOperationException)
			{ }

			// default to succeeded.
			operationStatus = OperationStatus.Success;

			// carry out each action.
			foreach (ActionIndexMap map in this.actions)
			{
				// abort if the user has decided to cancel.
				if (this.Progress.IsAborted)
				{
					this.Progress.UpdateActionStatus(map.Index, ProgressStatus.Aborted);
					operationStatus = OperationStatus.Aborted;
					break;
				}
				ProgressStatus stepStatus = ProgressStatus.Invalid;
				try
				{
					// perform the action.
					stepStatus = map.Action.DoAction(this, map.Index);
					this.Progress.UpdateActionStatus(map.Index, stepStatus);
				}
				catch (Exception e)
				{
					// fail the step with errors, add the error messages to the control.
					this.Progress.AddActionException(map.Index, e);
					this.Progress.UpdateActionStatus(map.Index, ProgressStatus.Error);
					stepStatus = ProgressStatus.Error;
				}
				if (stepStatus == ProgressStatus.Error)
				{
					// see if we're supposed to fail if any step fails
					if (this.QuitOnError == true)
					{
						// fail and quit
						this.operationStatus = OperationStatus.Error;
						break;
					}
					else
					{
						this.operationStatus = OperationStatus.CompletedWithErrors;
					}
				}
				else if (stepStatus != ProgressStatus.Success)
				{
					this.operationStatus = OperationStatus.CompletedWithErrors;
				}
			}

			// tell the dialog we're finishing.
			this.Progress.WorkerThreadExiting(operationStatus);

			// close the dialog if asked to. We have to put this after
			// the WorkerThreadExiting call because the progress dialog
			// won't allow itself to be closed until worker thread says 
			// it's finished.
			if ((this.CloseOnSuccessfulCompletion && (this.operationStatus == OperationStatus.Success)) ||
				(this.CloseOnUserCancel && this.Progress.IsAborted))
			{
				//((Form)this.Progress).BeginInvoke(new CloseProgressWindowCallback(CloseProgressWindowHandler), new object[] { this.Progress });
			}
		}

		private delegate void CloseProgressWindowCallback(IProgress progress);
		private void CloseProgressWindowHandler(IProgress progress)
		{
		}

		/// <summary>
		/// Adds the actions to an IProgress interface.
		/// </summary>
		private void FixUpActionsToProgress()
		{
			if (this.Progress == null)
			{
				return;
			}
			// add actions
			foreach (ActionIndexMap map in this.actions)
			{
				map.Index = this.Progress.AddAction(map.Action.ToString());
			}
			// add our delegate
			this.Progress.WorkerThreadStart = new ThreadStart(this.DoWorkOnThread);
		}
		#endregion
	}
}
