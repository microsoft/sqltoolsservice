//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Management.Sdk.Sfc;
using System;
using Microsoft.SqlServer.Management.Common;
using System.Collections.Generic;
using System.Collections;
using Microsoft.SqlServer.Management.Diagnostics;
using System.Collections.ObjectModel;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    public class GridColumnType
    {
        public const int 
            Text = 1, 
            Button = 2, 
            Bitmap = 3,
            Checkbox = 4,
			Hyperlink = 5,
            FirstCustomColumnType = 0x400;
    };

    // /// <summary>
    // /// ILogViewer provide an enviroment for operating with logs
    // ///     -- e.g. LogViewerForm having buttons for Load,Export,Search,Filter,Help,etc
    // /// </summary>
    // internal interface ILogViewer
    // {
    //     bool ActionInProgress   {get; set;}
    //     void EnableCommand      (LogViewerCommand command, bool enable);
    //     void ExecuteCommand     (LogViewerCommand command, LogViewerCommandOption option, params object [] args);

    //     ILogSourceTypeFactory   SourceTypeFactory {get;}
        
    //     IColumnManager          ColumnManager {get;}
    //     IConstraintManager      FilterManager {get;}
    //     ISearchManager          SearchManager {get;}

    //     IServiceProvider        ServiceProvider {get;}
    //     SqlConnectionInfo       SqlCI {get;}
    // }

    // /// <summary>
    /// ILogSourceTypeFactory knows to instantiate objects that deal with various types of logs
    ///     -- e.g. a factory that knows how to handle SqlServer, SqlAgent and Windows NT logs
    /// </summary>
    internal interface ILogSourceTypeFactory
    {
        ILogSourceType[] SourceTypes {get;}
    }

    /// <summary>
    /// ILogSourceType describes the interface for log source types
    ///     -- e.g. SqlServer, SqlAgent, Windows NT, file-stream, etc...
    /// </summary>
    internal interface ILogSourceType
    {
        string Name { get;}
        ILogSource[] Sources { get;}

        void Refresh();
    }

    // /// <summary>
    // /// LogSourceType describes a log source type 
    // ///     -- e.g. SqlServer, SqlAgent, Windows NT, file-stream, etc...
    // /// 
    // /// $ISSUE: 235833-2007/10/07-disoulio
    // /// We have to decouple the LogSourceType code from the Tree View controller in order
    // /// to make it more extensible if we decide to change UI representation
    // /// </summary>
    // internal abstract class LogSourceType: ILogSourceType
    // {
    //     #region Variables
    //     protected ILogSource[] logSources = null;
    //     #endregion

    //     public abstract string  Name    {get;}
    //     public ILogSource[] Sources{get { return logSources; } }

    //     public abstract void Refresh();

    //     /// <summary>
    //     /// This function updates the Source tree with the sources from the bricktypes
    //     /// </summary>
    //     /// <param name="initialSelections"></param>
    //     /// <param name="caller"></param>
    //     public virtual void UpdateSourceTree(LogViewerAggregatorSelection[] initialSelections, LogViewerAggregator caller)
    //     {
    //         bool expand = false;
    //         TreeNode tn = new TreeNode(this.Name);
    //         tn.Tag = this;

    //         STrace.Assert(logSources != null, "Log Source Type returned NULL .Sources");
    //         if (logSources != null)
    //         {
    //             int logNumber = 0;
    //             foreach (ILogSource logSource in this.logSources)
    //             {
    //                 Microsoft.SqlServer.Management.Diagnostics.STrace.Assert(logSource != null, "source type returned null ILogSource");

    //                 TreeNode treeNodeChild = new TreeNode(logSource.Name);
    //                 treeNodeChild.Tag = logSource;

    //                 tn.Nodes.Add(treeNodeChild);

    //                 //we call the expand selected to check if a node is selected so as to expand it's parent
    //                 if (this.ExpandSelected(treeNodeChild, logNumber, initialSelections, caller, logSource))
    //                 {
    //                     expand = true;
    //                 }
    //                 logNumber++;
    //             }
    //         }

    //         caller.TreeControl.Nodes.Add(tn);
    //         if (expand)
    //         {
    //             caller.TreeControl.SelectedNode = tn;
    //             tn.Expand();
    //         }
    //     }

    //     /// <summary>
    //     /// Gets a list of all the selected <see cref="ILogSource"/> for this Source Type. 
    //     /// </summary>
    //     /// <param name="root"></param>
    //     /// <returns>A mapping of <see cref="ILogSource"/> objects and their full source type names</returns>
    //     public virtual IDictionary<ILogSource,string> GetSelectedSources(TreeNode root)
    //     {
    //         IDictionary<ILogSource, string> sourceAndTypeDictionary = new Dictionary<ILogSource, string>();
    //         foreach (TreeNode tnLogSource in root.Nodes)
    //         {
    //             Microsoft.SqlServer.Management.Diagnostics.STrace.Assert(tnLogSource.Tag is ILogSource);
    //             ILogSource logSource = tnLogSource.Tag as ILogSource;

    //             if (tnLogSource.Checked)
    //             {
    //                 //this is the name of the aggregation log source (it helps to ensure saved items are not tooked from cache and not anymore re-initialized)
    //                 sourceAndTypeDictionary.Add(logSource, LogViewerSR.FullSourceName(this.Name, logSource.Name));

    //                 // add any subsources as well.
    //                 if (logSource.SubSources != null)
    //                 {
    //                     foreach (ILogSource subSource in logSource.SubSources)
    //                     {
    //                         sourceAndTypeDictionary.Add(subSource, LogViewerSR.FullSourceName(this.Name, subSource.Name));
    //                     }
    //                 }
    //             }
    //         }

    //         return sourceAndTypeDictionary;
    //     }

    //     /// <summary>
    //     /// This function checks wether the node should be selected and expands it 
    //     /// </summary>
    //     /// <param name="treeNodeChild"></param>
    //     /// <param name="logNumber"></param>
    //     /// <param name="initialSelections"></param>
    //     /// <param name="caller"></param>
    //     /// <param name="logSource"></param>
    //     /// <returns></returns>
    //     public bool ExpandSelected(TreeNode treeNodeChild, int logNumber, 
    //         LogViewerAggregatorSelection[] initialSelections, LogViewerAggregator caller, ILogSource logSource)
    //     {
    //         bool expand = false;
    //         try
    //         {
    //             if (initialSelections != null)
    //             {
    //                 foreach (LogViewerAggregatorSelection sel in initialSelections)
    //                 {
    //                     if (this.IsSelected(sel, logSource, caller, logNumber))
    //                     {
    //                         expand = true;
    //                     }
    //                 }

    //                 if (expand)
    //                 {
    //                     treeNodeChild.Checked = true;
    //                     treeNodeChild.Parent.Checked = true; // treeNodeChild.Checked = true -> did not trigger the autoupdating logic of aftercheck event, so... we manualy ensure parent is checked
    //                 }

    //             }
    //         }
    //         catch (Exception ex)
    //         {
    //             if ((caller.ViewerEnvironment != null) && (caller.ViewerEnvironment.ServiceProvider != null) && (caller.InvokeRequired == false))
    //             {
    //                 IMessageBoxProvider mbp = (IMessageBoxProvider)caller.ViewerEnvironment.ServiceProvider.GetService(typeof(IMessageBoxProvider));

    //                 if (mbp != null)
    //                 {
    //                     mbp.ShowMessage(ex.Message, LogViewerSR.LogAccess_ErrorAccessingLog(logSource.Name), Microsoft.NetEnterpriseServers.ExceptionMessageBoxButtons.OK, Microsoft.NetEnterpriseServers.ExceptionMessageBoxSymbol.Warning, caller.ViewerEnvironment.Owner);
    //                 }
    //             }

    //             return false;
    //         }

    //         return expand;
    //     }

    //     /// <summary>
    //     /// The check if selected is used to check if the current node matches the given selection. 
    //     /// I have made a seperate function for that because then the other log types just need to override this function
    //     /// in order to change the way they check the initial selections
    //     /// </summary>
    //     /// <param name="selection"></param>
    //     /// <param name="logSource"></param>
    //     /// <param name="caller"></param>
    //     /// <returns></returns>
    //     protected virtual bool IsSelected(LogViewerAggregatorSelection selection, ILogSource logSource, LogViewerAggregator caller, int logNumber)
    //     {
    //         if (selection.LogTypeName == this.Name)
    //         {
    //             // sel.LogSourceName will be non null if the maint plan node or job node was selected
    //             if (selection.LogSourceName != null)
    //             {
    //                 if (selection.LogSourceName == logSource.Name)
    //                 {
    //                     return true;
    //                 }
    //             }
    //             else if (selection.logNumber == logNumber ||  // logNumber matches
    //                      selection.logNumber == -1)      // or 'all' (-1) was specified
    //             {
    //                 return true;
    //             }
    //         }

    //         return false;
    //     }

    //     /// <summary>
    //     /// Function used when a refresh is called. It refreshes every log type and recreated teh initial selections
    //     /// </summary>
    //     /// <param name="tnLogType"></param>
    //     /// <param name="arExistingSelections"></param>
    //     public virtual void RefreshSourceNodes(TreeNode tnLogType, IList arExistingSelections)
    //     {
    //         int logIdx = 0;
    //         // compute list with existing selections for a given log type
    //         foreach (TreeNode tnLogSource in tnLogType.Nodes)
    //         {
    //             if (tnLogSource.Checked == true)
    //             {
    //                 // for primary log sources (by design) persisting position of the checkbox (so using null instead of logSource.Name since logSource.Name can change - e.g. recycle agent logs)
    //                 arExistingSelections.Add(new LogViewerAggregatorSelection(this.Name, null, logIdx, null, false));
    //             }

    //             ++logIdx;
    //         }

    //         // and refresh that log type
    //         this.Refresh();
    //     }
    // }

    /// <summary>
    /// ILogSource describes a log source
    ///     -- e.g. Current SqlServer log, Archive #3 of Sql Agent, NT Security log, etc...
    /// </summary>
    public interface ILogSource
    {
        string          Name    {get;}
        /// <summary>
        /// We allow only one initialization for the source. This is because upon aggregation we create a new LogSourceAggregation that
        /// contains the seperate sources and initialize it. So if a source is already initialized from previous collection we shouldn't
        /// initialize it again because we will have duplicate data.
        /// </summary>
        void            Initialize();
        ILogSource[]    SubSources {get;}
        ILogEntry CurrentEntry { get;}
        string[]        FieldNames {get;}
        ILogSource      GetRefreshedClone();
        bool            ReadEntry();
        void            CloseReader();
        bool            OrderedByDateDescending {get;}
    }

    // /// <summary>
    // /// ILogEntry describes a log entry
    // ///     -- e.g. a logon successufull message within an NT Security log, or message about an xp_xxx executed withing SqlServer log, etc...
    // /// </summary>
    // public interface ILogEntry
    // {
    //     string          OriginalSourceTypeName  {get;}
    //     string          OriginalSourceName      {get;}
    //     SeverityClass   Severity                {get;}
    //     DateTime        PointInTime             {get;}
    //     string          this[string fieldName]  {get;}
    //     bool            CanLoadSubEntries       {get;}
    //     List<ILogEntry> SubEntries              {get;}
    // }


    /// <summary>
    /// some ILogSources may support custom commands (like "Delete" supported by Maint Plans and Job History)
    /// 
    /// if they do they have to implement this interface and respond to
    /// Support(command) - telling us if it supports a particular command
    ///                     null - particular custom command not supported
    ///                     else - who will handle that command when executed
    /// 
    /// a command handler may be shared between multiple command targets
    /// current implementation of LogViewerAggregator enables UI controls
    /// like "Delete" button if all commands share same handler
    /// </summary>
    // internal interface ILogCommandTarget
    // {
    //     ILogCommandHandler      GetCommandHandler(LogViewerCommand command);
    // }

    /// <summary>
    /// a command handler executes a command on multiple targets
    /// all selected targets should share same command handler
    /// 
    /// in theory a command handler can support multiple commands
    /// 
    /// e.g. execute pop-up UI for "Delete" and execute it on multiple selected
    ///     ILogSources that support it (Job History-s, Maint Plans)
    /// </summary>
    // internal interface ILogCommandHandler
    // {
    //     void                    Execute(ILogViewer enviroment, LogViewerCommand command, ILogCommandTarget[] targets);
    //     // TODO: make this completly dinamic and have it provide its own ToolbarButton[]-s that should merge with LogViewer standard buttons
    // }

    /// <summary>
    /// ILogAggregator describes an algorithm that agregates multiple ILogSources
    ///     -- e.g. algorithm that interleaves multiple logs sources based on log entry times
    /// </summary>
    internal interface ILogAggregator
    {
        ILogSource                  PrepareAggregation  (string outputLogSourceName, ILogSource[] sources, ILogConstraints filter);
        //DelegateAggregationWork     AggregateDelegate   {get;} // you can use async invocation with callback to execute it in background (clr will use the thread pool)
        void                        CancelAsyncWork(); //CancelAsyncWork stops the current aggregation and at the end closes all the open readers for the sources
        void                        StopAsyncWork(); // StopAsyncWork does the same thing as CancelAsyncWork but instead it leaves the readers open because this is used in incremental aggregation so we will resume the collection by adding the new source selected

        //event       DelegateAggregationProgress OnAggregationProgress;
    }

    /// <summary>
    /// used for filtering and searching log entries
    /// </summary>
    internal interface ILogConstraints
    {
        bool                        MatchLogEntry(ILogEntry entry);
        bool                        IsEntryWithinFilterTimeWindow(ILogEntry entry);
    }

    // /// <summary>
    // /// IColumnManager
    // ///     -- e.g. manages what columns should be displayed by consumer and what columns should be not
    // /// </summary>
    // internal interface IColumnManager
    // {
    //     string[]    VisibleColumns {get;}
    //     string[]    AvailableColumns {set;}
    //     void        MoveColumn(int from, int to);

    //     event DelegateColumnsInformationChanged OnVisibleColumnsChanged;
    //     event DelegateColumnsInformationChanged OnAvailableColumnsChanged;
    // }

    internal interface ITypedColumns
    {
        TypedColumnCollection ColumnTypes { get; }
        void HyperLinkClicked(string sourcename, string columnname, string hyperlink, long row, int column);
    }

    // /// <summary>
    // /// ISearchManager
    // ///     -- manages constraints associated with the search ui
    // /// </summary>
    // internal interface ISearchManager
    // {
    //     object      ConstraintObject {get; set;}

    //     event DelegateConstraintInformation     OnSearch;
    // }


    // /// <summary>
    // /// ILogConsumer describes a component that consumes a ILogSource
    // ///     -- e.g. ui component that displays log entries in a grid
    // /// </summary>
    // internal interface ILogConsumer
    // {
    //     ILogDataStorage     Source {get; set;}
    //     ILogViewer          Enviroment {set;}
    //     event DelegateAggregationProgress onReportProgress;
    // }


#if false

    /// <summary>
    /// Interface for the storage view that creates a view of the data storage class
    /// </summary>
    internal interface ILogStorageView : IStorageView
    {
        ILogEntry EntryAt(long row);
        LogColumnInfo.RowInfo GetRowInfoAt(int row);
        ReadOnlyCollection<string> VisibleFieldNames { get; }
        void Clear();
        bool IsRowExpandable(int rowIndex);
        bool IsRowExpanded(int rowIndex);
        int RowLevel(int rowIndex);
        int ExpandRow(int rowIndex);
        int CollapseRow(int rowIndex);
        ReadOnlyCollection<string> VisibleSourceNames { get; }
    }

    /// <summary>
    /// Interface for the Data storage class that can be either memory based or disk based.
    /// </summary>
    internal interface ILogDataStorage : IDataStorage, ITypedColumns
    {
        void Initialize();
        /// <summary>
        /// returns a view of the storage with the applied filter or a plain view if the filter is disabled.
        /// </summary>
        /// <returns></returns>
        ILogStorageView GetFilteredView(); 
        /// <summary>
        /// stops the storing of the data, This is used in the incremental aggregation so as to stop the collection without closing the readers
        /// </summary>
        void StopStoringData(); 
        /// <summary>
        /// cancels the collection and implies that the readers should be closed
        /// </summary>
        void CancelStoring(); 
        StorageNotifyDelegate StorageNotifyDelegate { get; set;}
        ReadOnlyCollection<string> FieldNames { get;}
        ILogSource AggregationSource { get; set;}

        ReadOnlyCollection<string> VisibleFieldNames { get; set; }
        /// <summary>
        /// is used when no filter is applied in order to avoid going into aggregation mode and just output all available entries to the consumer
        /// </summary>
        bool NotifyAll { get; set; } 
        bool IsCanceled { get;}
        /// <summary>
        /// is used to return a list with the row numbers that correspond to all the entries from the demanded sources
        /// </summary>
        /// <returns></returns>
        IList<long> GetDemandedSourcesRowIndexList();
        /// <summary>
        /// This returns the fitler that was defined when we started the collection.
        /// This filter was pushed to the server
        /// </summary>
        LogConstraints CollectionFilter { get; set;}
        /// <summary>
        /// This returns the client side side filter that is currently set in order to 
        /// show a filtered view of the collected data
        /// </summary>
        LogConstraints ClientFilter { get; set;}
    }

    /// <summary>
    /// Interface for the the sorting view which keeps a mapping of relative rows and absolute rows 
    /// based on the column key
    /// </summary>
    internal interface ILogSortedView : IComparer<int>
    {
        void Initialize(ILogStorageView view);
        int KeyIndex { get; set; }
        bool IsDescending { get; set; }
        void SortData();
        int GetAbsoluteRowNumber(int iRelativeRow);
        void StopSortingData();
        int SortedRows();
        void ReverseSorting();
        void ExpandRow(int iRow, int subRowsCount);
        void CollapseRow(int iRow, int subRowsCount);
        void ExpandParentRows(ILogStorageView view);
        StorageNotifyDelegate StorageNotifyDelegate { get; set; }
    }

    /// <summary>
    /// used by ui thread to schedule async invocations (ui should use AsyncCallback to get notified when compleated)
    /// </summary>
    internal delegate void DelegateAggregationWork(ILogDataStorage storage);

    /// <summary>
    /// delegate used to notify that asyncronous aggregation job was completed
    ///     -- e.g. an aggregator finished initializing some of its inner sources a ui component that displays progress of aggregation will be called
    /// </summary>
    internal delegate void DelegateAggregationProgress(object sender,
                                                     string message, 
                                                     int percentage, 
                                                     IList<Exception> exceptionList);

    /// <summary>
    /// event that is triggered when user changes visible columns or available columns
    ///     -- e.g. user pop-ed up the ui beoyind a IColumnManager and wants to apply changes
    /// </summary>
    internal delegate void DelegateColumnsInformationChanged (object sender, string[] columns);

    /// <summary>
    /// severity associated with a log entry (ILogEntry)
    //  these should be ordered least severe to most severe where possible.
    /// </summary>
    public enum SeverityClass
    {
        Unknown = -1,
        Success,
        Information,
        SuccessAudit,
        InProgress,
        Retry,
        Warning,
        FailureAudit,
        Cancelled,
        Error
    }

    /// <summary>
    /// command that can be executed by a log viewer (ILogViewer)
    /// </summary>
    internal enum LogViewerCommand
    {
        Load = 0,
        Refresh,
        Export,
        Columns,
        Filter,
        Search,
        Delete,
        Help,
        Close,
        Cancel
    }

    /// <summary>
    /// command options
    /// </summary>
    internal enum LogViewerCommandOption
    {
        None = 0,
        Hide,
        Show
    }

    /// <summary>
    /// Event arguments for various events
    /// related to LogViewer
    /// </summary>
    internal class LogViewerEventArgs : EventArgs
    {
        private LogViewerCommand command;
	
        public LogViewerEventArgs(LogViewerCommand command)
        {
            this.command = command;
        }

        public LogViewerCommand Command
        {
            get { return command; }
        }
    }
#endif
}








