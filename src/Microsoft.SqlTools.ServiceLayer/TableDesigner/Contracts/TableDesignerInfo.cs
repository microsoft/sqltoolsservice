//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.TableDesigner.Contracts
{
    /// <summary>
    /// The information requested by the table designer UI.
    /// </summary>
    public class TableDesignerInfo
    {
        public TableDesignerView View { get; set; }

        public TableViewModel ViewModel { get; set; }

        public TableInfo TableInfo { get; set; }

        public TableDesignerIssue[] Issues { get; set; }
    }
}