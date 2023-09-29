//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;

namespace Microsoft.SqlTools.SqlCore.TableDesigner.Contracts
{
    public class PublishTableChangesResponse
    {
        public TableInfo NewTableInfo;
        public TableViewModel ViewModel;
        public TableDesignerView View;
        public Dictionary<string, string> Metadata;
    }
}
