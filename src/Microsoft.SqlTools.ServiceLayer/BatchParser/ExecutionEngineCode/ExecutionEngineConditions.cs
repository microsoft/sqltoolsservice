//------------------------------------------------------------------------------
// <copyright file="ExecutionEngineConditions.cs" company="Microsoft">
//	 Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections.Specialized;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.BatchParser.ExecutionEngineCode
{
    internal class ExecutionEngineConditions
    {
        #region Constructors / Destructor

        /// <summary>
        /// Default constructor
        /// </summary>
        public ExecutionEngineConditions()
        {
            // nothing
        }

        /// <summary>
        /// Overloaded constructor taking another ExecutionEngineCondition object as a reference
        /// </summary>
        public ExecutionEngineConditions(ExecutionEngineConditions condition)
        {
            _state = condition._state;
            _batchSeparator = condition._batchSeparator;
        }
        #endregion

        #region Statement strings

        public static string ShowPlanXmlStatement(bool isOn)
        {
            return string.Format(System.Globalization.CultureInfo.InvariantCulture, Consts.ShowPlanXml, (isOn ? Consts.On : Consts.Off));
        }

        public static string ShowPlanAllStatement(bool isOn)
        {
            return string.Format(System.Globalization.CultureInfo.InvariantCulture, Consts.ShowPlanAll, (isOn ? Consts.On : Consts.Off));
        }

        public static string ShowPlanTextStatement(bool isOn)
        {
            return string.Format(System.Globalization.CultureInfo.InvariantCulture, Consts.ShowPlanText, (isOn ? Consts.On : Consts.Off));
        }

        public static string StatisticsXmlStatement(bool isOn)
        {
            return string.Format(System.Globalization.CultureInfo.InvariantCulture, Consts.StatisticsXml, (isOn ? Consts.On : Consts.Off));
        }

        public static string StatisticsProfileStatement(bool isOn)
        {
            return string.Format(System.Globalization.CultureInfo.InvariantCulture, Consts.StatisticsProfile, (isOn ? Consts.On : Consts.Off));
        }

        public static string ParseOnlyStatement(bool isOn)
        {
            return string.Format(System.Globalization.CultureInfo.InvariantCulture, Consts.ParseOnly, (isOn ? Consts.On : Consts.Off));
        }

        public static string NoExecStatement(bool isOn)
        {
            return string.Format(System.Globalization.CultureInfo.InvariantCulture, Consts.NoExec, (isOn ? Consts.On : Consts.Off));
        }

        public static string StatisticsIOStatement(bool isOn)
        {
            return string.Format(System.Globalization.CultureInfo.InvariantCulture, Consts.StatisticsIO, (isOn ? Consts.On : Consts.Off));
        }

        public static string StatisticsTimeStatement(bool isOn)
        {
            return string.Format(System.Globalization.CultureInfo.InvariantCulture, Consts.StatisticsTime, (isOn ? Consts.On : Consts.Off));
        }

        public static string BeginTransactionStatement
        {
            get { return Consts.BeginTrans; }
        }

        public static string CommitTransactionStatement
        {
            get { return Consts.CommitTrans; }
        }

        public static string RollbackTransactionStatement
        {
            get { return Consts.Rollback; }
        }

        public static string BatchSeparatorStatement
        {
            get { return Consts.BatchSeparator; }
        }

        public static string ResetStatement
        {
            get { return Consts.Reset; }
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Checks the syntax of each Transact-SQL statement and returns any error messages 
        /// without compiling or executing the statement.
        /// </summary>
        public bool IsParseOnly
        {
            get { return _state[stateParseOnly]; }
            set { _state[stateParseOnly] = value; }
        }

        /// <summary>
        /// Script is wrapped withint BEGIN TRAN/COMMIT-ROLLBACK
        /// </summary>
        public bool IsTransactionWrapped
        {
            get { return _state[stateTransactionWrapped]; }
            set { _state[stateTransactionWrapped] = value; }
        }

        /// <summary>
        /// Gets or a set a value indicating whether script execution is tracked
        /// </summary>
        public bool IsScriptExecutionTracked
        {
            get { return _state[stateScriptExecutionTracked]; }
            set { _state[stateScriptExecutionTracked] = value; }
        }

        /// <summary>
        /// Halts the execution if an error is found
        /// </summary>
        public bool IsHaltOnError
        {
            get { return _state[stateHaltOnError]; }
            set { _state[stateHaltOnError] = value; }
        }

        /// <summary>
        /// Use estimated show plan
        /// </summary>
        public bool IsEstimatedShowPlan
        {
            get { return _state[stateEstimatedShowPlan]; }
            set { _state[stateEstimatedShowPlan] = value; }
        }

        /// <summary>
        /// Use actual show plan
        /// </summary>
        public bool IsActualShowPlan
        {
            get { return _state[stateActualShowPlan]; }
            set { _state[stateActualShowPlan] = value; }
        }

        /// <summary>
        /// Use Source information on messages shown to the user
        /// </summary>
        public bool IsSuppressProviderMessageHeaders
        {
            get { return _state[stateSuppressProviderMessageHeaders]; }
            set { _state[stateSuppressProviderMessageHeaders] = value; }
        }

        /// <summary>
        /// SET NO EXEC {on/off}
        /// </summary>
        public bool IsNoExec
        {
            get { return _state[stateNoExec]; }
            set { _state[stateNoExec] = value; }
        }

        /// <summary>
        /// SET STATISTICS IO {on/off}
        /// </summary>
        public bool IsStatisticsIO
        {
            get { return _state[stateStatisticsIO]; }
            set { _state[stateStatisticsIO] = value; }
        }

        /// <summary>
        /// SET SHOWPLAN_TEXT {on/off}
        /// </summary>
        public bool IsShowPlanText
        {
            get { return _state[stateShowPlanText]; }
            set { _state[stateShowPlanText] = value; }
        }

        /// <summary>
        /// SET STATISTICS IO {on/off}
        /// </summary>
        public bool IsStatisticsTime
        {
            get { return _state[stateStatisticsTime]; }
            set { _state[stateStatisticsTime] = value; }
        }

        /// <summary>
        /// SqlCmd support
        /// </summary>
        public bool IsSqlCmd
        {
            get { return _state[stateSqlCmd]; }
            set { _state[stateSqlCmd] = value; }
        }

        /// <summary>
        /// Batch separator statement
        /// </summary>
        public String BatchSeparator
        {
            get
            {
                return _batchSeparator;
            }
            set
            {
                Validate.IsNotNullOrEmptyString(nameof(value), value);
                _batchSeparator = value;
            }
        }

        #endregion

        #region Private fields
        private static class Consts
        {
            public static string On = "ON";
            public static string Off = "OFF";
            public static string ParseOnly = "SET PARSEONLY {0}";
            public static string NoExec = "SET NOEXEC {0}";
            public static string StatisticsIO = "SET STATISTICS IO {0}";
            public static string StatisticsTime = "SET STATISTICS TIME {0}";
            public static string ShowPlanXml = "SET SHOWPLAN_XML {0}";
            public static string ShowPlanAll = "SET SHOWPLAN_ALL {0}";
            public static string ShowPlanText = "SET SHOWPLAN_TEXT {0}";
            public static string StatisticsXml = "SET STATISTICS XML {0}";
            public static string StatisticsProfile = "SET STATISTICS PROFILE {0}";
            public static string BeginTrans = "BEGIN TRAN";
            public static string CommitTrans = "COMMIT TRAN";
            public static string Rollback = "ROLLBACK";
            public static string BatchSeparator = "GO";

            public static string Reset = "SET NOEXEC, FMTONLY OFF, PARSEONLY, SET SHOWPLAN_ALL, SET SHOWPLAN_TEXT";
        }

        private static readonly int stateParseOnly = BitVector32.CreateMask();
        private static readonly int stateTransactionWrapped = BitVector32.CreateMask(stateParseOnly);
        private static readonly int stateHaltOnError = BitVector32.CreateMask(stateTransactionWrapped);
        private static readonly int stateEstimatedShowPlan = BitVector32.CreateMask(stateHaltOnError);
        private static readonly int stateActualShowPlan = BitVector32.CreateMask(stateEstimatedShowPlan);
        private static readonly int stateSuppressProviderMessageHeaders = BitVector32.CreateMask(stateActualShowPlan);
        private static readonly int stateNoExec = BitVector32.CreateMask(stateSuppressProviderMessageHeaders);
        private static readonly int stateStatisticsIO = BitVector32.CreateMask(stateNoExec);
        private static readonly int stateShowPlanText = BitVector32.CreateMask(stateStatisticsIO);
        private static readonly int stateStatisticsTime = BitVector32.CreateMask(stateShowPlanText);
        private static readonly int stateSqlCmd = BitVector32.CreateMask(stateStatisticsTime);
        private static readonly int stateScriptExecutionTracked = BitVector32.CreateMask(stateSqlCmd);
                
        private BitVector32 _state = new BitVector32();
        private string _batchSeparator = Consts.BatchSeparator;

        #endregion
    }
}
