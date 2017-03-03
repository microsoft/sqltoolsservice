//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Microsoft.SqlTools.Credentials.Contracts;

namespace Microsoft.SqlTools.Credentials.Linux
{
    /// <summary>
    /// Simplified class to enable writing a set of credentials to/from disk
    /// </summary>
    public class CredentialsWrapper
    {
        public List<Credential> Credentials { get; set; }
    }
}
