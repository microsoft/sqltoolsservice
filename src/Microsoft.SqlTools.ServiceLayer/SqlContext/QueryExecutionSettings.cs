// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.SqlContext
{
    /// <summary>
    /// Collection of settings related to the execution of queries
    /// </summary>
    public class QueryExecutionSettings
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
        private static readonly ExecutionPlanOptions DefaultExecutionPlanOptions = new ExecutionPlanOptions
        { 
            IncludeActualExecutionPlanXml = false,
            IncludeEstimatedExecutionPlanXml = false
        };

        /// <summary>
        /// Default option for displaying a bit column as a number. (defacto standard as per SSMS)
        /// </summary>
        private const bool DefaultDisplayBitAsNumber = true;

        /// <summary>
        /// .
        /// </summary>
        private const int DefaultRowCount = 0;

        /// <summary>
        /// .
        /// </summary>
        private const int DefaultTextSize = 2147483647;

        /// <summary>
        /// .
        /// </summary>
        private const int DefaultExecutionTimeout = 0;


        /// <summary>
        /// .
        /// </summary>
        private const bool DefaultNoCount = false;

        /// <summary>
        /// .
        /// </summary>
        private const bool DefaultNoExec = false;

        /// <summary>
        /// .
        /// </summary>
        private const bool DefaultParseOnly = false;

        /// <summary>
        /// .
        /// </summary>
        private const bool DefaultArithAbort = false;

        /// <summary>
        /// .
        /// </summary>
        private const bool DefaultConcatNullYieldsNull = true;

        /// <summary>
        /// .
        /// </summary>
        private const bool DefaultStatisticsTime = false;

        /// <summary>
        /// .
        /// </summary>
        private const bool DefaultStatisticsIO = false;

        /// <summary>
        /// .
        /// </summary>
        private const bool DefaultXactAbortOn = false;
    
        /// <summary>
        /// .
        /// </summary>
        private const bool DefaultAnsiPadding = true;

        /// <summary>
        /// .
        /// </summary>
        private const bool DefaultAnsiWarnings = true;

        /// <summary>
        /// .
        /// </summary>
        private const bool DefaultAnsiNulls = true;

        /// <summary>
        /// .
        /// </summary>
        private const bool DefaultAnsiDefaults = false;

        /// <summary>
        /// .
        /// </summary>
        private const bool DefaultQuotedIdentifier = true;

        /// <summary>
        /// .
        /// </summary>
        private const bool DefaultAnsiNullDefaultOn = true;

        /// <summary>
        /// .
        /// </summary>
        private const bool DefaultImplicitTransactions = false;

        /// <summary>
        /// .
        /// </summary>
        private const bool DefaultCursorCloseOnCommit = false;

        /// <summary>
        /// .
        /// </summary>
        private const string DefaultTransactionIsolationLevel = "READ UNCOMMITTED";
        
        /// <summary>
        /// .
        /// </summary>
        private const string DefaultDeadlockPriority = "Normal";

        /// <summary>
        /// .
        /// </summary>
        private const int DefaultLockTimeout = 0;
        
        /// <summary>
        /// .
        /// </summary>
        private const int DefaultQueryGovernorCostLimit = 0;

        #endregion

        #region Member Variables

        private string batchSeparator;

        private int? maxCharsToStore;

        private int? maxXmlCharsToStore;

        private ExecutionPlanOptions? executionPlanOptions;

        private bool? displayBitAsNumber;

        private int? rowCount;

        private int? textSize;

        private int? executionTimeout;

        private bool? noCount;

        private bool? noExec;

        private bool? parseOnly;

        private bool? arithAbort;

        private bool? concatNullYieldsNull;

        private bool? showplanText;

        private bool? statisticsTime;

        private bool? statisticsIO;

        private bool? xactAbortOn;

        private string transactionIsolationLevel;

        private string deadlockPriority;

        private int? lockTimeout;

        private int? queryGovernorCostLimit;

        private bool? ansiDefaults;

        private bool? quotedIdentifier;

        private bool? ansiNullDefaultOn;

        private bool? implicitTransactions;

        private bool? cursorCloseOnCommit;

        private bool? ansiPadding;

        private bool? ansiWarnings;

        private bool? ansiNulls;


        #endregion

        #region Properties

        /// <summary>
        /// The configured batch separator, will use a default if a value was not configured
        /// </summary>
        public string BatchSeparator
        {
            get { return batchSeparator ?? DefaultBatchSeparator; }
            set { batchSeparator = value; }
        }

        /// <summary>
        /// Maximum number of characters to store in temp file for long character fields and binary
        /// fields. Will use a default if a value was not configured.
        /// </summary>
        public int MaxCharsToStore
        {
            get { return maxCharsToStore ?? DefaultMaxCharsToStore; }
            set { maxCharsToStore = value; }
        }

        /// <summary>
        /// Maximum number of characters to store in temp file for XML columns. Will use a default
        /// value if one was not configured.
        /// </summary>
        public int MaxXmlCharsToStore
        {
            get { return maxXmlCharsToStore ?? DefaultMaxXmlCharsToStore; }
            set { maxXmlCharsToStore = value; }
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
            get { return displayBitAsNumber ?? DefaultDisplayBitAsNumber; }
            set { displayBitAsNumber = value; }
        }

        /// <summary>
        /// .
        /// </summary>
        public int RowCount
        {
            get { return rowCount ?? DefaultRowCount; }
            set { rowCount = value; }
        }

        /// <summary>
        /// .
        /// </summary>
        public int TextSize
        {
            get { return textSize ?? DefaultMaxCharsToStore; }
            set { textSize = value; }
        }

        /// <summary>
        /// .
        /// </summary>
        public int ExecutionTimeout
        {
            get { return executionTimeout ?? DefaultExecutionTimeout; }
            set { executionTimeout = value; }
        }

        /// <summary>
        /// .
        /// </summary>
        public bool NoCount
        {
            get { return noCount ?? DefaultNoCount; }
            set { noCount = value; }
        }

        /// <summary>
        /// .
        /// </summary>
        public bool NoExec
        {
            get { return noExec ?? DefaultNoExec; }
            set { noExec = value; }
        }

        /// <summary>
        /// .
        /// </summary>
        public bool ParseOnly
        {
            get { return parseOnly ?? DefaultParseOnly; }
            set { parseOnly = value; }
        }

        /// <summary>
        /// .
        /// </summary>
        public bool ArithAbort
        {
            get { return arithAbort ?? DefaultArithAbort; }
            set { arithAbort = value; }
        }

        /// <summary>
        /// .
        /// </summary>
        public bool ConcatNullYieldsNull
        {
            get { return concatNullYieldsNull ?? DefaultConcatNullYieldsNull; }
            set { concatNullYieldsNull = value; }
        }

        /// <summary>
        /// .
        /// </summary>
        public bool ShowplanText
        {
            get { return showplanText ?? DefaultNoCount; }
            set { showplanText = value; }
        }

        /// <summary>
        /// .
        /// </summary>
        public bool StatisticsTime
        {
            get { return statisticsTime ?? DefaultStatisticsTime; }
            set { statisticsTime = value; }
        }

        /// <summary>
        /// .
        /// </summary>
        public bool StatisticsIO
        {
            get { return statisticsIO ?? DefaultStatisticsIO; }
            set { statisticsIO = value; }
        }

        /// <summary>
        /// .
        /// </summary>
        public bool XactAbortOn
        {
            get { return xactAbortOn ?? DefaultXactAbortOn; }
            set { xactAbortOn = value; }
        }

        /// <summary>
        /// .
        /// </summary>
        public string TransactionIsolationLevel
        {
            get { return transactionIsolationLevel ?? DefaultTransactionIsolationLevel; }
            set { transactionIsolationLevel = value; }
        }

        /// <summary>
        /// .
        /// </summary>
        public string DeadlockPriority
        {
            get { return deadlockPriority ?? DefaultDeadlockPriority; }
            set { deadlockPriority = value; }
        }

        /// <summary>
        /// .
        /// </summary>
        public int LockTimeout
        {
            get { return lockTimeout ?? DefaultLockTimeout; }
            set { lockTimeout = value; }
        }

        /// <summary>
        /// .
        /// </summary>
        public int QueryGovernorCostLimit
        {
            get { return queryGovernorCostLimit ?? DefaultQueryGovernorCostLimit; }
            set { queryGovernorCostLimit = value; }
        }

        /// <summary>
        /// .
        /// </summary>
        public bool AnsiDefaults
        {
            get { return ansiDefaults ?? DefaultAnsiDefaults; }
            set { ansiDefaults = value; }
        }

        /// <summary>
        /// .
        /// </summary>
        public bool QuotedIdentifier
        {
            get { return quotedIdentifier ?? DefaultQuotedIdentifier; }
            set { quotedIdentifier = value; }
        }

        /// <summary>
        /// .
        /// </summary>
        public bool AnsiNullDefaultOn
        {
            get { return ansiNullDefaultOn ?? DefaultAnsiNullDefaultOn; }
            set { ansiNullDefaultOn = value; }
        }

        /// <summary>
        /// .
        /// </summary>
        public bool ImplicitTransactions
        {
            get { return implicitTransactions ?? DefaultImplicitTransactions; }
            set { implicitTransactions = value; }
        }

        /// <summary>
        /// .
        /// </summary>
        public bool CursorCloseOnCommit
        {
            get { return cursorCloseOnCommit ?? DefaultCursorCloseOnCommit; }
            set { cursorCloseOnCommit = value; }
        }

        /// <summary>
        /// .
        /// </summary>
        public bool AnsiPadding
        {
            get { return ansiPadding ?? DefaultAnsiPadding; }
            set { ansiPadding = value; }
        }

        /// <summary>
        /// .
        /// </summary>
        public bool AnsiWarnings
        {
            get { return ansiWarnings ?? DefaultAnsiWarnings; }
            set { ansiWarnings = value; }
        }

        /// <summary>
        /// .
        /// </summary>
        public bool AnsiNulls
        {
            get { return ansiNulls ?? DefaultAnsiNulls; }
            set { ansiNulls = value; }
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
        }

        #endregion
    }
}
