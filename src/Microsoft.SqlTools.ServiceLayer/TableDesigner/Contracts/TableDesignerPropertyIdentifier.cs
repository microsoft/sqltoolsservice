//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.TableDesigner.Contracts
{
    public class TableDesignerPropertyIdentifier
    {
        public string ParentProperty { get; set; }

        public int Index { get; set; }

        public string Property { get; set; }
    }
}