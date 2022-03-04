//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.Kusto.ServiceLayer.DataSource.Contracts
{
    public class DataSourceConnectionDetails
    {
        public string ServerName { get; set; }
        public string DatabaseName { get; set; }
        public string UserToken { get; set; }
        public string ConnectionString { get; set; }
        public string AuthenticationType { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; } 
    }
}