//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.LanguageExtensibility.Contracts;
using Microsoft.SqlTools.ServiceLayer.Management;
using Microsoft.SqlTools.Utility;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Microsoft.SqlTools.ServiceLayer.LanguageExtensibility
{

    public enum ModifyType
    {
        Create,
        Alter
    }

    public enum ContentModifyType
    {
        Modify,
        Add,
        Remove
    }

    public class ExternalLanguageOperations
    {
        private static string StatusScript = $@"
SELECT is_installed
FROM sys.dm_db_external_language_stats s, sys.external_languages l
WHERE s.external_language_id = l.external_language_id AND language = @{LanguageNameParamName}";

        private const string GetAllScript = @"
SELECT l.external_language_id, language, l.create_date, dp.name, content, file_name, platform_desc, parameters, environment_variables
FROM sys.external_languages l 
JOIN sys.external_language_files lf on l.external_language_id = lf.external_language_id
JOIN sys.database_principals dp on l.principal_id = dp.principal_id
ORDER BY l.external_language_id, platform";

        private static string GetLanguageScript = $@"
SELECT l.external_language_id, language, l.create_date, dp.name, content, file_name, platform_desc, parameters, environment_variables
FROM  sys.external_languages l 
JOIN sys.external_language_files lf on l.external_language_id = lf.external_language_id
JOIN sys.database_principals dp on l.principal_id = dp.principal_id
WHERE language=@{LanguageNameParamName}
ORDER BY platform";

        public const string CreateScript = "CREATE EXTERNAL LANGUAGE";
        public const string DropScript = "DROP EXTERNAL LANGUAGE";
        public const string AlterScript = "ALTER EXTERNAL LANGUAGE";
        public const string SetContentScript = "SET";
        public const string AddContentScript = "ADD";
        public const string RemoveContentScript = "REMOVE";
        private const string ContentParamName = "CONTENT";
        private const string FileNameParamName = "FILE_NAME";
        private const string PlatformParamName = "PLATFORM";
        private const string EnvVariablesParamName = "ENVIRONMENT_VARIABLES";
        private const string ParametersParamName = "PARAMETERS";
        private const string LanguageNameParamName = "LANGUAGE";

        private string GetDropScript(string languageName)
        {
            return $@"{DropScript} [{languageName}]";
        }

        /// <summary>
        /// Returns the status of external languages in a connection
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="languageName"></param>
        /// <returns></returns>
        public virtual bool GetLanguageStatus(IDbConnection connection, string languageName)
        {
            bool status = false;
            try
            {
                using (IDbCommand command = connection.CreateCommand())
                {
                    command.CommandText = StatusScript;
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = $"@{LanguageNameParamName}";
                    parameter.Value = languageName;
                    command.Parameters.Add(parameter);
                    using (IDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            status = (Convert.ToBoolean(reader[0].ToString()));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Write(TraceEventType.Warning, $"Failed to get language status for language: {languageName}, error: {ex.Message}");
                status = false;
            }

            return status;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        public virtual List<ExternalLanguage> GetLanguages(IDbConnection connection)
        {
            return GetLanguages(connection, null);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="languageName"></param>
        /// <returns></returns>
        public virtual ExternalLanguage GetLanguage(IDbConnection connection, string languageName)
        {
            List<ExternalLanguage> result = GetLanguages(connection, languageName);
            if (result != null && result.Any())
            {
                return result.First();
            }
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="language"></param>
        public virtual void UpdateLanguage(IDbConnection connection, ExternalLanguage language)
        {
            if (language == null)
            {
                return;
            }

            Dictionary<string, object> parameters = new Dictionary<string, object>();
            ExternalLanguage currentLanguage = GetLanguage(connection, language.Name);
            if (currentLanguage == null)
            {
                ExecuteNonQuery(connection, GetCreateScript(language, parameters), parameters);
            }
            else
            {
                foreach (var content in language.Contents)
                {
                    var currentContent = currentLanguage.Contents.FirstOrDefault(x => x.PlatformId == content.PlatformId);
                    if (currentContent != null)
                    {
                        ExecuteNonQuery(connection, GetUpdateScript(language, content, parameters, ContentModifyType.Modify), parameters);
                    }
                    else
                    {
                        ExecuteNonQuery(connection, GetUpdateScript(language, content, parameters, ContentModifyType.Add), parameters);

                    }
                }
                foreach (var currentContent in currentLanguage.Contents)
                {
                    var content = language.Contents.FirstOrDefault(x => x.PlatformId == currentContent.PlatformId);
                    if (content == null)
                    {
                        ExecuteNonQuery(connection, GetUpdateScript(language, currentContent, parameters, ContentModifyType.Remove), parameters);

                    }
                }
            }
        }

        public virtual void DeleteLanguage(IDbConnection connection, string languageName)
        {
            if (string.IsNullOrWhiteSpace(languageName))
            {
                throw new LanguageExtensibilityException($"Invalid language name. name: {languageName}");
            }
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            ExecuteNonQuery(connection, GetDropScript(languageName), parameters);
        }

        /// <summary>
        /// Returns the status of external languages in a connection
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="languageName"></param>
        /// <returns></returns>
        private List<ExternalLanguage> GetLanguages(IDbConnection connection, string languageName = null)
        {
            Dictionary<int, ExternalLanguage> dic = new Dictionary<int, ExternalLanguage>();
            using (IDbCommand command = connection.CreateCommand())
            {
                command.CommandText = languageName != null ? GetLanguageScript : GetAllScript;
                if (languageName != null)
                {
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = $"@{LanguageNameParamName}";
                    parameter.Value = languageName;
                    command.Parameters.Add(parameter);
                }
                using (IDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int id = reader.GetInt32(0);
                        string name = reader.GetString(1);
                        string createdDate = reader.IsDBNull(2) ? string.Empty : reader.GetDateTime(2).ToString();
                        string owner = reader.IsDBNull(3) ? string.Empty : reader.GetString(3);
                        string extentionFileName = reader.IsDBNull(5) ? string.Empty : reader.GetString(5);
                        string platform = reader.IsDBNull(6) ? string.Empty : reader.GetString(6);
                        string parameters = reader.IsDBNull(7) ? string.Empty : reader.GetString(7);
                        string envVariables = reader.IsDBNull(8) ? string.Empty : reader.GetString(8);
                        if (!dic.ContainsKey(id))
                        {
                            dic.Add(id, new ExternalLanguage
                            {
                                Name = name,
                                Owner = owner,
                                CreatedDate = createdDate,
                                Contents = new List<ExternalLanguageContent>()
                            });
                        }
                        ExternalLanguage metadata = dic[id];
                        metadata.Contents.Add(new ExternalLanguageContent
                        {
                            EnvironmentVariables = envVariables,
                            Parameters = parameters,
                            Platform = platform,
                            ExtensionFileName = extentionFileName
                        });
                    }
                }
            }
            return new List<ExternalLanguage>(dic.Values);
        }

        private string GetCreateScript(ExternalLanguage language, Dictionary<string, object> parameters)
        {
            return GetLanguageModifyScript(language, language.Contents, parameters, ModifyType.Create);
        }

        private string GetUpdateScript(ExternalLanguage language, ExternalLanguageContent content, Dictionary<string, object> parameters, ContentModifyType contentModifyType)
        {
            return GetLanguageModifyScript(language, new List<ExternalLanguageContent> { content }, parameters, ModifyType.Alter, contentModifyType);
        }

        private string GetLanguageModifyScript(ExternalLanguage language, List<ExternalLanguageContent> contents, Dictionary<string, object> parameters, ModifyType modifyType, ContentModifyType contentModifyType = ContentModifyType.Add)
        {
            string contentScript = string.Empty;
            for (int i = 0; i < contents.Count; i++)
            {
                var content = contents[i];
                string seperator = contentScript == string.Empty ? "" : ",";
                contentScript = $"{contentScript}{seperator}{GetLanguageContent(content, i, parameters)}";
            }

            string ownerScript = string.IsNullOrWhiteSpace(language.Owner) ? "" : $"AUTHORIZATION {language.Owner}";
            string scriptAction = modifyType == ModifyType.Create ? CreateScript : AlterScript;
            string contentAction = "FROM";
            if (modifyType == ModifyType.Alter)
            {
                switch (contentModifyType)
                {
                    case ContentModifyType.Modify:
                        contentAction = SetContentScript;
                        break;
                    case ContentModifyType.Add:
                        contentAction = AddContentScript;
                        break;
                    case ContentModifyType.Remove:
                        contentAction = RemoveContentScript;
                        break;
                }
            }
            return $@"
{scriptAction} [{language.Name}]
{ownerScript}
{contentAction} {contentScript}
";
        }

        private string AddStringParameter(string paramName, string prefix, string postfix, string paramValue)
        {
            string script = string.Empty;
            
            if (!string.IsNullOrWhiteSpace(paramValue))
            {
                script = $"{prefix} {paramName} = N'{CUtils.EscapeStringSQuote(paramValue)}'";
            }
            
            return script;
        }

        private string GetLanguageContent(ExternalLanguageContent content, int index, Dictionary<string, object> parameters)
        {
            string postfix = index.ToString();
            string prefix = ",";
            string contentScript = string.Empty;
            if (content.IsLocalFile)
            {
                byte[] contentBytes;
                using (var stream = new FileStream(content.PathToExtension, FileMode.Open, FileAccess.Read))
                {
                    using (var reader = new BinaryReader(stream))
                    {
                        contentBytes = reader.ReadBytes((int)stream.Length);
                    }
                }
                parameters.Add($"{ContentParamName}{postfix}", contentBytes);
                contentScript = $"CONTENT = @{ContentParamName}{postfix}";
            }
            else if (!string.IsNullOrWhiteSpace(content.PathToExtension))
            {
                contentScript = $"{AddStringParameter(ContentParamName, string.Empty, postfix, content.PathToExtension)}";
            }
            return $@"( 
                      {contentScript}
                      {AddStringParameter(FileNameParamName, string.IsNullOrWhiteSpace(contentScript) ? string.Empty : prefix,
                      postfix, content.ExtensionFileName)}
                      {AddStringParameter(ParametersParamName, prefix, postfix, content.Parameters)}
                      {AddStringParameter(EnvVariablesParamName, prefix, postfix, content.EnvironmentVariables)}
                      )";
        }

        private void ExecuteNonQuery(IDbConnection connection, string script, Dictionary<string, object> parameters)
        {
            using (IDbCommand command = connection.CreateCommand())
            {
                command.CommandText = script;
                foreach (var item in parameters)
                {
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = item.Key;
                    parameter.Value = item.Value;
                    command.Parameters.Add(parameter);
                }

                command.ExecuteNonQuery();
            }
        }
    }
}
