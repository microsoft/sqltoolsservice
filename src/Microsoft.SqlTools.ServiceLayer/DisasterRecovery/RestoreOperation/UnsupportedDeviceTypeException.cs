using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
