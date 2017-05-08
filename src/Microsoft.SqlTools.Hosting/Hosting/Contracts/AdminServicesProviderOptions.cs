//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.Hosting.Contracts
{
    /// <summary>
    /// Defines the admin services provider options that the DMP server implements. 
    /// </summary>
    public class AdminServicesProviderOptions
    {
        public ServiceOption[] DatabaseInfoOptions { get; set; }

        public ServiceOption[] DatabaseFileInfoOptions { get; set; }

        public ServiceOption[] FileGroupInfoOptions { get; set; }
    }
}

