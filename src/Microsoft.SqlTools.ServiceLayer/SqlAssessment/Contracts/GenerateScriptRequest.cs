//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.SqlAssessment.Contracts
{
    /// <summary>
    /// Parameters for executing a query from a provided string
    /// </summary>
    public class GenerateScriptParams
    {
        /// <summary>
        /// Gets or sets a list of assessment result items
        /// to be written to a table
        /// </summary>
        public List<AssessmentResultItem> Items { get; set; }

        public TaskExecutionMode TaskExecutionMode { get; set; }

        public string TargetServerName { get; set; }

        public string TargetDatabaseName { get; set; }
    }

    public class GenerateScriptResult
    {
        /// <summary>
        /// Gets or sets a value indicating
        /// if assessment operation was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets an status message for the operation
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets script text
        /// </summary>
        public string Script { get; set; }
    }

    public class GenerateScriptRequest
    {
        public static readonly
            RequestType<GenerateScriptParams, ResultStatus> Type =
                RequestType<GenerateScriptParams, ResultStatus>.Create("assessment/generateScript");
    }
}
