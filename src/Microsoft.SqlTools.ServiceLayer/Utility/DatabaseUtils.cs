//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.ServiceLayer.Management;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security;

namespace Microsoft.SqlTools.ServiceLayer.Utility
{
    public class DatabaseUtils
    {
        /// <summary>
        /// Check if the database is a system database
        /// </summary>
        /// <param name="databaseName">the name of database</param>
        /// <returns>return true if the database is a system database</returns>
        public static bool IsSystemDatabaseConnection(string databaseName)
        {
            return (string.IsNullOrWhiteSpace(databaseName) ||
                string.Compare(databaseName, CommonConstants.MasterDatabaseName, StringComparison.OrdinalIgnoreCase) == 0 ||
                string.Compare(databaseName, CommonConstants.MsdbDatabaseName, StringComparison.OrdinalIgnoreCase) == 0 ||
                string.Compare(databaseName, CommonConstants.ModelDatabaseName, StringComparison.OrdinalIgnoreCase) == 0 ||
                string.Compare(databaseName, CommonConstants.TempDbDatabaseName, StringComparison.OrdinalIgnoreCase) == 0);
        }

        public static string AddStringParameterForInsert(string paramValue)
        {
            string value = string.IsNullOrWhiteSpace(paramValue) ? paramValue : CUtils.EscapeStringSQuote(paramValue);
            return $"'{value}'";
        }

        public static string AddStringParameterForUpdate(string columnName, string paramValue)
        {
            string value = string.IsNullOrWhiteSpace(paramValue) ? paramValue : CUtils.EscapeStringSQuote(paramValue);
            return $"{columnName} = N'{value}'";
        }

        public static string AddByteArrayParameterForUpdate(string columnName, string paramName, string fileName, Dictionary<string, object> parameters)
        {
            byte[] contentBytes;
            using (var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                using (var reader = new BinaryReader(stream))
                {
                    contentBytes = reader.ReadBytes((int)stream.Length);
                }
            }
            parameters.Add($"{paramName}", contentBytes);
            return $"{columnName} = @{paramName}";
        }

        public static string AddByteArrayParameterForInsert(string paramName, string fileName, Dictionary<string, object> parameters)
        {
            byte[] contentBytes;
            using (var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                using (var reader = new BinaryReader(stream))
                {
                    contentBytes = reader.ReadBytes((int)stream.Length);
                }
            }
            parameters.Add($"{paramName}", contentBytes);
            return $"@{paramName}";
        }

        public static SecureString GetReadOnlySecureString(string secret)
        {
            SecureString ss = new SecureString();
            foreach (char c in secret.ToCharArray())
            {
                ss.AppendChar(c);
            }
            ss.MakeReadOnly();

            return ss;
        }
    }
}
