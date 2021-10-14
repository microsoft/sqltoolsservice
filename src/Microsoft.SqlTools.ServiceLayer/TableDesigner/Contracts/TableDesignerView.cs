//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.TableDesigner.Contracts
{
    public class TableDesignerView
    {
        public DesignerDataPropertyInfo[] AdditionalTableProperties { get; set; }

        public DesignerDataPropertyInfo[] AdditionalTableColumnProperties { get; set; }
    }
}