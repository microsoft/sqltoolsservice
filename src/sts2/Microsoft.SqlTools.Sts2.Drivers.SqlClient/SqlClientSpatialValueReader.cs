//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Data.SqlTypes;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Types;
using Microsoft.SqlTools.Sts2.Abstractions;

namespace Microsoft.SqlTools.Sts2.Drivers.SqlClient
{
    /// <summary>
    /// Converts SQL Server's native CLR serialization to complete AsBinaryZM WKB.
    /// Conversion is opt-in (D-0020), bounded before allocation, and cell-local:
    /// malformed/provider-specific values become honest sentinels rather than
    /// failing the query. AsBinaryZM preserves Z and M ordinates.
    /// </summary>
    internal static class SqlClientSpatialValueReader
    {
        internal static object Read(SqlDataReader reader, int ordinal, string kind, int maxCellBytes)
        {
            long sourceBytes;
            try
            {
                sourceBytes = reader.GetBytes(ordinal, 0, null, 0, 0);
            }
            catch (Exception ex) when (IsExpectedConversionFailure(ex))
            {
                return Unavailable(kind, "unsupportedNativeValue");
            }

            if (sourceBytes <= 0 || sourceBytes > int.MaxValue)
            {
                return Unavailable(kind, "unsupportedNativeValue", sourceBytes: sourceBytes);
            }
            if (maxCellBytes > 0 && sourceBytes > maxCellBytes)
            {
                return Unavailable(kind, "maxCellBytes", sourceBytes: sourceBytes);
            }

            try
            {
                var native = new byte[checked((int)sourceBytes)];
                long offset = 0;
                while (offset < sourceBytes)
                {
                    int count = checked((int)Math.Min(32768, sourceBytes - offset));
                    long read = reader.GetBytes(ordinal, offset, native, checked((int)offset), count);
                    if (read <= 0)
                    {
                        return Unavailable(kind, "conversionFailed", sourceBytes: sourceBytes);
                    }
                    offset += read;
                }

                int srid;
                byte[] wkb;
                if (kind == "geometry")
                {
                    SqlGeometry value = SqlGeometry.Deserialize(new SqlBytes(native));
                    srid = value.STSrid.Value;
                    wkb = value.AsBinaryZM().Value;
                }
                else if (kind == "geography")
                {
                    SqlGeography value = SqlGeography.Deserialize(new SqlBytes(native));
                    srid = value.STSrid.Value;
                    wkb = value.AsBinaryZM().Value;
                }
                else
                {
                    return Unavailable(kind, "unsupportedNativeValue", sourceBytes: sourceBytes);
                }

                string? unavailableReason = WkbUnavailableReason(wkb.Length, maxCellBytes);
                if (unavailableReason is not null)
                {
                    return Unavailable(kind, unavailableReason, srid, sourceBytes);
                }
                return new DriverSpatialValue { Kind = kind, Srid = srid, Wkb = wkb };
            }
            catch (Exception ex) when (IsExpectedConversionFailure(ex))
            {
                return Unavailable(kind, "conversionFailed", sourceBytes: sourceBytes);
            }
        }

        private static bool IsExpectedConversionFailure(Exception ex) => ex is
            ArgumentException or
            FormatException or
            InvalidCastException or
            InvalidOperationException or
            NotSupportedException or
            OverflowException or
            SqlTypeException;

        internal static string? WkbUnavailableReason(int wkbLength, int maxCellBytes) =>
            wkbLength < 5
                ? "conversionFailed"
                : maxCellBytes > 0 && wkbLength > maxCellBytes
                    ? "maxCellBytes"
                    : null;

        private static DriverSpatialUnavailableValue Unavailable(
            string kind,
            string reason,
            int? srid = null,
            long? sourceBytes = null) => new()
        {
            Kind = kind,
            Reason = reason,
            Srid = srid,
            SourceBytes = sourceBytes,
        };
    }
}
