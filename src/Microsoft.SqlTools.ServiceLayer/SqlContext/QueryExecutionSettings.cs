// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.SqlContext
{
    /// <summary>
    /// Collection of settings related to the execution of queries
    /// </summary>
    public class QueryExecutionSettings : GeneralRequestDetails
    {
        #region Constants

        /// <summary>
        /// Default value for batch separator (de facto standard as per SSMS)
        /// </summary>
        private const string DefaultBatchSeparator = "GO";

        /// <summary>
        /// Default number of chars to store for long text fields (de facto standard as per SSMS)
        /// </summary>
        private const int DefaultMaxCharsToStore = 65535; // 64 KB - QE default

        /// <summary>
        /// Default number of chars to store of XML values (de facto standard as per SSMS)
        /// xml is a special case so number of chars to store is usually greater than for other long types 
        /// </summary>
        private const int DefaultMaxXmlCharsToStore = 2097152; // 2 MB - QE default

        /// <summary>
        /// Default selection of returning an actual XML showplan with all batches
        /// Do not return any execution plan by default
        /// </summary>
        private static ExecutionPlanOptions DefaultExecutionPlanOptions = new ExecutionPlanOptions
        { 
            IncludeActualExecutionPlanXml = false,
            IncludeEstimatedExecutionPlanXml = false
        };

        /// <summary>
        /// Default option for displaying a bit column as a number. (defacto standard as per SSMS)
        /// </summary>
        private const bool DefaultDisplayBitAsNumber = true;

        /// <summary>
        /// default row count
        /// </summary>
        private const int DefaultRowCount = 0;

        /// <summary>
        /// default text size
        /// </summary>
        private const int DefaultTextSize = 2147483647;

        /// <summary>
        /// default execution timeout
        /// </summary>
        private const int DefaultExecutionTimeout = 0;


        /// <summary>
        /// default no count
        /// </summary>
        private const bool DefaultNoCount = false;

        /// <summary>
        /// default no exec
        /// </summary>
        private const bool DefaultNoExec = false;

        /// <summary>
        /// default parse only
        /// </summary>
        private const bool DefaultParseOnly = false;

        /// <summary>
        /// default arith abort
        /// </summary>
        private const bool DefaultArithAbort = true;

        /// <summary>
        /// default concat null yields null
        /// </summary>
        private const bool DefaultConcatNullYieldsNull = true;

        /// <summary>
        /// default statistics time
        /// </summary>
        private const bool DefaultStatisticsTime = false;

        /// <summary>
        /// default statistics IO
        /// </summary>
        private const bool DefaultStatisticsIO = false;

        /// <summary>
        /// default transaction abort ON
        /// </summary>
        private const bool DefaultXactAbortOn = false;
    
        /// <summary>
        /// default ANSI padding
        /// </summary>
        private const bool DefaultAnsiPadding = true;

        /// <summary>
        /// default ANSI warnings
        /// </summary>
        private const bool DefaultAnsiWarnings = true;

        /// <summary>
        /// default ANSI Nulls
        /// </summary>
        private const bool DefaultAnsiNulls = true;

        /// <summary>
        /// default use ANSI defaults
        /// </summary>
        private const bool DefaultAnsiDefaults = false;

        /// <summary>
        /// default quoted identifier
        /// </summary>
        private const bool DefaultQuotedIdentifier = true;

        /// <summary>
        /// default ANSI NULL default ON
        /// </summary>
        private const bool DefaultAnsiNullDefaultOn = true;

        /// <summary>
        /// default implicit transactions
        /// </summary>
        private const bool DefaultImplicitTransactions = false;

        /// <summary>
        /// default cursor close on commit
        /// </summary>
        private const bool DefaultCursorCloseOnCommit = false;

        /// <summary>
        /// default transaction isolation level
        /// </summary>
        private const string DefaultTransactionIsolationLevel = "READ UNCOMMITTED";
        
        /// <summary>
        /// default deadlock priority
        /// </summary>
        private const string DefaultDeadlockPriority = "Normal";

        /// <summary>
        /// default lock timeout
        /// </summary>
        private const int DefaultLockTimeout = -1;
        
        /// <summary>
        /// default query governor cost limit
        /// </summary>
        private const int DefaultQueryGovernorCostLimit = 0;

        #endregion

        #region Member Variables

        private ExecutionPlanOptions? executionPlanOptions;

        #endregion

        #region Properties

        /// <summary>
        /// The configured batch separator, will use a default if a value was not configured
        /// </summary>
        public string BatchSeparator
        {
            get
            {
                return GetOptionValue<string>("batchSeparator", DefaultBatchSeparator);
            }
            set
            {
                SetOptionValue("batchSeparator", value);
            }
        }

        /// <summary>
        /// Maximum number of characters to store in temp file for long character fields and binary
        /// fields. Will use a default if a value was not configured.
        /// </summary>
        public int MaxCharsToStore
        {
            get
            {
                return GetOptionValue<int>("maxCharsToStore", DefaultMaxCharsToStore);
            }
            set
            {
                SetOptionValue("maxCharsToStore", value);
            }
        }

        /// <summary>
        /// Maximum number of characters to store in temp file for XML columns. Will use a default
        /// value if one was not configured.
        /// </summary>
        public int MaxXmlCharsToStore
        {
            get
            {
                return GetOptionValue<int>("maxXmlCharsToStore", DefaultMaxXmlCharsToStore);
            }
            set
            {
                SetOptionValue("maxXmlCharsToStore", value);
            }
        }

        /// <summary>
        /// Options for returning execution plans when executing queries
        /// </summary>
        public ExecutionPlanOptions ExecutionPlanOptions
        {
            get { return executionPlanOptions ?? DefaultExecutionPlanOptions; }
            set { executionPlanOptions = value; }
        }

        /// <summary>
        /// Determines how to generate display value for bit columns. If <c>true</c>, bit columns
        /// will be rendered as "1" or "0". If <c>false</c>, bit columns will be rendered as
        /// "true" or "false"
        /// </summary>
        public bool DisplayBitAsNumber
        {
            get
            {
                return GetOptionValue<bool>("displayBitAsNumber", DefaultDisplayBitAsNumber);
            }
            set
            {
                SetOptionValue("displayBitAsNumber", value);
            }
        }

        /// <summary>
        /// Set row count
        /// </summary>
        public int RowCount
        {
            get
            {
                return GetOptionValue<int>("rowCount", DefaultRowCount);
            }
            set
            {
                SetOptionValue("rowCount", value);
            }
        }

        /// <summary>
        /// Set text size
        /// </summary>
        public int TextSize
        {
            get
            {
                return GetOptionValue<int>("textSize", DefaultTextSize);
            }
            set
            {
                SetOptionValue("textSize", value);
            }
        }

        /// <summary>
        /// Set execution timeout
        /// </summary>
        public int ExecutionTimeout
        {
            get
            {
                return GetOptionValue<int>("executionTimeout", DefaultExecutionTimeout);
            }
            set
            {
                SetOptionValue("executionTimeout", value);
            }
        }

        /// <summary>
        /// Set no count
        /// </summary>
        public bool NoCount
        {
            get
            {
                return GetOptionValue<bool>("noCount", DefaultNoCount);
            }
            set
            {
                SetOptionValue("noCount", value);
            }
        }

        /// <summary>
        /// Set no exec
        /// </summary>
        public bool NoExec
        {
            get
            {
                return GetOptionValue<bool>("noExec", DefaultNoExec);
            }
            set
            {
                SetOptionValue("noExec", value);
            }
        }

        /// <summary>
        /// Set parse only
        /// </summary>
        public bool ParseOnly
        {
            get
            {
                return GetOptionValue<bool>("parseOnly", DefaultParseOnly);
            }
            set
            {
                SetOptionValue("parseOnly", value);
            }
        }

        /// <summary>
        /// Set arith abort
        /// </summary>
        public bool ArithAbort
        {
            get
            {
                return GetOptionValue<bool>("arithAbort", DefaultArithAbort);
            }
            set
            {
                SetOptionValue("arithAbort", value);
            }
        }

        /// <summary>
        /// Set concat null yields null
        /// </summary>
        public bool ConcatNullYieldsNull
        {
            get
            {
                return GetOptionValue<bool>("concatNullYieldsNull", DefaultConcatNullYieldsNull);
            }
            set
            {
                SetOptionValue("concatNullYieldsNull", value);
            }
        }

        /// <summary>
        /// Set statistics time
        /// </summary>
        public bool StatisticsTime
        {
            get
            {
                return GetOptionValue<bool>("statisticsTime", DefaultStatisticsTime);
            }
            set
            {
                SetOptionValue("statisticsTime", value);
            }
        }

        /// <summary>
        /// Set statistics I\O
        /// </summary>
        public bool StatisticsIO
        {
            get
            {
                return GetOptionValue<bool>("statisticsIO", DefaultStatisticsIO);
            }
            set
            {
                SetOptionValue("statisticsIO", value);
            }
        }

        /// <summary>
        /// Set transaction abort ON
        /// </summary>
        public bool XactAbortOn
        {
            get
            {
                return GetOptionValue<bool>("xactAbortOn", DefaultXactAbortOn);
            }
            set
            {
                SetOptionValue("xactAbortOn", value);
            }
        }

        /// <summary>
        /// Set transaction isolation level
        /// </summary>
        public string TransactionIsolationLevel
        {
            get
            {
                return GetOptionValue<string>("transactionIsolationLevel", DefaultTransactionIsolationLevel);
            }
            set
            {
                SetOptionValue("transactionIsolationLevel", value);
            }
        }

        /// <summary>
        /// Set deadlock priority
        /// </summary>
        public string DeadlockPriority
        {
            get
            {
                return GetOptionValue<string>("deadlockPriority", DefaultDeadlockPriority);
            }
            set
            {
                SetOptionValue("deadlockPriority", value);
            }
        }

        /// <summary>
        /// Set lock timeout
        /// </summary>
        public int LockTimeout
        {
            get
            {
                return GetOptionValue<int>("lockTimeout", DefaultLockTimeout);
            }
            set
            {
                SetOptionValue("lockTimeout", value);
            }
        }

        /// <summary>
        /// Set query governor cost limit
        /// </summary>
        public int QueryGovernorCostLimit
        {
            get
            {
                return GetOptionValue<int>("queryGovernorCostLimit", DefaultQueryGovernorCostLimit);
            }
            set
            {
                SetOptionValue("queryGovernorCostLimit", value);
            }
        }

        /// <summary>
        /// Set ANSI defaults ON
        /// </summary>
        public bool AnsiDefaults
        {
            get
            {
                return GetOptionValue<bool>("ansiDefaults", DefaultAnsiDefaults);
            }
            set
            {
                SetOptionValue("ansiDefaults", value);
            }
        }

        /// <summary>
        /// Set quoted identifier
        /// </summary>
        public bool QuotedIdentifier
        {
            get
            {
                return GetOptionValue<bool>("quotedIdentifier", DefaultQuotedIdentifier);
            }
            set
            {
                SetOptionValue("quotedIdentifier", value);
            }
        }

        /// <summary>
        /// Set ANSI null default on
        /// </summary>
        public bool AnsiNullDefaultOn
        {
            get
            {
                return GetOptionValue<bool>("ansiNullDefaultOn", DefaultAnsiNullDefaultOn);
            }
            set
            {
                SetOptionValue("ansiNullDefaultOn", value);
            }
        }

        /// <summary>
        /// Set implicit transactions
        /// </summary>
        public bool ImplicitTransactions
        {
            get
            {
                return GetOptionValue<bool>("implicitTransactions", DefaultImplicitTransactions);
            }
            set
            {
                SetOptionValue("implicitTransactions", value);
            }
        }

        /// <summary>
        /// Set cursor close on commit
        /// </summary>
        public bool CursorCloseOnCommit
        {
            get
            {
                return GetOptionValue<bool>("cursorCloseOnCommit", DefaultCursorCloseOnCommit);
            }
            set
            {
                SetOptionValue("cursorCloseOnCommit", value);
            }
        }

        /// <summary>
        /// Set ANSI padding
        /// </summary>
        public bool AnsiPadding
        {
            get
            {
                return GetOptionValue<bool>("ansiPadding", DefaultAnsiPadding);
            }
            set
            {
                SetOptionValue("ansiPadding", value);
            }
        }

        /// <summary>
        /// Set ANSI warnings
        /// </summary>
        public bool AnsiWarnings
        {
            get
            {
                return GetOptionValue<bool>("ansiWarnings", DefaultAnsiWarnings);
            }
            set
            {
                SetOptionValue("ansiWarnings", value);
            }
        }

        /// <summary>
        /// Set ANSI nulls
        /// </summary>
        public bool AnsiNulls
        {
            get
            {
                return GetOptionValue<bool>("ansiNulls", DefaultAnsiNulls);
            }
            set
            {
                SetOptionValue("ansiNulls", value);
            }
        }

        /// <summary>
        /// Setting to return the actual execution plan as XML
        /// </summary>
        public bool IncludeActualExecutionPlanXml
        {
            get
            {
                return GetOptionValue<bool>("includeActualExecutionPlanXml");
            }
            set
            {
                SetOptionValue("includeActualExecutionPlanXml", value);
            }
        }

        /// <summary>
        /// Setting to return the estimated execution plan as XML
        /// </summary>
        public bool IncludeEstimatedExecutionPlanXml
        {
            get
            {
                return GetOptionValue<bool>("includeEstimatedExecutionPlanXml");
            }
            set
            {
                SetOptionValue("includeEstimatedExecutionPlanXml", value);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Update the current settings with the new settings
        /// </summary>
        /// <param name="newSettings">The new settings</param>
        public void Update(QueryExecutionSettings newSettings)
        {
            BatchSeparator = newSettings.BatchSeparator;
            MaxCharsToStore = newSettings.MaxCharsToStore;
            MaxXmlCharsToStore = newSettings.MaxXmlCharsToStore;
            ExecutionPlanOptions = newSettings.ExecutionPlanOptions;
            DisplayBitAsNumber = newSettings.DisplayBitAsNumber;
            RowCount = newSettings.RowCount;
            TextSize = newSettings.TextSize;
            ExecutionTimeout = newSettings.ExecutionTimeout;
            NoCount = newSettings.NoCount;
            NoExec = newSettings.NoExec;
            ParseOnly = newSettings.ParseOnly;
            ArithAbort = newSettings.ArithAbort;
            StatisticsTime = newSettings.StatisticsTime;
            StatisticsIO = newSettings.StatisticsIO;
            XactAbortOn = newSettings.XactAbortOn;
            TransactionIsolationLevel = newSettings.TransactionIsolationLevel;
            DeadlockPriority = newSettings.DeadlockPriority;
            LockTimeout = newSettings.LockTimeout;
            QueryGovernorCostLimit = newSettings.QueryGovernorCostLimit;
            AnsiDefaults = newSettings.AnsiDefaults;
            QuotedIdentifier = newSettings.QuotedIdentifier;
            AnsiNullDefaultOn = newSettings.AnsiNullDefaultOn;
            ImplicitTransactions = newSettings.ImplicitTransactions;
            CursorCloseOnCommit = newSettings.CursorCloseOnCommit;
            AnsiPadding = newSettings.AnsiPadding;
            AnsiWarnings = newSettings.AnsiWarnings;
            AnsiNulls = newSettings.AnsiNulls;
        }

        #endregion
    }
}
