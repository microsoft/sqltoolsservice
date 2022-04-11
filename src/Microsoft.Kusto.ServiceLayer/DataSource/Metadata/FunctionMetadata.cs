//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.Kusto.ServiceLayer.DataSource.Metadata
{
    public class FunctionMetadata : DatabaseMetadata
    {
        public string DatabaseName { get; set; }
        
        public string Parameters { get; set; }
        
        public string Body { get; set; }
    }
}