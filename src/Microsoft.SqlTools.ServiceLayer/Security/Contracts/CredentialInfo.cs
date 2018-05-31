//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using Microsoft.SqlTools.ServiceLayer.Agent;

namespace Microsoft.SqlTools.ServiceLayer.Security.Contracts
{
    /// <summary>
    /// a class for storing various credential properties
    /// </summary>
    public class CredentialInfo
    {
        public string Identity { get; set; }
        public int Id { get; }
        public DateTime DateLastModified { get; }
        public DateTime CreateDate { get; }
       public string ProviderName { get; set; }       
    }
}
