//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Data;

namespace Microsoft.SqlTools.ServiceLayer.LanguageExtensions
{
    public class LanguageExtensionOperations
    {
        private const string LanguageStatusScript = @"SELECT is_installed
FROM sys.dm_db_external_language_stats s, sys.external_languages l
WHERE s.external_language_id = l.external_language_id AND language = @LanguageName";

        /// <summary>
        /// Returns the status of external languages in a connection
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="languageName"></param>
        /// <returns></returns>
        public bool GetLanguageStatus(IDbConnection connection, string languageName)
        {
            bool status = false;
            try
            {
                using (IDbCommand command = connection.CreateCommand())
                {
                    command.CommandText = LanguageStatusScript;
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = "@LanguageName";
                    parameter.Value = languageName;
                    command.Parameters.Add(parameter);
                    using (IDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            status = (reader[0].ToString() == "True");
                        }
                    }
                }
            }
            catch
            {
                status = false;
            }

            return status;
        }
    }
}
