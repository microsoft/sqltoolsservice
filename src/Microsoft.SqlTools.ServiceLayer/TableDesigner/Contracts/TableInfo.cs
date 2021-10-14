//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.TableDesigner.Contracts
{
    public class TableInfo
    {
        public string Server { get; set; }

        public string Database { get; set; }

        public string Schema { get; set; }

        public string Name { get; set; }

        public bool IsNewTable { get; set; }

        public string ConnectionUri { get; set; }
    }
}