//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Microsoft.SqlTools.SqlCore.TableDesigner.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.SchemaDesigner
{
    public class SchemaDesignerReportObject
    {
        public Guid? TableId { get; set; }
        public GeneratePreviewReportResult Report { get; set; }
    }
}