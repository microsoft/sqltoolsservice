//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System;
using System.ComponentModel;
using System.Linq;

namespace Microsoft.SqlServer.Management.QueryStoreModel.Common
{
    /// <summary>
    /// This is the list of replicas that are supported in query store UI. We call them ReplicaGroups.
    /// </summary>
    [TypeConverter(typeof(ReplicaValueConverter))]
    public enum ReplicaGroup
    {
        [LocalizedString("ReplicaOptionPrimary")]
        Primary = 1,

        [LocalizedString("ReplicaOptionSecondary")]
        Secondary = 2,

        [LocalizedString("ReplicaOptionGeoSecondary")]
        GeoSecondary = 3,

        [LocalizedString("ReplicaOptionGeoHASecondary")]
        GeoHASecondary = 4,
    }

    public static class ReplicaUtils
    {

        /// <summary>
        /// Gets the LocalizedString value of this Replica
        /// </summary>
        /// <param name="enumValue"></param>
        /// <returns></returns>
        public static string LocalizedString(ReplicaGroup enumValue)
        {
            LocalizedStringAttribute attribute = enumValue.GetType()
                .GetMember(enumValue.ToString()).Single()
                .GetCustomAttributes(typeof(LocalizedStringAttribute), false)
                .FirstOrDefault() as LocalizedStringAttribute;

            if (attribute != null)
            {
                return Resources.ResourceManager.GetString(attribute.Value);
            }

            // this indicates a code level error
            System.Diagnostics.Debug.Fail($"Unknown Replica Type {enumValue}");
            return string.Empty;
        }
    }

    /// <summary>
    /// Provide a mapping between the enum selection for configuration and human-readable text
    /// </summary>
    public class ReplicaValueConverter : EnumStringConverter<ReplicaGroup>
    {
        protected override string EnumToString(ReplicaGroup enumValue) => ReplicaUtils.LocalizedString(enumValue);
    }

    /// <summary>
    /// This is the list of replicas that are supported in query store UI. We call them ReplicaGroups.
    /// </summary>
    public class ReplicaGroupItem 
    {
        // Using Public and private accessors to allow for comboBox to read the replica values.
        // With the public only the replica names do not populate.
        private readonly string _replicaName;
        public string ReplicaName => _replicaName;

        private readonly long _replicaGroupId;
        public long ReplicaGroupId => _replicaGroupId;
        public ReplicaGroupItem(long replicaGroupId, string replicaName)
        {
            if (string.IsNullOrEmpty(replicaName))
            {
                throw new ArgumentException(Resources.ReplicaNameNotNullEmpty, nameof(replicaName));
            }
            if (replicaGroupId < 0)
            {
                throw new ArgumentException(Resources.ReplicaIdPositive, nameof(replicaGroupId));
            }
            _replicaGroupId = replicaGroupId;
            _replicaName = replicaName;
        }
    }

    /// <summary>
    /// Converts an enumeration value to its corresponding long integer representation
    /// </summary>
    public static class EnumExtensions
    {
        public static long ToLong(this Enum value) => Convert.ToInt64(value);
    }
}
