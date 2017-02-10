//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

//using System;
//using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts.ExecuteRequests;
//using Microsoft.SqlTools.ServiceLayer.SqlContext;
//using Microsoft.SqlTools.ServiceLayer.Workspace;
//using Xunit;

//namespace Microsoft.SqlTools.ServiceLayer.Test.QueryExecution.Execution
//{
//    public class RequestParamTests
//    {
//        [Fact]
//        public void StringRequest()
//        {
//            // If: I attempt to get query text from execute string params
//            ExecuteRequestParamsBase queryParams = new ExecuteStringParams
//            {
//                OwnerUri = Common.OwnerUri,
//                Query = Common.StandardQuery
//            };

//            // Then: The text should match the standard query
//            Assert.Equal(Common.StandardQuery, queryParams.SqlText);
//        }

//        [Fact]
//        public void DocumentRequestFull()
//        {
//            // Setup:
//            // ... Create a workspace service with a multi-line constructed query
//            string query = string.Format("{0}{1}GO{1}{0}", Common.StandardQuery, Environment.NewLine);
//            var workspaceService = GetDefaultWorkspaceService(query);

//            // If: I attempt to get query text from execute document params (entire document)
//            ExecuteDocumentSelectionParams.WorkspaceService = workspaceService;
//            ExecuteRequestParamsBase queryParams = new ExecuteDocumentSelectionParams
//            {
//                OwnerUri = Common.OwnerUri,
//                QuerySelection = Common.WholeDocument
//            };

//            // Then: The text should match the constructed query
//            Assert.Equal(query, queryParams.SqlText);
//        }

//        [Fact]
//        public void DocumentRequestPartial()
//        {
//            // Setup:
//            // ... Create a workspace service with a multi-line constructed query
//            string query = string.Format("{0}{1}GO{1}{0}", Common.StandardQuery, Environment.NewLine);
//            var workspaceService = GetDefaultWorkspaceService(query);

//            // If: I attempt to get query text from execute document params (partial document)
//            ExecuteDocumentSelectionParams.WorkspaceService = workspaceService;
//            ExecuteRequestParamsBase queryParams = new ExecuteDocumentSelectionParams
//            {
//                OwnerUri = Common.OwnerUri,
//                QuerySelection = Common.SubsectionDocument
//            };

//            // Then: The text should be a subset of the constructed query
//            Assert.Contains(query, queryParams.SqlText);
//        }

//        private static WorkspaceService<SqlToolsSettings> GetDefaultWorkspaceService(string query)
//        {
//            WorkspaceService<SqlToolsSettings>.Instance.CurrentSettings = new SqlToolsSettings();
//            var workspaceService = Common.GetPrimedWorkspaceService(query);
//            return workspaceService;
//        }

//    }
//}
