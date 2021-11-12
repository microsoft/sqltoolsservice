//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;

namespace Microsoft.SqlTools.ServiceLayer.TableDesigner.Contracts
{
    /// <summary>
    /// Table designer's view definition, there are predefined common properties.
    /// Specify the additional properties in this class.
    /// </summary>
    public class TableDesignerView
    {
        public List<DesignerDataPropertyInfo> AdditionalTableProperties { get; set; } = new List<DesignerDataPropertyInfo>();

        public List<DesignerDataPropertyInfo> AdditionalTableColumnProperties { get; set; } = new List<DesignerDataPropertyInfo>();

        public List<string> ColumnsTableProperties { get; set; } = new List<string>();

        public bool CanAddColumns { get; set; }

        public bool CanRemoveColumns { get; set; }
    }
}