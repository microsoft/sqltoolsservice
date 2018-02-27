//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Text;
using System.Xml;
using Microsoft.SqlTools.ServiceLayer.Admin;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo.Agent;
using SMO = Microsoft.SqlServer.Management.Smo;


namespace Microsoft.SqlTools.ServiceLayer.Agent
{

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

    public interface ILogEntry
    {
        string          OriginalSourceTypeName  {get;}
        string          OriginalSourceName      {get;}
        SeverityClass   Severity                {get;}
        DateTime        PointInTime             {get;}
        string          this[string fieldName]  {get;}
        bool            CanLoadSubEntries       {get;}
        List<ILogEntry> SubEntries              {get;}
    }


    internal class LogSourceJobHistory : ILogSource, IDisposable //, ITypedColumns, ILogCommandTarget
    {
        internal const string JobDialogParameterTemplate = "<params><jobid></jobid></params>";
        internal const string JobStepDialogParameterTemplate = "<params><jobid></jobid><acceptedits>true</acceptedits></params>";

        internal enum DialogType
        {
            JobDialog = 0,
            JobStepDialog = 1
        }

        #region Variables
        private string m_jobName = null;
        //assigning invalid jobCategoryId
        public int m_jobCategoryId = -1;
        private Guid m_jobId = Guid.Empty;
        private SqlConnectionInfo m_sqlConnectionInfo = null;

        private List<ILogEntry> m_logEntries = null;
        private string m_logName = null;
        private bool m_logInitialized = false;
        private string[] m_fieldNames = null;
        private ILogEntry m_currentEntry = null;
        private int m_index = 0;
        private bool m_isClosed = false;
        private TypedColumnCollection columnTypes;
        private IServiceProvider serviceProvider = null;

       // ILogCommandHandler m_customCommandHandler = null;

        private static string historyTableDeclaration   = "declare @tmp_sp_help_jobhistory table";
        private static string historyTableDeclaration80 = "create table #tmp_sp_help_jobhistory";
        private static string historyTableName   = "@tmp_sp_help_jobhistory";
        private static string historyTableName80 = "#tmp_sp_help_jobhistory";
        private static string jobHistoryQuery =
@"{0}
(
    instance_id int null, 
    job_id uniqueidentifier null, 
    job_name sysname null, 
    step_id int null, 
    step_name sysname null, 
    sql_message_id int null, 
    sql_severity int null, 
    message nvarchar(4000) null, 
    run_status int null, 
    run_date int null, 
    run_time int null, 
    run_duration int null, 
    operator_emailed sysname null, 
    operator_netsent sysname null, 
    operator_paged sysname null, 
    retries_attempted int null, 
    server sysname null  
)

insert into {1} 
exec msdb.dbo.sp_help_jobhistory 
    @job_id = '{2}',
    @mode='FULL' 
        
SELECT
    tshj.instance_id AS [InstanceID],
    tshj.sql_message_id AS [SqlMessageID],
    tshj.message AS [Message],
    tshj.step_id AS [StepID],
    tshj.step_name AS [StepName],
    tshj.sql_severity AS [SqlSeverity],
    tshj.job_id AS [JobID],
    tshj.job_name AS [JobName],
    tshj.run_status AS [RunStatus],
    CASE tshj.run_date WHEN 0 THEN NULL ELSE
    convert(datetime, 
            stuff(stuff(cast(tshj.run_date as nchar(8)), 7, 0, '-'), 5, 0, '-') + N' ' + 
            stuff(stuff(substring(cast(1000000 + tshj.run_time as nchar(7)), 2, 6), 5, 0, ':'), 3, 0, ':'), 
            120) END AS [RunDate],
    tshj.run_duration AS [RunDuration],
    tshj.operator_emailed AS [OperatorEmailed],
    tshj.operator_netsent AS [OperatorNetsent],
    tshj.operator_paged AS [OperatorPaged],
    tshj.retries_attempted AS [RetriesAttempted],
    tshj.server AS [Server],
    getdate() as [CurrentDate]
FROM {1} as tshj
ORDER BY [InstanceID] ASC";

        #endregion

        #region Public Property - used by Delete handler
        public string JobName
        {
            get
            {
                return m_jobName;
            }
        }
        #endregion

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                // dispose child ILogSources...
                ILogSource me = this as ILogSource;
                if (me.SubSources != null)
                {
                    for (int i = 0; i < me.SubSources.Length; ++i)
                    {
                        IDisposable d = me.SubSources[i] as IDisposable;
                        if (d != null)
                        {
                            d.Dispose();
                        }
                    }
                }

                if (m_currentEntry != null)
                {
                    m_currentEntry = null;
                }
            }
        }



        #region Constructor
        

        public LogSourceJobHistory(string jobName, SqlConnectionInfo sqlCi, object customCommandHandler, int jobCategoryId, Guid JobId, IServiceProvider serviceProvider)
        {
            m_logName = jobName;
            m_jobCategoryId = jobCategoryId;
            m_jobId = JobId;
            m_logEntries = new List<ILogEntry>();

            m_jobName = jobName;
            m_sqlConnectionInfo = sqlCi;
            m_fieldNames = new string[] 
            {
                "LogViewerSR.Field_StepID",
                "LogViewerSR.Field_Server",
                "LogViewerSR.Field_JobName",
                "LogViewerSR.Field_StepName",
                "LogViewerSR.Field_Notifications",
                "LogViewerSR.Field_Message",
                "LogViewerSR.Field_Duration",
                "LogViewerSR.Field_SqlSeverity",
                "LogViewerSR.Field_SqlMessageID",
                "LogViewerSR.Field_OperatorEmailed",
                "LogViewerSR.Field_OperatorNetsent",
                "LogViewerSR.Field_OperatorPaged",
                "LogViewerSR.Field_RetriesAttempted"
            };

            columnTypes = new TypedColumnCollection();
            for (int i = 0; i < m_fieldNames.Length; i++)
            {
                columnTypes.AddColumnType(m_fieldNames[i], SourceColumnTypes[i]);
            }

            //m_customCommandHandler = customCommandHandler;
            
            this.serviceProvider = serviceProvider;
        }
        #endregion

        private static int[] SourceColumnTypes
        {
            get
            {
                return new int[] 
                {
                    GridColumnType.Hyperlink,
                    GridColumnType.Text,
                    GridColumnType.Hyperlink,
                    GridColumnType.Text,
                    GridColumnType.Text,
                    GridColumnType.Text,
                    GridColumnType.Text,
                    GridColumnType.Text,
                    GridColumnType.Text,
                    GridColumnType.Text,
                    GridColumnType.Text,
                    GridColumnType.Text,
                    GridColumnType.Text
                };
            }
        }

        public TypedColumnCollection ColumnTypes
        {
            get
            {
                return columnTypes;
            }
        }
        #region ILogSource interface implementation

        bool ILogSource.OrderedByDateDescending
        {
            get { return false; }
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
            if (!m_isClosed && m_index >= 0)
                {
                    m_currentEntry = m_logEntries[m_index--];

                    return true;
                }
                else
                {
                    return false;
                }
        }

        void ILogSource.CloseReader()
        {
            m_index = m_logEntries.Count -1;
            m_isClosed = true;
            m_currentEntry = null;
            return;
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

            // do the actual initialization, retrieveing the ILogEntry-s
            InitializeInternal();
            m_logInitialized = true;
        }

        ILogSource[] ILogSource.SubSources
        {
            get { return null; }
        }

        string[] ILogSource.FieldNames
        {
            get
            {
                return m_fieldNames;
            }
        }

        ILogSource ILogSource.GetRefreshedClone()
        {
            return new LogSourceJobHistory(m_jobName, m_sqlConnectionInfo, null, m_jobCategoryId, m_jobId, this.serviceProvider);

        }
        #endregion

        #region Implementation
        /// <summary>
        /// does the actual initialization by retrieving Server/ErrorLog/Text via enumerator
        /// </summary>
        private void InitializeInternal()
        {
            m_logEntries.Clear();

            IDbConnection connection = null;
            try
            {
                connection = m_sqlConnectionInfo.CreateConnectionObject();
                connection.Open();

                IDbCommand command = connection.CreateCommand();

                string jobId = this.m_jobId.ToString();

                string query = 
                      (this.m_sqlConnectionInfo.ServerVersion == null 
                    || this.m_sqlConnectionInfo.ServerVersion.Major >= 9) ?

                                String.Format(jobHistoryQuery,
                                              historyTableDeclaration,
                                              historyTableName,
                                              jobId) :

                                String.Format(jobHistoryQuery,
                                              historyTableDeclaration80,
                                              historyTableName80,
                                              jobId);


                command.CommandType = CommandType.Text;
                command.CommandText = query;

                DataTable dtJobHistory = new DataTable();
                SqlDataAdapter adapter = new SqlDataAdapter((SqlCommand)command);
                adapter.Fill(dtJobHistory);

                int n = dtJobHistory.Rows.Count;

                // populate m_logEntries with ILogEntry-s that have (ref to us, rowno)
                for (int rowno = 0; rowno < n; rowno++)
                {
                    // we will create here only the job outcomes (0) - entries, and skip non 0
                    // job outcome (step 0) it will extract the sub-entries (steps 1...n) itself
                    ILogEntry jobOutcome = new LogEntryJobHistory(m_jobName, dtJobHistory, rowno);

                    int skippedSubentries = (jobOutcome.SubEntries == null) ? 0 : jobOutcome.SubEntries.Count;
                    rowno += skippedSubentries; // skip subentries

                    m_logEntries.Add(jobOutcome);
                }

                m_index = m_logEntries.Count - 1;
            }
            finally
            {
                if (connection != null)
                {
                    connection.Close();
                }
            }
        }
        #endregion

        #region internal class - LogEntryJobHistory
        /// <summary>
        /// LogEntryJobHistory - represents a SqlServer log entry
        /// </summary>
        internal class LogEntryJobHistory : ILogEntry
        {
            #region Constants
            internal const string cUrnRunDate = "RunDate";
            internal const string cUrnRunStatus = "RunStatus";
            internal const string cUrnRunDuration = "RunDuration";
            internal const string cUrnJobName = "JobName";
            internal const string cUrnStepID = "StepID";
            internal const string cUrnStepName = "StepName";
            internal const string cUrnMessage = "Message";
            internal const string cUrnSqlSeverity = "SqlSeverity";
            internal const string cUrnSqlMessageID = "SqlMessageID";
            internal const string cUrnOperatorEmailed = "OperatorEmailed";
            internal const string cUrnOperatorNetsent = "OperatorNetsent";
            internal const string cUrnOperatorPaged = "OperatorPaged";
            internal const string cUrnRetriesAttempted = "RetriesAttempted";
            internal const string cUrnServerName = "Server";
            internal const string cUrnServerTime = "CurrentDate";

            internal const string cUrnInstanceID = "InstanceID"; // internally used by Agent to mark order in which steps were executed

            #endregion

            #region Variables
            string m_originalSourceName = null;
            DateTime m_pointInTime = DateTime.MinValue;
            SeverityClass m_severity = SeverityClass.Unknown;

            string m_fieldJobName = null;
            string m_fieldStepID = null;
            string m_fieldStepName = null;
            string m_fieldMessage = null;
            string m_fieldDuration = null;
            string m_fieldSqlSeverity = null;
            string m_fieldSqlMessageID = null;
            string m_fieldOperatorEmailed = null;
            string m_fieldOperatorNetsent = null;
            string m_fieldOperatorPaged = null;
            string m_fieldRetriesAttempted = null;
            private string m_serverName = null;
            List<ILogEntry> m_subEntries = null;
            #endregion

            #region Constructor
            /// <summary>
            /// constructor used by log source to create 'job outcome' entries
            /// </summary>
            /// <param name="sourceName"></param>
            /// <param name="dt">data table containing all history info for a job</param>
            /// <param name="rowno">index for row that describes 'job outcome', rowno+1..n will describe 'job steps'</param>
            public LogEntryJobHistory(string sourceName, DataTable dt, int rowno)
            {
                InitializeJobHistoryStepSubEntries(sourceName, dt, rowno); // create subentries, until we hit job outcome or end of history

                // initialize job outcome
                if ((m_subEntries != null) && (m_subEntries.Count > 0))
                {
                    // are we at the end of history data set?
                    if ((rowno + m_subEntries.Count) < dt.Rows.Count)
                    {
                        // row following list of subentries coresponds to an outcome job that already finished
                        InitializeJobHistoryFromDataRow(sourceName, dt.Rows[rowno + m_subEntries.Count]);
                    }
                    else
                    {
                        // there is no row with stepID=0 that coresponds to a job job outcome for a job that is running
                        // since agent will write the outcome only after it finishes the job therefore we will build ourselves
                        // an entry describing the running job, to which we will host the subentries for job steps already executed
                        InitializeJobHistoryForRunningJob(sourceName, dt.Rows[rowno]);
                    }
                }
                else
                {                   
                    InitializeJobHistoryFromDataRow(sourceName, dt.Rows[rowno]);
                }                
            }

            /// <summary>
            /// constructor used by a parent log entry to create child 'job step' sub-entries
            /// </summary>
            /// <param name="sourceName"></param>
            /// <param name="dr">row describing subentry</param>
            public LogEntryJobHistory(string sourceName, DataRow dr)
            {
                InitializeJobHistoryFromDataRow(sourceName, dr); // initialize intself                
            }
            #endregion

            #region Implementation
            /// <summary>
            /// builds an entry based on a row returned by enurerator - that can be either a job step or a job outcome
            /// </summary>
            /// <param name="dr"></param>
            private void InitializeJobHistoryFromDataRow(string sourceName, DataRow dr)
            {
                try
                {
                    m_originalSourceName = sourceName;

                    m_pointInTime = Convert.ToDateTime(dr[cUrnRunDate], System.Globalization.CultureInfo.InvariantCulture);
                    m_serverName = Convert.ToString(dr[cUrnServerName], System.Globalization.CultureInfo.InvariantCulture);
                    m_fieldJobName = Convert.ToString(dr[cUrnJobName], System.Globalization.CultureInfo.InvariantCulture);
                    switch ((Microsoft.SqlServer.Management.Smo.Agent.CompletionResult)Convert.ToInt32(dr[cUrnRunStatus], System.Globalization.CultureInfo.InvariantCulture))
                    {
                        case CompletionResult.Cancelled:
                            m_severity = SeverityClass.Cancelled;
                            break;
                        case CompletionResult.Failed:
                            m_severity = SeverityClass.Error;
                            break;
                        case CompletionResult.InProgress:
                            m_severity = SeverityClass.InProgress;
                            break;
                        case CompletionResult.Retry:
                            m_severity = SeverityClass.Retry;
                            break;
                        case CompletionResult.Succeeded:
                            m_severity = SeverityClass.Success;
                            break;
                        case CompletionResult.Unknown:
                            m_severity = SeverityClass.Unknown;
                            break;
                        default:                            
                            m_severity = SeverityClass.Unknown;
                            break;
                    }

                    //
                    // check our subentries, see if any of them have a worse status than we do.
                    // if so, then we should set ourselves to SeverityClass.Warning
                    //
                    if (this.m_subEntries != null)
                    {
                        for (int i = 0; i < this.m_subEntries.Count; i++)
                        {
                            if (this.m_subEntries[i].Severity > this.m_severity &&
                                (this.m_subEntries[i].Severity == SeverityClass.Retry ||
                                 this.m_subEntries[i].Severity == SeverityClass.Warning ||
                                 this.m_subEntries[i].Severity == SeverityClass.FailureAudit ||
                                 this.m_subEntries[i].Severity == SeverityClass.Error))
                            {
                                this.m_severity = SeverityClass.Warning;
                                break;
                            }
                        }
                    }

                    // if stepId is zero then dont show stepID and step name in log viewer
                    // Valid step Ids starts from index 1
                    int currentStepId = (int)dr[cUrnStepID];
                    if (currentStepId == 0)
                    {
                        m_fieldStepID = String.Empty;
                        m_fieldStepName = String.Empty;

                    }
                    else
                    {
                        m_fieldStepID = Convert.ToString(currentStepId, System.Globalization.CultureInfo.CurrentCulture);
                        m_fieldStepName = Convert.ToString(dr[cUrnStepName], System.Globalization.CultureInfo.CurrentCulture);
                    }
                    
                    m_fieldMessage = Convert.ToString(dr[cUrnMessage], System.Globalization.CultureInfo.CurrentCulture);
                    m_fieldSqlSeverity = Convert.ToString(dr[cUrnSqlSeverity], System.Globalization.CultureInfo.CurrentCulture);
                    m_fieldSqlMessageID = Convert.ToString(dr[cUrnSqlMessageID], System.Globalization.CultureInfo.CurrentCulture);
                    m_fieldOperatorEmailed = Convert.ToString(dr[cUrnOperatorEmailed], System.Globalization.CultureInfo.CurrentCulture);
                    m_fieldOperatorNetsent = Convert.ToString(dr[cUrnOperatorNetsent], System.Globalization.CultureInfo.CurrentCulture);
                    m_fieldOperatorPaged = Convert.ToString(dr[cUrnOperatorPaged], System.Globalization.CultureInfo.CurrentCulture);
                    m_fieldRetriesAttempted = Convert.ToString(dr[cUrnRetriesAttempted], System.Globalization.CultureInfo.CurrentCulture);

                    Int64 hhmmss = Convert.ToInt64(dr[cUrnRunDuration], System.Globalization.CultureInfo.InvariantCulture); // HHMMSS
                    int hh = Convert.ToInt32(hhmmss / 10000, System.Globalization.CultureInfo.InvariantCulture);
                    int mm = Convert.ToInt32((hhmmss / 100) % 100, System.Globalization.CultureInfo.InvariantCulture);
                    int ss = Convert.ToInt32(hhmmss % 100, System.Globalization.CultureInfo.InvariantCulture);
                    m_fieldDuration = Convert.ToString(new TimeSpan(hh, mm, ss), System.Globalization.CultureInfo.CurrentCulture);
                }
                catch (InvalidCastException)
                {
                    // keep null data if we hit some invalid info
                }
            }

            /// <summary>
            /// builds sub-entries (steps 1...n), until we find a 'job outcome' (step0) or end of history (meaning job is in progress)
            /// </summary>
            /// <param name="dt"></param>
            /// <param name="rowno">points to 1st subentry => points to 1st 'job step'</param>
            private void InitializeJobHistoryStepSubEntries(string sourceName, DataTable dt, int rowno)
            {
                if (m_subEntries == null)
                {
                    m_subEntries = new List<ILogEntry>();
                }
                else
                {
                    m_subEntries.Clear();
                }

                int i = rowno;
                while (i < dt.Rows.Count)
                {
                    DataRow dr = dt.Rows[i];

                    object o = dr[cUrnStepID];

                    try
                    {
                        int stepID = Convert.ToInt32(o, System.Globalization.CultureInfo.InvariantCulture);

                        if (stepID == 0)
                        {
                            // we found the 'job outcome' for our set of steps
                            break;
                        }

                        //
                        // we want to have the subentries ordered newest to oldest, 
                        // which is the same time order as the parent nodes themselves. 
                        // that's why we add each one to the head of the list
                        //
                        m_subEntries.Insert(0, new LogEntryJobHistory(sourceName, dr));
                    }
                    catch (InvalidCastException)
                    {                       
                    }

                    ++i;
                }                
            }

            /// <summary>
            /// builds an entry for a running job - in this case there is no row available since agent logs outcomes only after job finishes
            /// </summary>
            /// <param name="sourceName"></param>
            /// <param name="dr">points to last entry - which should corespond to first step - so we can compute job name and duration</param>
            private void InitializeJobHistoryForRunningJob(string sourceName, DataRow dr)
            {
                try
                {
                    m_originalSourceName = sourceName;

                    m_pointInTime = Convert.ToDateTime(dr[cUrnRunDate], System.Globalization.CultureInfo.InvariantCulture);
                    m_fieldJobName = Convert.ToString(dr[cUrnJobName], System.Globalization.CultureInfo.InvariantCulture);

                    m_severity = SeverityClass.InProgress;

                    m_fieldStepID = null;
                    m_fieldStepName = null;
                    m_fieldMessage = "DropObjectsSR.InProgressStatus"; // $FUTURE - assign its own string when string resources got un-freezed
                    m_fieldSqlSeverity = null;
                    m_fieldSqlMessageID = null;
                    m_fieldOperatorEmailed = null;
                    m_fieldOperatorNetsent = null;
                    m_fieldOperatorPaged = null;
                    m_fieldRetriesAttempted = null;
                    m_serverName = null;

                    m_fieldDuration = Convert.ToString(Convert.ToDateTime(dr[cUrnServerTime]) - Convert.ToDateTime(dr[cUrnRunDate], System.Globalization.CultureInfo.InvariantCulture), System.Globalization.CultureInfo.InvariantCulture);
                }
                catch (InvalidCastException)
                {
                    // keep null data if we hit some invalid info
                }
            }
            #endregion

            #region ILogEntry interface implementation
            string ILogEntry.OriginalSourceTypeName
            {
                get
                {
                    return "LogViewerSR.LogSourceTypeJobHistory";
                }
            }

            string ILogEntry.OriginalSourceName
            {
                get
                {
                    return m_originalSourceName;
                }
            }

            SeverityClass ILogEntry.Severity
            {
                get
                {
                    return m_severity;
                }
            }

            DateTime ILogEntry.PointInTime
            {
                get
                {
                    return m_pointInTime;
                }
            }

            string ILogEntry.this[string fieldName]
            {
                get
                {
                    // if (fieldName == LogViewerSR.Field_JobName ||
                    //     fieldName == LogViewerSR.Field_Source)
                    // {
                    //     return m_fieldJobName;
                    // }
                    // else if (fieldName == LogViewerSR.Field_StepID)
                    // {
                    //     return m_fieldStepID;
                    // }
                    // else if (fieldName == LogViewerSR.Field_StepName)
                    // {
                    //     return m_fieldStepName;
                    // }
                    // else if (fieldName == LogViewerSR.Field_Message)
                    // {
                    //     return m_fieldMessage;
                    // }
                    // else if (fieldName == LogViewerSR.Field_Duration)
                    // {
                    //     return m_fieldDuration;
                    // }
                    // else if (fieldName == LogViewerSR.Field_SqlSeverity)
                    // {
                    //     return m_fieldSqlSeverity;
                    // }
                    // else if (fieldName == LogViewerSR.Field_SqlMessageID)
                    // {
                    //     return m_fieldSqlMessageID;
                    // }
                    // else if (fieldName == LogViewerSR.Field_OperatorEmailed)
                    // {
                    //     return m_fieldOperatorEmailed;
                    // }
                    // else if (fieldName == LogViewerSR.Field_OperatorNetsent)
                    // {
                    //     return m_fieldOperatorNetsent;
                    // }
                    // else if (fieldName == LogViewerSR.Field_OperatorPaged)
                    // {
                    //     return m_fieldOperatorPaged;
                    // }
                    // else if (fieldName == LogViewerSR.Field_RetriesAttempted)
                    // {
                    //     return m_fieldRetriesAttempted;
                    // }
                    // else if (fieldName == LogViewerSR.Field_Server)
                    // {
                    //     return m_serverName;
                    // }
                    // else
                    {
                        return null;
                    }
                }
            }

            bool ILogEntry.CanLoadSubEntries
            {
                get { return ((m_subEntries != null) && (m_subEntries.Count > 0)); }
            }
        
            List<ILogEntry> ILogEntry.SubEntries
            {
                get { return m_subEntries; }
            }
            #endregion
        }
        #endregion

        #region ILogCommandTarget interface implementation

        // ILogCommandHandler ILogCommandTarget.GetCommandHandler(LogViewerCommand command)
        // {
        //     return (command == LogViewerCommand.Delete) ? m_customCommandHandler : null;
        // }

        #endregion

        /// <summary>
        /// helper method to create a data container to pass in as context to job, job step dialog
        /// </summary>
        /// <param name="parameterTemplate"></param>
        /// <returns></returns>
        private CDataContainer CreateDataContainerToLaunchJobDialog(string parameterTemplate)
        {
            CDataContainer dataContainer = new CDataContainer(CDataContainer.ServerType.SQL, m_sqlConnectionInfo, true);

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(parameterTemplate);

            dataContainer.Document = doc;
            dataContainer.SetDocumentPropertyValue("jobid", m_jobId.ToString());
            return dataContainer;
        }

        #region ITypedColumns Members
        /// <summary>
        /// handler method when hyper link was clicked on cell in job history row
        /// if job name cell is clicked - job properties dialog is launched
        /// if stepId cell is clicked - job step properties for specific job, steps ID is launched
        /// </summary>
        /// <param name="sourcename"></param>
        /// <param name="columnname"></param>
        /// <param name="hyperlink"></param>
        /// <param name="row"></param>
        /// <param name="column"></param>
        // public void HyperLinkClicked(string sourcename, string columnname, string hyperlink, long row, int column)
        // {
        //     if (LogViewerSR.Field_JobName == columnname)
        //     {
        //         this.LaunchFormHelper(DialogType.JobDialog, null);
        //     }
        //     else if (LogViewerSR.Field_StepID == columnname)
        //     {
        //         // get step ID from hyper linked step id in log history
        //         int stepId = int.Parse(hyperlink);
        //         if (int.TryParse(hyperlink, out stepId))
        //         {
        //             // stepid received is index that starts from 1, in JobSteps.Steps[] is id zero based
        //             if (stepId > 0)
        //             {
        //                 int? currentJobStepId = (int?)(stepId - 1);
        //                 this.LaunchFormHelper(DialogType.JobStepDialog, currentJobStepId);
        //             }
        //         }
        //         else
        //         {
        //             throw new InvalidOperationException("Step ID received was invalid integer");
        //         }
        //     }
        //     else
        //     {
        //         throw new NotImplementedException();
        //     }

        // }

        /// <summary>
        /// helper method to launch specified dialog, optionally accepts stepid parameter
        /// </summary>
        /// <param name="dialogType"></param>
        /// <param name="jobStepId"></param>
        // private void LaunchFormHelper(DialogType dialogType, int? jobStepId) 
        // {
        //     CDataContainer dataContainer = null;
        //     ISqlControlCollection control = null;

        //     switch (dialogType)
        //     {
        //         case DialogType.JobDialog:
        //             dataContainer = this.CreateDataContainerToLaunchJobDialog(JobDialogParameterTemplate);
        //             control = new JobPropertySheet(dataContainer);
        //             break;

        //         case DialogType.JobStepDialog:
        //             if (null == jobStepId)
        //             {
        //                 throw new ArgumentNullException("jobStepId");
        //             }

        //             dataContainer = this.CreateDataContainerToLaunchJobDialog(JobStepDialogParameterTemplate);
        //             JobData jobData = new JobData(dataContainer);
        //             control = new JobStepPropertySheet(dataContainer, (JobStepData)jobData.JobSteps.Steps[(int)jobStepId], this.serviceProvider);
        //             break;

        //         default:
        //             throw new InvalidArgumentException("dialogType");
        //     }

        //     // launch form
        //     using (LaunchForm lf = new LaunchForm(control, this.serviceProvider))
        //     {
        //         lf.ShowDialog();
        //     }
        // }

        #endregion
    }
}
