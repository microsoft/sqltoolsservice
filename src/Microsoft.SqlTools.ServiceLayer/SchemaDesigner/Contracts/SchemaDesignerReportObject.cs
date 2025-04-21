//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;

namespace Microsoft.SqlTools.ServiceLayer.SchemaDesigner
{
    public class SchemaDesignerChangeReport
    {
        public Guid? TableId { get; set; }
        public string TableName { get; set; } = string.Empty;
        public SchemaDesignerReportTableState TableState { get; set; } = SchemaDesignerReportTableState.CREATED;
        public List<string> ActionsPerformed { get; set; } = new List<string>();
    }

    public enum SchemaDesignerReportTableState
    {
        CREATED = 0,
        UPDATED = 1,
        DROPPED = 2,
    }
}