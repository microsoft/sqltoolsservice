//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

namespace Microsoft.SqlTools.ServiceLayer.Utility
{
    public static class ByteConverter
    {
        /// <summary>
        /// Converts value in KBs to MBs
        /// </summary>
        /// <param name="valueInKb">value in kilo bytes</param>
        /// <returns>Returns as double type</returns>
        public static double ConvertKbtoMb(double valueInKb)
        {
            return (Math.Round(valueInKb / 1024, 2));
        }

        /// <summary>
        /// Converts value in MBs to GBs
        /// </summary>
        /// <param name="valueInMb">value in mega bytes</param>
        /// <returns>Returns as double type</returns>
        public static double ConvertMbtoGb(double valueInMb)
        {
            return (Math.Round(valueInMb / 1024, 2));
        }
    }
}
