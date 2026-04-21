//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


using System;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    /// <summary>
    /// a class for storing various credential properties
    /// </summary>
    public class CredentialInfo : SqlObject
    {
        public int Id { get; set; }
        public string Identity { get; set; } = null!;
        public DateTime DateLastModified { get; set; }
        public DateTime CreateDate { get; set; }
        public string ProviderName { get; set; } = null!;
        public string? Secret { get; set; }
    }
}