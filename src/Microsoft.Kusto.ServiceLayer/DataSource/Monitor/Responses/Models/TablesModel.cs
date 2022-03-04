//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.Kusto.ServiceLayer.DataSource.Monitor.Responses.Models
{
    public class TablesModel
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string TimeSpanColumn { get; set; }

        public ColumnsModel[] Columns { get; set; }
    }
}