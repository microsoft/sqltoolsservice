//------------------------------------------------------------------------------
// <copyright file="BatchResultSetEventArgs.cs" company="Microsoft">
//	 Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Data;

namespace Microsoft.SqlTools.ServiceLayer.BatchParser.ExecutionEngineCode
{
    internal class BatchResultSetEventArgs : EventArgs
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="dataReader"></param>
        internal BatchResultSetEventArgs(IDataReader dataReader, ShowPlanType expectedShowPlan)
        {
            _dataReader = dataReader;
            _expectedShowPlan = expectedShowPlan;
        }

        /// <summary>
        /// Data reader associated with the result set
        /// </summary>
        public IDataReader DataReader
        {
            get
            {
                return _dataReader;
            }
        }

        /// <summary>
        /// Show Plan to be expected if any during the execution
        /// </summary>
        public ShowPlanType ExpectedShowPlan
        {
            get
            {
                return _expectedShowPlan;
            }
        }

        #region Private fields
        private readonly IDataReader _dataReader = null;
        private readonly ShowPlanType _expectedShowPlan = ShowPlanType.None;
        #endregion
    }
}
