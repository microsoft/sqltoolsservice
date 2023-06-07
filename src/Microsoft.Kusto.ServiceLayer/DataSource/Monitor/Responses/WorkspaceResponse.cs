//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.Kusto.ServiceLayer.DataSource.Monitor.Responses.Models;

namespace Microsoft.Kusto.ServiceLayer.DataSource.Monitor.Responses
{
    public class WorkspaceResponse
    {
        public TableGroupsModel[] TableGroups { get; set; }
        public TablesModel[] Tables { get; set; }
        public WorkspacesModel[] Workspaces { get; set; }
    }
}