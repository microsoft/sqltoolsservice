//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Globalization;
using Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlTools.ServiceLayer.DisasterRecovery.RestoreOperation
{
    public class UnsupportedDeviceTypeException: Exception
    {
        public UnsupportedDeviceTypeException(Edition engineEdition, DeviceType deviceType) : base(SR.UnsupportedDeviceType(deviceType.ToString(), engineEdition.ToString()))
        {
        }
    }
}
