//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Connection.ReliableConnection
{
    /// <summary>
    /// This class caches server information for subsequent use
    /// </summary>
    internal static class CachedServerInfo
    {
        public enum CacheVariable {
            IsSqlDw,
            IsAzure
        }

        private struct CachedInfo
        {
            public bool IsAzure;
            public DateTime LastUpdate;
            public bool IsSqlDw;
        }

        private static ConcurrentDictionary<string, CachedInfo> _cache;
        private static object _cacheLock;
        private const int _maxCacheSize = 1024;
        private const int _deleteBatchSize = 512;
        private const int MinimalQueryTimeoutSecondsForAzure = 300;

        static CachedServerInfo()
        {
            _cache = new ConcurrentDictionary<string, CachedInfo>(StringComparer.OrdinalIgnoreCase);
            _cacheLock = new object();
        }

        public static int GetQueryTimeoutSeconds(IDbConnection connection)
        {
            string dataSource = SafeGetDataSourceFromConnection(connection);
            return GetQueryTimeoutSeconds(dataSource);
        }

        public static int GetQueryTimeoutSeconds(string dataSource)
        {
            //keep existing behavior and return the default ambient settings
            //if the provided data source is null or whitespace, or the original
            //setting is already 0 which means no limit.
            int originalValue = AmbientSettings.QueryTimeoutSeconds;
            if (string.IsNullOrWhiteSpace(dataSource)
                || (originalValue == 0))
            {
                return originalValue;
            }

            CachedInfo info;
            bool hasFound = _cache.TryGetValue(dataSource, out info);

            if (hasFound && info.IsAzure
                && originalValue < MinimalQueryTimeoutSecondsForAzure)
            {
                return MinimalQueryTimeoutSecondsForAzure;
            }
            else
            {
                return originalValue;
            }
        }

        public static void AddOrUpdateIsAzure(IDbConnection connection, bool isAzure)
        {
            AddOrUpdateCache(connection, isAzure, CacheVariable.IsAzure);
        }

        public static void AddOrUpdateIsSqlDw(IDbConnection connection, bool isSqlDw)
        {
            AddOrUpdateCache(connection, isSqlDw, CacheVariable.IsSqlDw);
        }

        private static void AddOrUpdateCache(IDbConnection connection, bool newState, CacheVariable cacheVar)
        {
            Validate.IsNotNull(nameof(connection), connection);
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(connection.ConnectionString);
            AddOrUpdateCache(builder.DataSource, newState, cacheVar);
        }

        internal static void AddOrUpdateCache(string dataSource, bool newState, CacheVariable cacheVar)
        {
            Validate.IsNotNullOrWhitespaceString(nameof(dataSource), dataSource);
            CachedInfo info;
            bool hasFound = _cache.TryGetValue(dataSource, out info);

            if ((cacheVar == CacheVariable.IsSqlDw && hasFound && info.IsSqlDw == newState) || 
                (cacheVar == CacheVariable.IsAzure && hasFound && info.IsAzure == newState)) 
            {
                return;
            }
            else
            {
                lock (_cacheLock)
                {
                    if (! _cache.ContainsKey(dataSource))
                    {
                        //delete a batch of old elements when we try to add a new one and
                        //the capacity limitation is hit
                        if (_cache.Keys.Count > _maxCacheSize - 1)
                        {
                            var keysToDelete = _cache
                                .OrderBy(x => x.Value.LastUpdate)
                                .Take(_deleteBatchSize)
                                .Select(pair => pair.Key);

                            foreach (string key in keysToDelete)
                            {
                                _cache.TryRemove(key, out info);
                            }
                        }
                    }

                    if (cacheVar == CacheVariable.IsSqlDw)
                    {
                        info.IsSqlDw = newState;
                    }
                    else if (cacheVar == CacheVariable.IsAzure)
                    {
                        info.IsAzure = newState;
                    }
                    info.LastUpdate = DateTime.UtcNow;
                    _cache.AddOrUpdate(dataSource, info, (key, oldValue) => info);
                }
            }
        }

        public static bool TryGetIsSqlDw(IDbConnection connection, out bool isSqlDw)
        {
            Validate.IsNotNull(nameof(connection), connection);

            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(connection.ConnectionString);
            return TryGetIsSqlDw(builder.DataSource, out isSqlDw);
        }

        public static bool TryGetIsSqlDw(string dataSource, out bool isSqlDw)
        {
            Validate.IsNotNullOrWhitespaceString(nameof(dataSource), dataSource);
            CachedInfo info;
            bool hasFound = _cache.TryGetValue(dataSource, out info);

            if(hasFound)
            {
                isSqlDw = info.IsSqlDw;
                return true;
            }
            
            isSqlDw = false;
            return false;
        }

        private static string SafeGetDataSourceFromConnection(IDbConnection connection)
        {
            if (connection == null)
            {
                return null;
            }

            try
            {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(connection.ConnectionString);
                return builder.DataSource;
            }
            catch
            {
                Logger.Write(LogLevel.Error,  String.Format(Resources.FailedToParseConnectionString, connection.ConnectionString));
                return null;
            }
        }
    }
}
