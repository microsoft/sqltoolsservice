
//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
#nullable disable

using System.Collections.Generic;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement
{
    public class DatabaseViewInfo : SqlObjectViewInfo
    {
        public string[] LoginNames { get; set; }
        public string[] CollationNames { get; set; }
        public string[] CompatibilityLevels { get; set; }
        public string[] ContainmentTypes { get; set; }
        public string[] RecoveryModels { get; set; }

        public bool IsAzureDB { get; set; }
        public string[] AzureBackupRedundancyLevels { get; set; }
        public Dictionary<string, string[]> AzureServiceLevelObjectives { get; set; }
        public string[] AzureEditions { get; set; }
        public Dictionary<string, string[]> AzureMaxSizes { get; set; }
    }
}