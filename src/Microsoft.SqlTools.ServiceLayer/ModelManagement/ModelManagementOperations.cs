//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.Management;
using Microsoft.SqlTools.ServiceLayer.ModelManagement.Contracts;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.SqlTools.ServiceLayer.Utility;
using System;

namespace Microsoft.SqlTools.ServiceLayer.ModelManagement
{

    public class ModelManagementOperations
    {
        private string database;
        private string table;
        public ModelManagementOperations(string database, string table)
        {
            this.database = database;
            this.table = table;
        }

        private const string ModelIdParamName = "id";
        private const string ModelNameParamName = "name";
        private const string ModelDescriptionParamName = "desciprtion";
        private const string ModelVersionParamName = "version";
        private const string ModelCreatedParamName = "created";
        private const string ModelContentParamName = "content";
        private const string ModelArtifactNameParamName = "artifact_name";
   
        private static string GetModelsScript(string table)
        {
            return $@"
SELECT artifact_id, artifact_name, name, description, version, created
FROM {table}
ORDER BY name";
        }

        private static string GetModelScript(string table)
        {
           return $@"
SELECT artifact_id, artifact_name, name, description, version, created
FROM {table}
Where name=@{ModelNameParamName}";
        }

        private static string GetModelContentScript(string table)
        {
            return $@"
SELECT artifact_content
FROM {table}
Where name=@{ModelNameParamName}";
        }

        private static string AddModelScript(string table)
        {
            return $@"
INSERT INTO {table}
(name, artifact_name, description, version, created, artifact_content)
VALUES
(@{ModelNameParamName}, @{ModelArtifactNameParamName}, @{ModelDescriptionParamName}, @{ModelVersionParamName}, @{ModelCreatedParamName}, @{ModelContentParamName})";
        }

        private static string UpdateModelScript(string table)
        {
            return $@"
UPDATE [{CUtils.EscapeStringCBracket(table)}]
SET
name = @{ModelNameParamName},
name = @{ModelNameParamName},
description = @{ModelDescriptionParamName},
version = @{ModelVersionParamName}
WHERE artifact_id = @{ModelIdParamName}";
        }

        private static string DeleteModelScript(string table)
        {
            return $@"
DELETE FROM [{CUtils.EscapeStringCBracket(table)}]
WHERE name=@{ModelNameParamName}";
        }

        private static string CreateModelsTableScript(string table)
        {
            return $@"
IF NOT EXISTS
   (  SELECT [name]
      FROM sys.tables
      WHERE [name] = '{CUtils.EscapeStringSQuote(table)}'
   )
CREATE TABLE [dbo].[{CUtils.EscapeStringCBracket(table)}] (
	[artifact_id] [int] IDENTITY(1,1) NOT NULL,
	[artifact_name] [varchar](256) NOT NULL,
	[gtoup_path] [varchar](256) NULL,
	[artifact_content] [varbinary](max) NOT NULL,
	[artifact_initial_size] [bigint] NULL,
    [name] [varchar](256) NOT NULL,
    [version] [varchar](256) NOT NULL,
	[created] [datetime] NOT NULL,
	[description] [varchar](256) NULL,
 CONSTRAINT [artifact_pk] PRIMARY KEY CLUSTERED 
(
	[artifact_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
ELSE
BEGIN
IF NOT EXISTS (SELECT * FROM syscolumns WHERE ID=OBJECT_ID('[dbo].[{CUtils.EscapeStringCBracket(table)}]') AND NAME='name')
ALTER TABLE [dbo].[{CUtils.EscapeStringCBracket(table)}]
ADD 
[name] [varchar](256) NOT NULL,
[version] [varchar](256) NOT NULL,
[created] [datetime] NOT NULL,
[description] [varchar](256) NULL
END
";
        }

        private static string CreateModelsDatabaseScript(string database)
        {
            return $@"
IF NOT EXISTS (
    SELECT [name]
        FROM sys.databases
        WHERE [name] = N'{CUtils.EscapeStringSQuote(database)}'
)
CREATE DATABASE [{CUtils.EscapeStringCBracket(database)}]
";
        }
    
      
        /// <summary>
        /// Returns the list of models
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        public virtual List<RegisteredModel> GetModels(IDbConnection connection)
        {
            ConfigureTable(connection);
            return GetModels(connection, null);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="modelName"></param>
        /// <returns></returns>
        public virtual RegisteredModel GetModel(IDbConnection connection, string name)
        {
            ConfigureTable(connection);
            List<RegisteredModel> result = GetModels(connection, name);
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
        /// <param name="model"></param>
        public virtual void UpdateModel(IDbConnection connection, RegisteredModel model)
        {
            ConfigureTable(connection);
            if (string.IsNullOrWhiteSpace(model?.Name))
            {
                throw new ModelManagementException($"Failed to update model. model or model name is empty.");
            }

            Dictionary<string, object> parameters = new Dictionary<string, object>();
            parameters.Add(ModelContentParamName, FileUtilities.GetFileContent(model.FilePath));
            parameters.Add(ModelCreatedParamName, DateTime.Now);
            parameters.Add(ModelDescriptionParamName, model.Description);
            parameters.Add(ModelVersionParamName, model.Version);
            parameters.Add(ModelNameParamName, model.Name);
            RegisteredModel currentData = GetModel(connection, model.Name);
            if (currentData == null)
            {
                ExecuteNonQuery(connection, AddModelScript(table), parameters);
            }
            else
            {
                parameters.Add(ModelIdParamName, currentData.Id);
                ExecuteNonQuery(connection, UpdateModelScript(table), parameters);
            }
        }

        public virtual void DeleteModel(IDbConnection connection, string name)
        {
            ConfigureTable(connection);
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ModelManagementException($"Failed to delete model. model name is empty");
            }
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            parameters.Add(ModelNameParamName, name);
            ExecuteNonQuery(connection, DeleteModelScript(table), parameters);
        }

        /// <summary>
        /// Returns the status of external models in a connection
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="modelName"></param>
        /// <returns></returns>
        private List<RegisteredModel> GetModels(IDbConnection connection, string modelName = null)
        {
            List<RegisteredModel> list = new List<RegisteredModel>();
            using (IDbCommand command = connection.CreateCommand())
            {
                command.CommandText = modelName != null ? GetModelScript(table) : GetModelsScript(table);
                if (modelName != null)
                {
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = $"@{ModelNameParamName}";
                    parameter.Value = modelName;
                    command.Parameters.Add(parameter);
                }
                using (IDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int id = reader.GetInt32(0);
                        string name = reader.GetString(1);
                        string description = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);
                        string version = reader.IsDBNull(3) ? string.Empty : reader.GetString(3);
                        string createdDate = reader.IsDBNull(4) ? string.Empty : reader.GetDateTime(4).ToString();
                       
                        list.Add(new RegisteredModel
                        {
                            Name = name,
                            Version = version,
                            Description = description,
                            CreatedDate = createdDate,
                            Id = id
                        });
                    }
                }
            }
            return list;
        }

        private void ConfigureTable(IDbConnection connection)
        {
            QueryUtils.ExecuteNonQuery(connection, CreateModelsDatabaseScript(database));
            ExecuteNonQuery(connection, CreateModelsTableScript(table));
        }
        public void ExecuteNonQuery(IDbConnection connection, string script, Dictionary<string, object> parameters = null)
        {
            var currenDbName = connection.Database;
            connection.ChangeDatabase(database);

            QueryUtils.ExecuteNonQuery(connection, script, parameters);
            connection.ChangeDatabase(database);

        }

        private string AddStringParameter(string paramName, string prefix, string paramValue)
        {
            string value = string.IsNullOrWhiteSpace(paramValue) ? paramValue : CUtils.EscapeStringSQuote(paramValue);
            return $"{prefix} {paramName} = N'{value}'";
        }
    }
}
