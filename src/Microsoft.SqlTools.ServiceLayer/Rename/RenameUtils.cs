//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using System;
using Microsoft.SqlTools.ServiceLayer.Rename.Requests;

namespace Microsoft.SqlTools.ServiceLayer.Rename
{
    /// <summary>
    /// Help class for Rename Service
    /// </summary>
    public static class RenameUtils
    {
        public static void Validate(ProcessRenameEditRequestParams requestParams)
        {
            if (requestParams.TableInfo.IsNewTable == true)
            {
                throw new InvalidOperationException(SR.TableDoesNotExist);
            }
            if (requestParams.ChangeInfo.Type == ChangeType.COLUMN)
            {
                throw new NotImplementedException(SR.FeatureNotYetImplemented);
            }
            if (String.IsNullOrEmpty(requestParams.TableInfo.Schema) || String.IsNullOrEmpty(requestParams.TableInfo.TableName) || String.IsNullOrEmpty(requestParams.TableInfo.ConnectionString) || String.IsNullOrEmpty(requestParams.TableInfo.Server) || String.IsNullOrEmpty(requestParams.TableInfo.Id))
            {
                throw new ArgumentException(SR.RenameRequestParametersNotNullOrEmpty);
            }
        }

        public static string CombineTableNameWithSchema(string schema, string tableName)
        {
            schema = schema.Replace("[", "").Replace("]", "").Trim();
            tableName = tableName.Replace("[", "").Replace("]", "").Trim();
            return String.Join(".", schema, tableName);
        }
    }
}