//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using System.Collections.Generic;

namespace Microsoft.SqlTools.ServiceLayer.TableDesigner.Contracts
{
    /// <summary>
    /// The information requested by the table designer UI.
    /// </summary>
    public class TableDesignerInfo
    {
        public TableDesignerView View { get; set; }

        public TableViewModel ViewModel { get; set; }

        public List<string> ColumnTypes { get; set; }

        public List<string> Schemas { get; set; }
    }
}