//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.DisasterRecovery.Contracts
{    
    public class BackupInfo
    {
        public string OwnerUri { get; set;  }

        public string BackupType { get; set; }
    }
}
