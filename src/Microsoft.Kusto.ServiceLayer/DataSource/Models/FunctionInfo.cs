//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.Kusto.ServiceLayer.DataSource.Models
{
    public class FunctionInfo
    {
        public string Name { get; set; }
        public string Parameters { get; set; }
        public string Body { get; set; }
        public string Folder { get; set; }
        public string DocString { get; set; }
    }
}