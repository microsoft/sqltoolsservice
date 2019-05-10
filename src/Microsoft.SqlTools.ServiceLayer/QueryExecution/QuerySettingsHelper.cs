//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.IO;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts.ExecuteRequests;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage;
using Microsoft.SqlTools.ServiceLayer.SqlContext;
using Microsoft.SqlTools.ServiceLayer.Workspace;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.Utility;
using System.Diagnostics;
using System.Xml.Serialization;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution
{
    /// <summary>
    /// Service for executing queries
    /// </summary>
    public class QuerySettingsHelper
    {
        //strings for various "SET <option> ON/OFF" statements
        private static string s_On = "ON";
        private static string s_Off = "OFF";
        private static string s_Low = "LOW";
        private static string s_Normal = "NORMAL";
        //private static string s_ReadCommited = "READ COMMITTED";
        private static string s_SetNoCount = "SET NOCOUNT {0}";
        private static string s_SetConcatenationNull = "SET CONCAT_NULL_YIELDS_NULL {0}";
        private static string s_SetNumericAbort = "SET NUMERIC_ROUNDABORT {0}";
        private static string s_SetXACTAbort = "SET XACT_ABORT {0}";
        private static string s_SetArithAbort = "SET ARITHABORT {0}";
        private static string s_SetFmtOnly = "SET FMTONLY {0}";
        private static string s_SetForceplan = "SET FORCEPLAN {0}";
        private static string s_SetRowCount = "SET ROWCOUNT {0}";
        private static string s_SetLockTimeout = "SET LOCK_TIMEOUT {0}";
        private static string s_SetTextSize = "SET TEXTSIZE {0}";
        private static string s_SetQueryGovernorCost = "SET QUERY_GOVERNOR_COST_LIMIT {0}";
        private static string s_SetDeadlockPriority = "SET DEADLOCK_PRIORITY {0}";
        private static string s_SetTranIsolationLevel = "SET TRANSACTION ISOLATION LEVEL {0}";
        private static string s_SetAnsiNulls = "SET ANSI_NULLS {0}";
        private static string s_SetAnsiNullDefault = "SET ANSI_NULL_DFLT_ON {0}";
        private static string s_SetAnsiPadding = "SET ANSI_PADDING {0}";
        private static string s_SetAnsiWarnings = "SET ANSI_WARNINGS {0}";
        private static string s_SetCursorCloseOnCommit = "SET CURSOR_CLOSE_ON_COMMIT {0}";
        private static string s_SetImplicitTransaction = "SET IMPLICIT_TRANSACTIONS {0}";
        private static string s_SetQuotedIdentifier = "SET QUOTED_IDENTIFIER {0}";

        private QueryExecutionSettings settings;

        public QuerySettingsHelper(QueryExecutionSettings settings)
        {
            this.settings = settings;
        }

        [XmlIgnore]
        public string SetNoCountString
        {
            get
            {
                return string.Format(System.Globalization.CultureInfo.InvariantCulture, s_SetNoCount, (this.settings.NoCount ? s_On : s_Off));
            }
        }

        [XmlIgnore()]
        public string SetConcatenationNullString
        {
            get
            {
                return string.Format(System.Globalization.CultureInfo.InvariantCulture, s_SetConcatenationNull, (this.settings.ConcatNullYieldsNull ? s_On : s_Off));
            }
        }

         [XmlIgnore()]
        public string SetNumericAbortString
        {
            get
            {
                return string.Format(System.Globalization.CultureInfo.InvariantCulture, s_SetNumericAbort, (this.settings.ArithAbort ? s_On : s_Off));
            }
        }

        [XmlIgnore]
        public string SetXactAbortString
        {
            get
            {
                return string.Format(System.Globalization.CultureInfo.InvariantCulture, s_SetXACTAbort, (this.settings.XactAbortOn ? s_On : s_Off));
            }
        }

        [XmlIgnore()]
        public string SetArithAbortString
        {
            get
            {
                return string.Format(System.Globalization.CultureInfo.InvariantCulture, s_SetArithAbort, (this.settings.ArithAbort ? s_On : s_Off));
            }
        }

        [XmlIgnore()]
        public string SetRowCountString
        {
            get
            {
                return string.Format(System.Globalization.CultureInfo.InvariantCulture, s_SetRowCount, this.settings.RowCount);
            }
        }

         [XmlIgnore()]
        public string SetLockTimeoutString
        {
            get
            {
                return string.Format(System.Globalization.CultureInfo.InvariantCulture, s_SetLockTimeout, this.settings.LockTimeout);
            }
        }


        [XmlIgnore()]
        public string SetTextSizeString
        {
            get
            {
                return string.Format(System.Globalization.CultureInfo.InvariantCulture, s_SetTextSize, this.settings.TextSize);
            }
        }

        [XmlIgnore()]
        public string SetQueryGovernorCostString
        {
            get
            {
                return string.Format(System.Globalization.CultureInfo.InvariantCulture, s_SetQueryGovernorCost, this.settings.QueryGovernorCostLimit);
            }
        }

        [XmlIgnore()]
        public string SetDeadlockPriorityString
        {
            get
            {
                
                bool isDeadlockPriorityLow = string.Compare(this.settings.DeadlockPriority, "low", StringComparison.OrdinalIgnoreCase) == 0;
                return string.Format(System.Globalization.CultureInfo.InvariantCulture, s_SetDeadlockPriority, (isDeadlockPriorityLow ? s_Low : s_Normal));
            }
        }


        [XmlIgnore]
        public string SetTransactionIsolationLevelString
        {
            get
            {
                return string.Format(System.Globalization.CultureInfo.InvariantCulture, s_SetTranIsolationLevel, this.settings.TransactionIsolationLevel);
            }
        }


        [XmlIgnore()]
        public string SetAnsiNullsString
        {
            get
            {
                return string.Format(System.Globalization.CultureInfo.InvariantCulture, s_SetAnsiNulls, (this.settings.AnsiNulls ? s_On : s_Off));
            }
        }

        [XmlIgnore()]
        public string SetAnsiNullDefaultString
        {
            get
            {
                return string.Format(System.Globalization.CultureInfo.InvariantCulture, s_SetAnsiNullDefault, (this.settings.AnsiNullDefaultOn ? s_On : s_Off));
            }
        }

        [XmlIgnore()]
        public string SetAnsiPaddingString
        {
            get
            {
                return string.Format(System.Globalization.CultureInfo.InvariantCulture, s_SetAnsiPadding, (this.settings.AnsiPadding ? s_On : s_Off));
            }
        }

        [XmlIgnore()]
        public string SetAnsiWarningsString
        {
            get
            {
                return string.Format(System.Globalization.CultureInfo.InvariantCulture, s_SetAnsiWarnings, (this.settings.AnsiWarnings ? s_On : s_Off));
            }
        }

        [XmlIgnore()]
        public string SetCursorCloseOnCommitString
        {
            get
            {
                return string.Format(System.Globalization.CultureInfo.InvariantCulture, s_SetCursorCloseOnCommit, (this.settings.CursorCloseOnCommit ? s_On : s_Off));
            }
        }

        [XmlIgnore()]
        public string SetImplicitTransactionString
        {
            get
            {
                return string.Format(System.Globalization.CultureInfo.InvariantCulture, s_SetImplicitTransaction, (this.settings.ImplicitTransactions ? s_On : s_Off));
            }
        }


        [XmlIgnore()]
        public string SetQuotedIdentifierString
        {
            get
            {
                return string.Format(System.Globalization.CultureInfo.InvariantCulture, s_SetQuotedIdentifier, (this.settings.QuotedIdentifier ? s_On : s_Off));
            }
        }
    }
}