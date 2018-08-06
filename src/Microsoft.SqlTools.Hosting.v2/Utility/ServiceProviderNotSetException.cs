//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

namespace Microsoft.SqlTools.Hosting.Utility
{
    public class ServiceProviderNotSetException : InvalidOperationException {

        public ServiceProviderNotSetException()
            : base(SR.ServiceProviderNotSet) {
        }
    }
}