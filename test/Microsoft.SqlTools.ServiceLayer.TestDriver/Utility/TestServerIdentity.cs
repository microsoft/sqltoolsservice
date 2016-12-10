//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.TestDriver.Utility
{
    /// <summary>
    /// The model to deserialize the server names json
    /// </summary>
    public class TestServerIdentity
    {
        public string ServerName { get; set; }
        public string ProfileName { get; set; }

        public TestServerType ServerType { get; set; }
    }

    public enum TestServerType
    {
        None,
        Azure,
        OnPrem
    }
}
