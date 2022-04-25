//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.Kusto.ServiceLayer.DataSource.Monitor.Responses.Models
{
    public class WorkspacesModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Region { get; set; }
        public string ResourceId { get; set; }
        public string[] TableGroups { get; set; }
        public string[] Tables { get; set; }
    }
}