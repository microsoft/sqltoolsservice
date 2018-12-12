//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Specialized;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.BatchParser.ExecutionEngineCode
{
    internal class ExecutionEngineConditions
    {

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
                
        private BitVector32 state = new BitVector32();
        private string batchSeparator = Consts.BatchSeparator;

        #endregion
        
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
            state = condition.state;
            batchSeparator = condition.batchSeparator;
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
            get { return state[stateParseOnly]; }
            set { state[stateParseOnly] = value; }
        }

        /// <summary>
        /// Script is wrapped withint BEGIN TRAN/COMMIT-ROLLBACK
        /// </summary>
        public bool IsTransactionWrapped
        {
            get { return state[stateTransactionWrapped]; }
            set { state[stateTransactionWrapped] = value; }
        }

        /// <summary>
        /// Gets or a set a value indicating whether script execution is tracked
        /// </summary>
        public bool IsScriptExecutionTracked
        {
            get { return state[stateScriptExecutionTracked]; }
            set { state[stateScriptExecutionTracked] = value; }
        }

        /// <summary>
        /// Halts the execution if an error is found
        /// </summary>
        public bool IsHaltOnError
        {
            get { return state[stateHaltOnError]; }
            set { state[stateHaltOnError] = value; }
        }

        /// <summary>
        /// Use estimated show plan
        /// </summary>
        public bool IsEstimatedShowPlan
        {
            get { return state[stateEstimatedShowPlan]; }
            set { state[stateEstimatedShowPlan] = value; }
        }

        /// <summary>
        /// Use actual show plan
        /// </summary>
        public bool IsActualShowPlan
        {
            get { return state[stateActualShowPlan]; }
            set { state[stateActualShowPlan] = value; }
        }

        /// <summary>
        /// Use Source information on messages shown to the user
        /// </summary>
        public bool IsSuppressProviderMessageHeaders
        {
            get { return state[stateSuppressProviderMessageHeaders]; }
            set { state[stateSuppressProviderMessageHeaders] = value; }
        }

        /// <summary>
        /// SET NO EXEC {on/off}
        /// </summary>
        public bool IsNoExec
        {
            get { return state[stateNoExec]; }
            set { state[stateNoExec] = value; }
        }

        /// <summary>
        /// SET STATISTICS IO {on/off}
        /// </summary>
        public bool IsStatisticsIO
        {
            get { return state[stateStatisticsIO]; }
            set { state[stateStatisticsIO] = value; }
        }

        /// <summary>
        /// SET SHOWPLAN_TEXT {on/off}
        /// </summary>
        public bool IsShowPlanText
        {
            get { return state[stateShowPlanText]; }
            set { state[stateShowPlanText] = value; }
        }

        /// <summary>
        /// SET STATISTICS IO {on/off}
        /// </summary>
        public bool IsStatisticsTime
        {
            get { return state[stateStatisticsTime]; }
            set { state[stateStatisticsTime] = value; }
        }

        /// <summary>
        /// SqlCmd support
        /// </summary>
        public bool IsSqlCmd
        {
            get { return state[stateSqlCmd]; }
            set { state[stateSqlCmd] = value; }
        }

        /// <summary>
        /// Batch separator statement
        /// </summary>
        public string BatchSeparator
        {
            get
            {
                return batchSeparator;
            }
            set
            {
                Validate.IsNotNullOrEmptyString(nameof(value), value);
                batchSeparator = value;
            }
        }

        #endregion
    }
}
