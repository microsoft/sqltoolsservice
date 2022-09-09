//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using System;
using System.Text.RegularExpressions;
using Microsoft.SqlTools.BatchParser.Utility;
using Microsoft.SqlTools.ServiceLayer.Rename.Requests;

namespace Microsoft.SqlTools.ServiceLayer.Rename
{
    /// <summary>
    /// Help class for Rename Service
    /// </summary>
    public static class RenameUtils
    {
        private static readonly string RegexMatchPatternValidObjectName = @"^[\p{L}_][\p{L}\p{N}@$#_]{0,127}$";
        public static void Validate(ProcessRenameEditRequestParams requestParams)
        {
            if (requestParams == null)
            {
                throw new ArgumentNullException();
            }
            if (String.IsNullOrEmpty(requestParams.TableInfo.TableName) || String.IsNullOrEmpty(requestParams.TableInfo.Database) || String.IsNullOrEmpty(requestParams.TableInfo.Schema) || String.IsNullOrEmpty(requestParams.TableInfo.OldName) || String.IsNullOrEmpty(requestParams.ChangeInfo.NewName))
            {
                throw new ArgumentException(SR.RenameRequestParametersNotNullOrEmpty);
            }
            if (Regex.IsMatch(requestParams.TableInfo.Schema, RegexMatchPatternValidObjectName) || Regex.IsMatch(requestParams.TableInfo.Database, RegexMatchPatternValidObjectName) || Regex.IsMatch(requestParams.TableInfo.OldName, RegexMatchPatternValidObjectName) || Regex.IsMatch(requestParams.TableInfo.TableName, RegexMatchPatternValidObjectName) || Regex.IsMatch(requestParams.ChangeInfo.NewName, RegexMatchPatternValidObjectName))
            {
                throw new ArgumentOutOfRangeException(SR.NotAllowedCharacterForRenaming);
            }
        }

        public static string CombineTableNameWithSchema(string schema, string tableName)
        {
            schema = schema.Replace("[", "").Replace("]", "").Trim();
            tableName = tableName.Replace("[", "").Replace("]", "").Trim();
            return String.Join(".", schema, tableName);
        }

        public static string GetRenameSQLCommand(ProcessRenameEditRequestParams requestParams)
        {
            Logger.Verbose("Inside in the GetRenameSQLCommand()-Method");
            if (requestParams.ChangeInfo.Type == ChangeType.COLUMN)
            {
                return String.Format(@"
                USE [{0}];
                    EXEC sp_rename @objname = '{1}', @newname = '{2}', @objtype ='{3}';
            ", requestParams.TableInfo.Database, RenameUtils.CombineTableNameWithSchema(requestParams.TableInfo.Schema, requestParams.TableInfo.TableName) + "." + requestParams.TableInfo.OldName, requestParams.ChangeInfo.NewName, Enum.GetName(requestParams.ChangeInfo.Type));
            }
            else
            {
                return String.Format(@"
                USE [{0}];
                    EXEC sp_rename @objname = '{1}', @newname = '{2}';
            ", requestParams.TableInfo.Database, RenameUtils.CombineTableNameWithSchema(requestParams.TableInfo.Schema, requestParams.TableInfo.OldName), requestParams.ChangeInfo.NewName);
            }
        }
    }
}