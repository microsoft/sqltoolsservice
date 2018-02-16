//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Management.Sdk.Sfc;
using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using Microsoft.SqlServer.Management.Diagnostics;
using System.Runtime.CompilerServices;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{

#region LogSourceAggregation - ILogSource info built from multiple other sources
    internal class LogSourceAggregation : ILogSource, ITypedColumns, IDisposable
    {
#region Constants
        private const int cMaximumNotificationChunkSize = 128; // 16384 high no: faster aggregation, low no: responsive ui
#endregion

#region Variables
        private string m_logName = null;
        private bool m_logInitialized = false;
        private string[] m_fieldNames = null;
        private TypedColumnCollection m_columnTypes = null;

        List<ILogSource> m_originalSources = null;
        List<ILogSource> m_sources = null;
        ILogConstraints m_filter = null;
        private LogAggregator m_owner = null;
        private ILogEntry m_currentEntry = null;
        private List<ILogEntry> m_currentEntrySources = null;
        private List<Exception> m_exceptionList = null;

#endregion

#region Reverse order Property
        private bool ReverseOrder
        {
            get
            {
                return true;
            }
        }
#endregion

#region Constructor
        /// <summary>
        /// 
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="name"></param>
        /// <param name="sources"></param>
        /// <param name="filter">if null no filter, else use it to filter every ILogEntry</param>
        public LogSourceAggregation (LogAggregator owner, string name, ILogSource[] sources, ILogConstraints filterTemplate)
        {
            m_owner = owner;

            m_logName = name;
            m_originalSources = new List<ILogSource>(sources);

            m_fieldNames = AggregateFieldNames(sources);

            AggregateColumnTypes(sources);

            // if (filterTemplate != null)
            // {               
            //     m_filter = new LogConstraints(this, filterTemplate as LogConstraints);
            // }
            // else
            {
                m_filter = null;
            }
        }

        public void Dispose()
        {
            for (int i = 0; i < m_originalSources.Count; ++i)
            {
                if (m_originalSources[i] is IDisposable)
                {
                    (m_originalSources[i] as IDisposable).Dispose();
                }
            }

            m_currentEntry = null;
            m_sources = null;
            m_exceptionList = null;
        }

#endregion

#region ILogSource interface implementation

        bool ILogSource.OrderedByDateDescending
        {
            get {return this.ReverseOrder;}
        }

        ILogEntry ILogSource.CurrentEntry
        {
            get
            {
                return m_currentEntry;
            }
        }

        bool ILogSource.ReadEntry()
        {           
            //the m_currentEntrySources list contains the list of currentEntries for each logSource
            //when the readentry is called for the first time the list is null so we need to initialize it
            if (m_currentEntrySources == null)
            {
                m_sources = new List<ILogSource>(m_originalSources);
                m_currentEntrySources = new List<ILogEntry>(m_sources.Count);
                for (int i = 0; i < m_sources.Count; i++)
                {
                    //the null value acts as a guard that indicates whether we have read all the entries from the current source
                    //or if an error happened. That is why I initialize all the sources with null
                    m_currentEntrySources.Add(null);
                    try
                    {

                        if (m_sources[i].CurrentEntry != null || (m_sources[i].ReadEntry()))
                        {
                            m_currentEntrySources[i] = m_sources[i].CurrentEntry;
                            if (m_filter != null)
                            {
                                while (!m_filter.MatchLogEntry(m_sources[i].CurrentEntry))
                                {
                                    //check if cancel
                                    if (IsCanceled())
                                    {
                                        return false;
                                    }

                                    if (m_sources[i].ReadEntry())
                                    {
                                        m_currentEntrySources[i] = m_sources[i].CurrentEntry;

                                        if (m_filter != null && !m_filter.IsEntryWithinFilterTimeWindow(m_currentEntrySources[i]))
                                        {
                                            m_currentEntrySources[i] = null;
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        m_currentEntrySources[i] = null;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception e) //whenever a source issued an exception, the exception is stored in the exception list and the source is removed from the list
                    {
                        AddExceptionToExceptionList(e, m_sources[i].Name);
                        m_currentEntrySources[i] = null;
                    }

                }
            }

            //we check the currentEntrySources again to see if there are any entries read from the source
            //if not it means that either the source has no entries (that satisfy the filter if a filter is defined) 
            //or an error happened so we need to close the reader and remove the source.
            for (int i = 0; i < m_currentEntrySources.Count; i++)
            {
                if (m_currentEntrySources[i] == null)
                {
                    m_sources[i].CloseReader();
                    m_sources.RemoveAt(i);
                    m_currentEntrySources.RemoveAt(i);
                    i--; //we need this to make the indexer point at the previous log source
                }
            }

            int sourceindex = -1;

            if (m_sources.Count == 1 && m_currentEntrySources[0] != null)
            {
                sourceindex = 0;
            }
            else
            {
                DateTime maxtime = DateTime.MinValue;
                for (int i = 0; i < m_sources.Count; i++)
                {
                    if (maxtime.CompareTo(m_currentEntrySources[i].PointInTime) <= 0)
                    {
                        maxtime = m_currentEntrySources[i].PointInTime;
                        sourceindex = i;
                    }
                }
            }

            if (sourceindex > -1)
            {
                m_currentEntry = m_sources[sourceindex].CurrentEntry;
                try
                {
                    do
                    {
                        //check if cancel
                        if (IsCanceled())
                        {
                            return false;
                        }

                        if (m_sources[sourceindex].ReadEntry())
                        {
                            m_currentEntrySources[sourceindex] = m_sources[sourceindex].CurrentEntry;

                            if (m_filter != null && !m_filter.IsEntryWithinFilterTimeWindow(m_currentEntrySources[sourceindex]))
                            {
                                m_currentEntrySources[sourceindex] = null;
                                break;
                            }
                        }
                        else
                        {
                            m_currentEntrySources[sourceindex] = null;
                            break;
                        }

                    }
                    while (m_filter != null && !m_filter.MatchLogEntry(m_sources[sourceindex].CurrentEntry));
                }
                catch (Exception e) //whenever a source issued an exception, the exception is stored in the exception list and the source is removed from the list
                {
                    AddExceptionToExceptionList(e, m_sources[sourceindex].Name);
                    m_currentEntrySources[sourceindex] = null;
                }

            }
            else
            {
                return false;
            }

            return true;
        }

        void ILogSource.CloseReader()
        {
            foreach (ILogSource source in m_originalSources)
            {
                source.CloseReader();
            }

            m_currentEntrySources = null;
            m_currentEntry = null;
            m_exceptionList = null;

        }

        string ILogSource.Name
        {
            get
            {
                return m_logName;
            }
        }

        void ILogSource.Initialize()
        {
            if (m_logInitialized == true)
            {
                return;
            }

            // initialize original sources
            int n = m_originalSources.Count;
            int i = 0;
            for (i = 0; i < m_originalSources.Count; i++)
            {

                ILogSource s = m_originalSources[i];

                try
                {
                    // format notification message
                    m_owner.Raise_AggregationProgress("Raise_AggregationProgress", //LogViewerSR.AggregationProgress_Initialize(i + 1, n, (s.Name != null) ? s.Name.Trim() : String.Empty),
                                                      0,
                                                      null);

                    // initialize (load) inner source
                    s.Initialize();
                }
                catch (Exception e) //whenever a source issued an exception, the exception is stored in the exception list and the source is removed from the list
                {
                    AddExceptionToExceptionList(e, s.Name);
                    m_originalSources.RemoveAt(i);
                    s.CloseReader();
                    i--;
                }

                // check for cancel
                if (IsCanceled())
                {
                    return;
                }
            }

            // report all inner source loaded
            m_owner.Raise_AggregationProgress("LogViewerSR.AggregationProgress_InitializationDone", 
                                              LogAggregator.cProgressLoaded,
                                              null);


            m_logInitialized = true;
        }

        string[] ILogSource.FieldNames
        {
            get
            {
                return m_fieldNames;
            }
        }

        TypedColumnCollection ITypedColumns.ColumnTypes
        {
            get
            {
                return m_columnTypes;
            }
        }

        ILogSource[] ILogSource.SubSources
        {
            get { return null;}
        }

        ILogSource      ILogSource.GetRefreshedClone()
        {
            return this;
        }

#endregion

#region Implementation
        /// <summary>
        /// computes the available fields for the aggregated log source
        /// </summary>
        /// <param name="sources"></param>
        internal static string[] AggregateFieldNames(ILogSource[] sources)
        {
            List<string> ar = new List<string>();

            foreach(ILogSource s in sources)
            {
                if ((s != null) && (s.FieldNames != null))
                {
                    foreach(string fieldName in s.FieldNames)
                    {
                        if (ar.Contains(fieldName))
                        {
                            continue; // do not add it again
                        }
                        ar.Add(fieldName);
                    }
                }
            }

            return ar.ToArray();
        }

        /// <summary>
        /// computes the available column types for the aggregated log source
        /// </summary>
        /// <param name="sources"></param>
        private void AggregateColumnTypes(ILogSource[] sources)
        {
            m_columnTypes = new TypedColumnCollection();

            foreach (ILogSource s in sources)
            {
                if (s is ITypedColumns)
                {
                    ITypedColumns cs = (ITypedColumns)s;
                    if ((cs != null) && (cs.ColumnTypes != null))
                    {
                        foreach (string fieldName in s.FieldNames)
                        {
                            m_columnTypes.AddColumnType(fieldName, cs.ColumnTypes.GetColumnType(fieldName));
                        }
                    }
                }
            }

        }

        /// <summary>
        /// checks to see if somebody decided to cancel or stop the operation
        /// </summary>
        private bool IsCanceled()
        {
            return m_owner.CancelInternal || m_owner.StopInternal;
        }


        public IList<Exception> ExceptionList
        {
            get
            {
                return m_exceptionList;
            }
        }

        public void ClearExceptionList()
        {
            m_exceptionList = null;
        }

        private void AddExceptionToExceptionList(Exception e, string sourceName)
        {
            e.Source = sourceName;

            if (m_exceptionList == null)
            {
                m_exceptionList = new List<Exception>();
            }
            m_exceptionList.Add(e);
        }

#endregion

#region [Conditional("DEBUG")] validate correctness of a log source
        /// <summary>
        /// validate if entries are in correct order
        /// 
        /// costly operation so we compile this only if "DEBUG" is defined
        /// iterates through all the entries and if their datetime is different the
        /// DateTime.MinValue or DateTime.MaxValue compares it with adjacent entries
        /// 
        /// we do not compare subentries as aggregation is performed
        /// only at entries level (subentries are always linked to thier parent entry)
        /// 
        /// the order should be ascending  (newer logs are after older logs)  if reverseOrder = false
        /// the order should be descending (newer logs are before older logs) if reverseOrder = true
        /// </summary>
        /// <param name="entries"></param>
        /// <param name="reverseOrder"></param>
        [System.Diagnostics.Conditional("DEBUG")]
        private static void ConditionalDEBUG_ValidateLogEntriesOrder(List<ILogEntry> entries, 
                                                                     bool reverseOrder)
        {
            System.Diagnostics.Debug.WriteLine("LogSourceAggregation.ConditionalDEBUG_ValidateLogEntriesOrder ------- reverseOrder=" + reverseOrder.ToString());
            
            if ((entries == null) || (entries.Count < 2))
            {
                return;
            }

            for (int i = 0; i < (entries.Count - 1); ++i)
            {
                int j = i + 1;

                DateTime dti = entries[i].PointInTime;
                DateTime dtj = entries[j].PointInTime;

                if (
                   (dti != DateTime.MinValue) && (dti != DateTime.MaxValue) &&
                   (dtj != DateTime.MinValue) && (dtj != DateTime.MaxValue)
                   )
                {
                    // if logs are comming from same source then we dont Assert since it is not
                    // the aggregator algoritm to blame but the log source provider who broke
                    // the assumption that log sources are coming already pre-sorted
                    if ((entries[i].OriginalSourceTypeName == entries[j].OriginalSourceTypeName) &&
                        (entries[i].OriginalSourceName == entries[j].OriginalSourceName))
                    {
                        continue;
                    }
                }
            }
        }
#endregion


        #region ITypedColumns Members


        public void HyperLinkClicked(string sourcename, string columnname, string hyperlink, long row, int column)
        {
            foreach (ILogSource s in m_originalSources)
            {
                if ((s is ITypedColumns) &&
                    (s.Name == sourcename)) // The original source for the row containing the hyperlink
                {
                    ((ITypedColumns)s).HyperLinkClicked(sourcename, columnname, hyperlink, row, column);
                }
            }
        }

        #endregion
    }
#endregion

    #region TypedColumnCollection

    internal class TypedColumnCollection
    {
        private Dictionary<string, int> m_typedColumns = null;

        internal TypedColumnCollection()
        {
            m_typedColumns = new Dictionary<string, int>();
        }

        internal void AddColumnType(string columnName, int columnType)
        {
            if (!m_typedColumns.ContainsKey(columnName))
            {
                m_typedColumns.Add(columnName, columnType);
            }
        }

        internal int GetColumnType(string columnName)
        {
            int returnType;
            if (m_typedColumns.TryGetValue(columnName, out returnType))
            {
                return returnType;
            }
            return GridColumnType.Text;
        }

        internal bool IsEmpty
        {
            get
            {
                return m_typedColumns.Count == 0;
            }
        }
    }

    #endregion
    #region LogAggregator class - ILogAggregator algorithm
    /// <summary>
    /// Summary description for LogAggregator.
    /// </summary>
    internal class LogAggregator : ILogAggregator
    {
#region Constants
        internal const int cProgressLogCreated      =   1;
        internal const int cProgressLoaded          =  15;
        internal const int cProgressAlmostDone      =  95;
        internal const int cProgressDone            = 100;
#endregion

#region Properties - CancelInternal (lock-ed access)
        private volatile bool m_boolCancelInternal = false;
        internal bool CancelInternal
        {
            get
            {

                return m_boolCancelInternal;

            }
            set
            {
                lock (this)
                {
                    m_boolCancelInternal = value;
                }
            }
        }

        private volatile bool m_boolStopInternal = false;
        internal bool StopInternal
        {
            get
            {

                return m_boolStopInternal;

            }
            set
            {
                lock (this)
                {
                    m_boolStopInternal = value;
                }
            }
        }


        private bool m_reverseOrder = true;
        internal bool ReverseOrder
        {
            get
            {
                return m_reverseOrder;
            }
            set
            {
                m_reverseOrder = value;
            }
        }
#endregion

#region Variables
        //private DelegateAggregationWork m_aggregationworkDelegate;
        private LogSourceAggregation m_currentSource = null;
#endregion

#region Constructor
        /// <summary>
        /// create an log aggregator using a default empty cache
        /// </summary>
        public LogAggregator()
        {
           // m_aggregationworkDelegate = new DelegateAggregationWork(this.DelegateAggregationWorkImplementation);
        }

#endregion

#region ILogAggregator interface implementation
        ILogSource ILogAggregator.PrepareAggregation(string outputLogSourceName, ILogSource[] sources, ILogConstraints filterTemplate)
        {
            ILogSource outputSource = CreateUninitializedAggregation(outputLogSourceName, sources, filterTemplate);

            m_currentSource = outputSource as LogSourceAggregation;
            
            return outputSource;
        }


        // DelegateAggregationWork ILogAggregator.AggregateDelegate
        // {
        //     get
        //     {
        //         STrace.Assert(m_aggregationworkDelegate != null);
        //         return m_aggregationworkDelegate;
        //     }
        // }

        void ILogAggregator.CancelAsyncWork()
        {
            CancelInternal = true;
        }


        void ILogAggregator.StopAsyncWork()
        {
             StopInternal = true;
        }

        // private DelegateAggregationProgress m_aggregationprogressDelegate = null;
        // event DelegateAggregationProgress ILogAggregator.OnAggregationProgress
        // {
        //     [MethodImpl(MethodImplOptions.Synchronized)]
        //     add
        //     {
        //         STrace.Assert(value!=null);

        //         m_aggregationprogressDelegate = (DelegateAggregationProgress) Delegate.Combine(m_aggregationprogressDelegate, value);
        //     }

        //     [MethodImpl(MethodImplOptions.Synchronized)]
        //     remove
        //     {
        //         m_aggregationprogressDelegate = (DelegateAggregationProgress) Delegate.Remove(m_aggregationprogressDelegate, value);
        //     }
        // }
#endregion

#region CreateUninitializedAggregation algorithm
        /// <summary>
        /// agregates one or more sources -> creates a new (uninitialized) aggregation
        /// 
        /// NOTE:
        ///     we also 'aggregate' only 1 source to gain the advantage offered by this algoritm
        ///     of being able to pump entry-s to ui thread in chucks instead of sending all source
        ///     in one shoot -> more responsive ui
        /// </summary>
        /// <param name="outputLogSourceName"></param>
        /// <param name="sources"></param>
        /// <param name="constraints">null if no filter</param>
        /// <returns></returns>
        private ILogSource CreateUninitializedAggregation(string outputLogSourceName, ILogSource[] sources, ILogConstraints filterTemplate)
        {
            // STrace.Assert(outputLogSourceName!=null);
            // STrace.Assert(outputLogSourceName.Trim().Length != 0);
            // STrace.Assert(sources!=null);
            // STrace.Assert(sources.Length != 0);

            // zero sources - nothing we can do
            if ((sources == null) || (sources.Length==0))
            {
                return null;
            }

            ILogSource newAggregation = null;
            try
            {
                // foreach (ILogSource source in sources)
                // {
                //     if (source is LogSourceSqlServer)
                //     {
                //         (source as LogSourceSqlServer).Filter = filterTemplate;
                //     }
                // }

                // not in cache, so build it, add it to cache (if caching ok) and return it
                newAggregation = new LogSourceAggregation(this, outputLogSourceName, sources, filterTemplate);

                return newAggregation;
                
            }
            finally
            {
                Raise_AggregationProgress( "LogViewerSR.AggregationProgress_BeginInitialize", 
                                          cProgressLogCreated,
                                          null);
            }
        }
#endregion

#region DelegateAggregationWorkImplementation - entry for - asynchronous invocation with callback ***** via delegate
        private List<Exception> m_exceptionsList = new List<Exception>();
        // private void DelegateAggregationWorkImplementation(ILogDataStorage storage)
        // {
        //     DateTime dtTimeStart = DateTime.Now;
        //     try
        //     {
        //         CancelInternal = false;
        //         StopInternal = false;

        //         storage.Initialize();

        //         //STrace.Assert(storage.AggregationSource is LogSourceAggregation, "Aggregation source must be of type LogSourceAggregation");

        //         if (storage.AggregationSource != null &&
        //             (storage.AggregationSource as LogSourceAggregation).ExceptionList != null) //if the exception list is not null we should notify for the errors
        //         {
        //             m_exceptionsList.AddRange((storage.AggregationSource as LogSourceAggregation).ExceptionList);
        //             (storage.AggregationSource as LogSourceAggregation).ClearExceptionList();
        //         }

        //         if (!StopInternal)
        //         {
        //             //we close the readers only when a stop hasn't issued
        //             if (storage.AggregationSource != null)
        //             {
        //                 storage.AggregationSource.CloseReader();
        //             }

        //             if (m_exceptionsList.Count > 0) //if the exception list is not empty we should notify for the errors
        //             {
        //                 Raise_AggregationProgress(null, cProgressDone, m_exceptionsList);
        //                 m_exceptionsList.Clear();
                        
        //             }
        //             else
        //             {
        //                 // report success
        //                 Raise_AggregationProgress(LogViewerSR.AggregationProgress_Done(Convert.ToInt32(storage.GetFilteredView().NumRows())),
        //                                           cProgressDone,
        //                                           null);
        //             }
        //         }


        //     }
        //     catch (LogOperationCanceledException e)
        //     {
        //         STrace.Trace("LogViewer", "EXCEPTION LogAggregator.WorkerEntryPoint got: " + e.Message);

        //         if (storage.AggregationSource != null)
        //         {
        //             storage.AggregationSource.CloseReader();
        //         }

        //         Raise_AggregationProgress(LogViewerSR.AggregationProgress_Stopped(e.EntriesProcessed),
        //                                     cProgressDone,
        //                                     null);

        //     }
        //     //this code catches exceptions related to the general aggregation work, exceptions generated from readers during
        //     //the collection are caught internally and are kept in the logsourceaggregator exceptionList so as not to interrupt the collection
        //     catch (Exception e) 
        //     {
        //         STrace.Trace("LogViewer", "EXCEPTION LogAggregator.WorkerEntryPoint got: " + e.Message);
        //         if (storage.AggregationSource != null)
        //         {
        //             storage.AggregationSource.CloseReader();
        //         }

        //         List<Exception> exceptionList = new List<Exception>(); //we create a list here because the RaiseAggregationDelegate accepts a IList<exception>
        //         exceptionList.Add(e);

        //         // report failure
        //         Raise_AggregationProgress(null,
        //                                   cProgressDone,
        //                                   exceptionList);

        //     }
        //     finally
        //     {
        //         m_currentSource = null;
        //         STrace.Trace("LogViewer", "*** track: source life: 005 ... (background=" + System.Threading.Thread.CurrentThread.IsBackground + ":" + System.Threading.Thread.CurrentThread.GetHashCode() + ") LogAggregator.DelegateAggregationWorkImplementation() delegate finally ending --- timespan:" + DateTime.Now.Subtract(dtTimeStart).ToString());
        //     }
        // }
#endregion

#region Report Progress
        /// <summary>
        /// if job not null and callbackProgress available -> invoke progress delegate in ui thread
        /// </summary>
        /// <param name="job"></param>
        /// <param name="message"></param>
        /// <param name="percent"></param>
        internal void Raise_AggregationProgress(string message, 
                                                int percent,
                                                IList<Exception> exceptionList)
        {
            // STrace.Assert(m_aggregationprogressDelegate != null, "nobody interested in aggregation's progress");
            // STrace.Assert(message != null || exceptionList != null, "message and exception cannot both be null");
            // STrace.Assert(percent >= 0, "percent should be >= 0");
            // STrace.Assert(percent <= 100, "percent should be <= 100");

            // if (m_aggregationprogressDelegate != null)
            // {
            //     m_aggregationprogressDelegate(this, message, percent, exceptionList);
            // }
        }

#endregion
    }
#endregion
}

