using System;
using Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlTools.ServiceLayer.DisasterRecovery.RestoreOperation
{
    class UnsupportedDeviceTypeException: Exception
    {
        public UnsupportedDeviceTypeException(DeviceType deviceType) : base("Unsupported device type " + deviceType.ToString() + ".")
        {
        }
    }
}
