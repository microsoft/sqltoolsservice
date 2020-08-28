//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.Kusto.ServiceLayer.QueryExecution.Contracts.ExecuteRequests
{
    /// <summary>
    /// Parameters for executing a query from a document open in the workspace
    /// </summary>
    public class ExecuteDocumentStatementParams : ExecuteRequestParamsBase
    {
        /// <summary>
        /// Line in the document for the location of the SQL statement
        /// </summary>
        public int Line { get; set; }

        /// <summary>
        /// Column in the document for the location of the SQL statement
        /// </summary>
        public int Column { get; set; }        
    }
    
    public class ExecuteDocumentStatementRequest
    {
        public static readonly 
            RequestType<ExecuteDocumentStatementParams, ExecuteRequestResult> Type = 
            RequestType<ExecuteDocumentStatementParams, ExecuteRequestResult>.Create("query/executedocumentstatement");
    }
}
