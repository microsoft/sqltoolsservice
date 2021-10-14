//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.TableDesigner.Contracts
{
    /// <summary>
    /// Table designer's view definition, there are predefined common properties.
    /// Specify the additional properties in this class.
    /// </summary>
    public class TableDesignerView
    {
        public DesignerDataPropertyInfo[] AdditionalTableProperties { get; set; }

        public DesignerDataPropertyInfo[] AdditionalTableColumnProperties { get; set; }
    }
}