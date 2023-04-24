//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    /// <summary>
    /// a class for storing various login properties
    /// </summary>
    public class DatabaseInfo : SqlObject
    {
        public string? Owner { get; set; }
        public string? CollationName { get; set; }
        public string? RecoveryModel { get; set; }
        public string? CompatibilityLevel { get; set; }
        public string? ContainmentType { get; set; }
        public bool IsAzure { get; set; }
        public bool IsDW { get; set; }
    }
}