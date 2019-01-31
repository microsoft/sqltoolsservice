//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection
{
    /// <summary>
    /// This class represents connection (and other) settings specified by called of the DacFx API.  DacFx
    /// cannot rely on the registry to supply override values therefore setting overrides must be made
    /// by the top-of-the-stack
    /// </summary>
    public sealed class AmbientSettings
    {
        private const string LogicalContextName = "__LocalContextConfigurationName";

        public enum StreamBackingStore
        {
            // MemoryStream
            Memory = 0,

            // FileStream
            File = 1
        }

        // Internal for test purposes
        internal const string MasterReferenceFilePathIndex = "MasterReferenceFilePath";
        internal const string DatabaseLockTimeoutIndex = "DatabaseLockTimeout";
        internal const string QueryTimeoutIndex = "QueryTimeout";
        internal const string LongRunningQueryTimeoutIndex = "LongRunningQueryTimeout";
        internal const string AlwaysRetryOnTransientFailureIndex = "AlwaysRetryOnTransientFailure";
        internal const string MaxDataReaderDegreeOfParallelismIndex = "MaxDataReaderDegreeOfParallelism";
        internal const string ConnectionRetryHandlerIndex = "ConnectionRetryHandler";
        internal const string TraceRowCountFailureIndex = "TraceRowCountFailure";
        internal const string TableProgressUpdateIntervalIndex = "TableProgressUpdateInterval";
        internal const string UseOfflineDataReaderIndex = "UseOfflineDataReader";
        internal const string StreamBackingStoreForOfflineDataReadingIndex = "StreamBackingStoreForOfflineDataReading";
        internal const string DisableIndexesForDataPhaseIndex = "DisableIndexesForDataPhase";
        internal const string ReliableDdlEnabledIndex = "ReliableDdlEnabled";
        internal const string ImportModelDatabaseIndex = "ImportModelDatabase";
        internal const string SupportAlwaysEncryptedIndex = "SupportAlwaysEncrypted";
        internal const string SkipObjectTypeBlockingIndex = "SkipObjectTypeBlocking";
        internal const string DoNotSerializeQueryStoreSettingsIndex = "DoNotSerializeQueryStoreSettings";
        internal const string AlwaysEncryptedWizardMigrationIndex = "AlwaysEncryptedWizardMigration";

        public static AmbientData _defaultSettings;

        static AmbientSettings()
        {
            _defaultSettings = new AmbientData();
        }       

        /// <summary>
        /// Access to the default ambient settings.  Access to these settings is made available
        /// for SSDT scenarios where settings are read from the registry and not set explicitly through
        /// the API
        /// </summary>
        public static AmbientData DefaultSettings
        {
            get { return _defaultSettings; }
        }

        public static string MasterReferenceFilePath
        {
            get { return GetValue<string>(MasterReferenceFilePathIndex); }
        }

        public static int LockTimeoutMilliSeconds
        {
            get { return GetValue<int>(DatabaseLockTimeoutIndex); }
        }

        public static int QueryTimeoutSeconds
        {
            get { return GetValue<int>(QueryTimeoutIndex); }
        }

        public static int LongRunningQueryTimeoutSeconds
        {
            get { return GetValue<int>(LongRunningQueryTimeoutIndex); }
        }

        public static Action<SqlServerRetryError> ConnectionRetryMessageHandler
        {
            get { return GetValue<Action<SqlServerRetryError>>(ConnectionRetryHandlerIndex); }
        }

        public static bool AlwaysRetryOnTransientFailure
        {
            get { return GetValue<bool>(AlwaysRetryOnTransientFailureIndex); }
        }

        public static int MaxDataReaderDegreeOfParallelism
        {
            get { return GetValue<int>(MaxDataReaderDegreeOfParallelismIndex); }
        }

        public static int TableProgressUpdateInterval
        {
            // value of zero means do not fire 'heartbeat' progress events. Non-zero values will
            //   fire a heartbeat progress event every n seconds.
            get { return GetValue<int>(TableProgressUpdateIntervalIndex); }
        }

        public static bool TraceRowCountFailure
        {
            get { return GetValue<bool>(TraceRowCountFailureIndex); }
        }

        public static bool UseOfflineDataReader
        {
            get { return GetValue<bool>(UseOfflineDataReaderIndex); }
        }

        public static StreamBackingStore StreamBackingStoreForOfflineDataReading
        {
            get { return GetValue<StreamBackingStore>(StreamBackingStoreForOfflineDataReadingIndex); }
        }

        public static bool DisableIndexesForDataPhase
        {
            get { return GetValue<bool>(DisableIndexesForDataPhaseIndex); }
        }

        public static bool ReliableDdlEnabled
        {
            get { return GetValue<bool>(ReliableDdlEnabledIndex); }
        }

        public static bool ImportModelDatabase
        {
            get { return GetValue<bool>(ImportModelDatabaseIndex); }
        }

        /// <summary>
        /// Setting that shows whether Always Encrypted is supported.
        /// If false, then reverse engineering and script interpretation of a database with any Always Encrypted object will fail
        /// </summary>
        public static bool SupportAlwaysEncrypted
        {
            get { return GetValue<bool>(SupportAlwaysEncryptedIndex); }
        }

        public static bool AlwaysEncryptedWizardMigration
        {
            get { return GetValue<bool>(AlwaysEncryptedWizardMigrationIndex); }
        }

        /// <summary>
        /// Setting that determines whether checks for unsupported object types are performed.
        /// If false, unsupported object types will prevent extract from being performed.
        /// Default value is false.
        /// </summary>
        public static bool SkipObjectTypeBlocking
        {
            get { return GetValue<bool>(SkipObjectTypeBlockingIndex); }
        }

        /// <summary>
        /// Setting that determines whether the Database Options that store Query Store settings will be left out during package serialization.
        /// Default value is false.
        /// </summary>
        public static bool DoNotSerializeQueryStoreSettings
        {
            get { return GetValue<bool>(DoNotSerializeQueryStoreSettingsIndex); }
        }

        /// <summary>
        /// Called by top-of-stack API to setup/configure settings that should be used
        /// throughout the API (lower in the stack).  The settings are reverted once the returned context
        /// has been disposed.
        /// </summary>
        public static IStackSettingsContext CreateSettingsContext()
        {
            return new StackConfiguration();
        }

        private static T1 GetValue<T1>(string configIndex)
        {
            IAmbientDataDirectAccess config = _defaultSettings;

            return (T1)config.Data[configIndex].Value;
        }

        /// <summary>
        /// Data-transfer object that represents a specific configuration
        /// </summary>
        public class AmbientData : IAmbientDataDirectAccess
        {
            private readonly Dictionary<string, AmbientValue> _configuration;

            public AmbientData()
            {
                _configuration = new Dictionary<string, AmbientValue>(StringComparer.OrdinalIgnoreCase);
                _configuration[DatabaseLockTimeoutIndex] = new AmbientValue(5000);
                _configuration[QueryTimeoutIndex] = new AmbientValue(60);
                _configuration[LongRunningQueryTimeoutIndex] = new AmbientValue(0);
                _configuration[AlwaysRetryOnTransientFailureIndex] = new AmbientValue(false);
                _configuration[ConnectionRetryHandlerIndex] = new AmbientValue(typeof(Action<SqlServerRetryError>), null);
                _configuration[MaxDataReaderDegreeOfParallelismIndex] = new AmbientValue(8);
                _configuration[TraceRowCountFailureIndex] = new AmbientValue(false);  // default: throw DacException on rowcount mismatch during import/export data validation                
                _configuration[TableProgressUpdateIntervalIndex] = new AmbientValue(300); // default: fire heartbeat progress update events every 5 minutes
                _configuration[UseOfflineDataReaderIndex] = new AmbientValue(false);
                _configuration[StreamBackingStoreForOfflineDataReadingIndex] = new AmbientValue(StreamBackingStore.File);  //applicable only when UseOfflineDataReader is set to true
                _configuration[MasterReferenceFilePathIndex] = new AmbientValue(typeof(string), null);
                // Defect 1210884: Enable an option to allow secondary index, check and fk constraints to stay enabled during data upload with import in DACFX for IES
                _configuration[DisableIndexesForDataPhaseIndex] = new AmbientValue(true);
                _configuration[ReliableDdlEnabledIndex] = new AmbientValue(false);
                _configuration[ImportModelDatabaseIndex] = new AmbientValue(true);
                _configuration[SupportAlwaysEncryptedIndex] = new AmbientValue(false);
                _configuration[AlwaysEncryptedWizardMigrationIndex] = new AmbientValue(false);
                _configuration[SkipObjectTypeBlockingIndex] = new AmbientValue(false);
                _configuration[DoNotSerializeQueryStoreSettingsIndex] = new AmbientValue(false);
            }

            public string MasterReferenceFilePath
            {
                get { return (string)_configuration[MasterReferenceFilePathIndex].Value; }
                set { _configuration[MasterReferenceFilePathIndex].Value = value; }
            }

            public int LockTimeoutMilliSeconds
            {
                get { return (int)_configuration[DatabaseLockTimeoutIndex].Value; }
                set { _configuration[DatabaseLockTimeoutIndex].Value = value; }
            }
            public int QueryTimeoutSeconds
            {
                get { return (int)_configuration[QueryTimeoutIndex].Value; }
                set { _configuration[QueryTimeoutIndex].Value = value; }
            }
            public int LongRunningQueryTimeoutSeconds
            {
                get { return (int)_configuration[LongRunningQueryTimeoutIndex].Value; }
                set { _configuration[LongRunningQueryTimeoutIndex].Value = value; }
            }
            public bool AlwaysRetryOnTransientFailure
            {
                get { return (bool)_configuration[AlwaysRetryOnTransientFailureIndex].Value; }
                set { _configuration[AlwaysRetryOnTransientFailureIndex].Value = value; }
            }
            public Action<SqlServerRetryError> ConnectionRetryMessageHandler
            {
                get { return (Action<SqlServerRetryError>)_configuration[ConnectionRetryHandlerIndex].Value; }
                set { _configuration[ConnectionRetryHandlerIndex].Value = value; }
            }
            public bool TraceRowCountFailure
            {
                get { return (bool)_configuration[TraceRowCountFailureIndex].Value; }
                set { _configuration[TraceRowCountFailureIndex].Value = value; }
            }
            public int TableProgressUpdateInterval
            {
                get { return (int)_configuration[TableProgressUpdateIntervalIndex].Value; }
                set { _configuration[TableProgressUpdateIntervalIndex].Value = value; }
            }

            public bool UseOfflineDataReader
            {
                get { return (bool)_configuration[UseOfflineDataReaderIndex].Value; }
                set { _configuration[UseOfflineDataReaderIndex].Value = value; }
            }

            public StreamBackingStore StreamBackingStoreForOfflineDataReading
            {
                get { return (StreamBackingStore)_configuration[StreamBackingStoreForOfflineDataReadingIndex].Value; }
                set { _configuration[StreamBackingStoreForOfflineDataReadingIndex].Value = value; }
            }

            public bool DisableIndexesForDataPhase
            {
                get { return (bool)_configuration[DisableIndexesForDataPhaseIndex].Value; }
                set { _configuration[DisableIndexesForDataPhaseIndex].Value = value; }
            }

            public bool ReliableDdlEnabled
            {
                get { return (bool)_configuration[ReliableDdlEnabledIndex].Value; }
                set { _configuration[ReliableDdlEnabledIndex].Value = value; }
            }

            public bool ImportModelDatabase
            {
                get { return (bool)_configuration[ImportModelDatabaseIndex].Value; }
                set { _configuration[ImportModelDatabaseIndex].Value = value; }
            }

            public bool SupportAlwaysEncrypted
            {
                get { return (bool)_configuration[SupportAlwaysEncryptedIndex].Value; }
                set { _configuration[SupportAlwaysEncryptedIndex].Value = value; }
            }

            public bool AlwaysEncryptedWizardMigration
            {
                get { return (bool)_configuration[AlwaysEncryptedWizardMigrationIndex].Value; }
                set { _configuration[AlwaysEncryptedWizardMigrationIndex].Value = value; }
            }

            public bool SkipObjectTypeBlocking
            {
                get { return (bool)_configuration[SkipObjectTypeBlockingIndex].Value; }
                set { _configuration[SkipObjectTypeBlockingIndex].Value = value; }
            }

            public bool DoNotSerializeQueryStoreSettings
            {
                get { return (bool)_configuration[DoNotSerializeQueryStoreSettingsIndex].Value; }
                set { _configuration[DoNotSerializeQueryStoreSettingsIndex].Value = value; }
            }

            /// <summary>
            /// Provides a way to bulk populate settings from a dictionary
            /// </summary>
            public void PopulateSettings(IDictionary<string, object> settingsCollection)
            {
                if (settingsCollection != null)
                {
                    Dictionary<string, object> newSettings = new Dictionary<string, object>();

                    // We know all the values are set on the current configuration
                    foreach (KeyValuePair<string, object> potentialPair in settingsCollection)
                    {
                        AmbientValue currentValue;
                        if (_configuration.TryGetValue(potentialPair.Key, out currentValue))
                        {
                            object newValue = potentialPair.Value;
                            newSettings[potentialPair.Key] = newValue;
                        }
                    }

                    if (newSettings.Count > 0)
                    {
                        foreach (KeyValuePair<string, object> newSetting in newSettings)
                        {
                            _configuration[newSetting.Key].Value = newSetting.Value;
                        }
                    }
                }
            }

            /// <summary>
            /// Logs the Ambient Settings
            /// </summary>
            public void TraceSettings()
            {
                // NOTE: logging as warning so we can get this data in the IEService DacFx logs
                Logger.Write(TraceEventType.Warning, Resources.LoggingAmbientSettings);

                foreach (KeyValuePair<string, AmbientValue> setting in _configuration)
                {
                    // Log Ambient Settings
                    Logger.Write(
                        TraceEventType.Warning,
                        string.Format(
                            Resources.AmbientSettingFormat,
                            setting.Key,
                            setting.Value == null ? setting.Value : setting.Value.Value));
                }
            }

            Dictionary<string, AmbientValue> IAmbientDataDirectAccess.Data
            {
                get { return _configuration; }
            }
        }

        /// <summary>
        /// This class is used as value in the dictionary to ensure that the type of value is correct.
        /// </summary>
        private class AmbientValue
        {
            private readonly Type _type;
            private readonly bool _isTypeNullable;
            private object _value;

            public AmbientValue(object value)
                : this(value == null ? null : value.GetType(), value)
            {
            }

            public AmbientValue(Type type, object value)
            {
                if (type == null)
                {
                    throw new ArgumentNullException("type");
                }
                _type = type;
                _isTypeNullable = !type.GetTypeInfo().IsValueType || Nullable.GetUnderlyingType(type) != null;
                Value = value;
            }

            public object Value
            {
                get { return _value; }
                set
                {
                    if ((_isTypeNullable && value == null) || _type.GetTypeInfo().IsInstanceOfType(value))
                    {
                        _value = value;
                    }
                    else
                    {
                        Logger.Write(TraceEventType.Error, string.Format(Resources.UnableToAssignValue, value.GetType().FullName, _type.FullName));
                    }
                }
            }
        }

        /// <summary>
        /// This private interface allows pass-through access directly to member data
        /// </summary>
        private interface IAmbientDataDirectAccess
        {
            Dictionary<string, AmbientValue> Data { get; }
        }

        /// <summary>
        /// This class encapsulated the concept of configuration that is set on the stack and
        /// flows across multiple threads as part of the logical call context
        /// </summary>
        private sealed class StackConfiguration : IStackSettingsContext
        {
            private readonly AmbientData _data;

            public StackConfiguration()
            {
                _data = new AmbientData();
                //CallContext.LogicalSetData(LogicalContextName, _data);
            }

            public AmbientData Settings
            {
                get { return _data; }
            }

            public void Dispose()
            {
                Dispose(true);
            }
            private void Dispose(bool disposing)
            {
                //CallContext.LogicalSetData(LogicalContextName, null);
            }
        }
    }
}
