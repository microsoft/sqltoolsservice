//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Data;

namespace Microsoft.SqlTools.ServiceLayer.BatchParser.ExecutionEngineCode
{
    /// <summary>
    /// Class associated with setting batch results
    /// </summary>
    public class BatchResultSetEventArgs : EventArgs
    {
        
        private readonly IDataReader dataReader = null;
        private readonly ShowPlanType expectedShowPlan = ShowPlanType.None;

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="dataReader"></param>
        public BatchResultSetEventArgs(IDataReader dataReader, ShowPlanType expectedShowPlan)
        {
            this.dataReader = dataReader;
            this.expectedShowPlan = expectedShowPlan;
        }

        /// <summary>
        /// Data reader associated with the result set
        /// </summary>
        public IDataReader DataReader
        {
            get
            {
                return dataReader;
            }
        }

        /// <summary>
        /// Show Plan to be expected if any during the execution
        /// </summary>
        public ShowPlanType ExpectedShowPlan
        {
            get
            {
                return expectedShowPlan;
            }
        }
    }
}
