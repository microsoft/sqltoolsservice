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

    /// <summary>
    /// ILogAggregator describes an algorithm that agregates multiple ILogSources
    ///     -- e.g. algorithm that interleaves multiple logs sources based on log entry times
    /// </summary>
    internal interface ILogAggregator
    {
        ILogSource                  PrepareAggregation  (string outputLogSourceName, ILogSource[] sources, ILogConstraints filter);
        void                        CancelAsyncWork(); //CancelAsyncWork stops the current aggregation and at the end closes all the open readers for the sources
        void                        StopAsyncWork(); // StopAsyncWork does the same thing as CancelAsyncWork but instead it leaves the readers open because this is used in incremental aggregation so we will resume the collection by adding the new source selected

    }

    /// <summary>
    /// used for filtering and searching log entries
    /// </summary>
    internal interface ILogConstraints
    {
        bool                        MatchLogEntry(ILogEntry entry);
        bool                        IsEntryWithinFilterTimeWindow(ILogEntry entry);
    }

    internal interface ITypedColumns
    {
        TypedColumnCollection ColumnTypes { get; }
        void HyperLinkClicked(string sourcename, string columnname, string hyperlink, long row, int column);
    }

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








