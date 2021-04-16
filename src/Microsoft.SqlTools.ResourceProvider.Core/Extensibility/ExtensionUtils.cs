//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.SqlTools.Extensibility;

namespace Microsoft.SqlTools.ResourceProvider.Core.Extensibility
{
    /// <summary>
    /// Extension methods for exportable and service 
    /// </summary>
    public static class ExtensionUtils
    {

        /// <summary>
        /// Finds a service of specific type which has the same metadata as class using the service provider.
        /// If multiple services found, the one with the highest priority will be returned
        /// </summary>
        /// <typeparam name="T">The type of the service</typeparam>
        /// <returns>A service of type T or null if not found</returns>
        public static T GetService<T>(this IMultiServiceProvider provider, IServerDefinition serverDefinition)
            where T : IExportable
        {
            return provider.GetServices<T>()
                    .FilterExportables(serverDefinition)
                    .OrderByDescending(s => SortOrder(s)).
                    FirstOrDefault();
        }

        private static int SortOrder<T>(T service)
        {
            IExportable exportable = service as IExportable;
            if (exportable != null)
            {
                return exportable.Metadata.Priority;
            }
            return 0;
        }

        public static IEnumerable<T> FilterExportables<T>(this IEnumerable<T> exportables, IServerDefinition serverDefinition = null)
             where T : IExportable
        {
            if (exportables == null)
            {
                return null;
            }
            //Get all the possible matches 
            IEnumerable<T> allMatched = serverDefinition != null ?
                exportables.Where(x => Match(x.Metadata, serverDefinition)).ToList() : exportables;
            IList<T> list = allMatched.ToList();

            //If specific server type requested and the list has any item with that server type remove the others.
            //for instance is there's server for all server types and one specifically for sql and give metadata is asking for sql then
            //we should return the sql one even if the other service has higher priority 

            IList<T> withSameServerType = list.Where(x => serverDefinition.HasSameServerName(x.Metadata)).ToList();
            if (withSameServerType.Any())
            {
                list = withSameServerType;
            }
            IList<T> withSameCategory = list.Where(x => serverDefinition.HasSameCategory(x.Metadata)).ToList();
            if (withSameCategory.Any())
            {
                list = withSameCategory;
            }
            return list;
        }

        public static bool HasSameServerName(this IServerDefinition serverDefinition, IServerDefinition metadata)
        {
            if (serverDefinition != null && metadata != null)
            {
                // Note: this does not handle null <-> string.Empty equivalence. For now ignoring this as it should not matter
                return string.Equals(serverDefinition.ServerType, metadata.ServerType, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        public static bool HasSameCategory(this IServerDefinition serverDefinition, IServerDefinition metadata)
        {
            if (serverDefinition != null && metadata != null)
            {
                // Note: this does not handle null <-> string.Empty equivalence. For now ignoring this as it should not matter
                return string.Equals(serverDefinition.Category, metadata.Category, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        /// <summary>
        /// Returns true if the given server definition is secure type. (i.e.  Azure)
        /// </summary>
        internal static bool IsSecure(this IServerDefinition serverDefinition)
        {
            if (serverDefinition != null && serverDefinition.Category != null)
            {
                return serverDefinition.Category.Equals(Categories.Azure, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }
        
        internal static string GetServerDefinitionKey(this IServerDefinition serverDefinition)
        {
            string key = string.Empty;
            if (serverDefinition != null)
            {
                key = string.Format(CultureInfo.InvariantCulture, "{0}", GetKey(serverDefinition.Category));
            }

            return key;
        }

        internal static bool EqualsServerDefinition(this IServerDefinition serverDefinition, IServerDefinition otherServerDefinition)
        {
            if (serverDefinition == null && otherServerDefinition == null)
            {
                return true;
            }
            if (serverDefinition != null && otherServerDefinition != null)
            {
                return (((string.IsNullOrEmpty(serverDefinition.Category) && string.IsNullOrEmpty(otherServerDefinition.Category)) || serverDefinition.HasSameCategory(otherServerDefinition)) &&
                    ((string.IsNullOrEmpty(serverDefinition.ServerType) && string.IsNullOrEmpty(otherServerDefinition.ServerType)) || serverDefinition.HasSameServerName(otherServerDefinition)));
            }
            return false;
        }
        
        internal static bool EmptyOrEqual(this string value1, string value2)
        {
            if (string.IsNullOrEmpty(value1) && string.IsNullOrEmpty(value2))
            {
                return true;
            }
            return value1 == value2;
        }

        private static string GetKey(string name)
        {
            return string.IsNullOrEmpty(name) ? string.Empty : name.ToUpperInvariant();
        }

        /// <summary>
        /// Returns true if the metadata matches the given server definition
        /// </summary>       
        public static bool Match(this IServerDefinition first, IServerDefinition other)
        {
            if (first == null)
            {
                // TODO should we handle this differently? 
                return false;
            }
            if (other == null)
            {
                return false;
            }
            return MatchMetaData(first.ServerType, other.ServerType)
                && MatchMetaData(first.Category, other.Category);
        }
        
        /// <summary>
        /// Returns true if the metadata value matches the given value
        /// </summary>   
        private static bool MatchMetaData(string metaData, string requestedMetaData)
        {
            if (string.IsNullOrEmpty(metaData) || string.IsNullOrEmpty(requestedMetaData))
            {
                return true;
            }
            return (metaData.Equals(requestedMetaData, StringComparison.OrdinalIgnoreCase));
        }

    }
}
