//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;

namespace Microsoft.SqlTools.ServiceLayer.Security.Contracts
{
    /// <summary>
    /// a class for storing various credential properties
    /// </summary>
    public class CredentialInfo
    {
        public int Id { get; set; }
        public string Identity { get; set; }
        public string Name { get; set; }
        public DateTime DateLastModified { get; set; }
        public DateTime CreateDate { get; set; }
        public string ProviderName { get; set; }       
    }
}
