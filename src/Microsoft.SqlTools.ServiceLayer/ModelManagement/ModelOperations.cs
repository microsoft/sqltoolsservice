//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.ServiceLayer.Management;
using Microsoft.SqlTools.ServiceLayer.ModelManagement.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;

namespace Microsoft.SqlTools.ServiceLayer.ModelManagement
{
	public class ModelOperations
	{
		/// <summary>
		/// Returns models from given table 
		/// </summary>
		/// <param name="connection">Db connection</param>
		/// <param name="request">model request</param>
		/// <returns>Models</returns>
		public virtual List<ModelMetadata> GetModels(IDbConnection connection, ModelRequestBase request)
		{
			List<ModelMetadata> models = new List<ModelMetadata>();
			using (IDbCommand command = connection.CreateCommand())
			{
				command.CommandText = GetSelectModelsQuery(request.DatabaseName, request.TableName, request.SchemaName);

				using (IDataReader reader = command.ExecuteReader())
				{
					while (reader.Read())
					{
						models.Add(LoadModelMetadata(reader));
					}
				}
			}

			return models;
		}

		/// <summary>
		/// Downlaods model content into a temp file and returns the file path
		/// </summary>
		/// <param name="connection">Db connection</param>
		/// <param name="request">model request</param>
		/// <returns>Model file path</returns>
		public virtual string DownloadModel(IDbConnection connection, DownloadModelRequestParams request)
		{
			string fileName = Path.GetTempFileName();
			using (IDbCommand command = connection.CreateCommand())
			{
				Dictionary<string, object> parameters = new Dictionary<string, object>();
				command.CommandText = GetSelectModelContentQuery(request.DatabaseName, request.TableName, request.SchemaName, request.ModelId, parameters);

				foreach (var item in parameters)
				{
					var parameter = command.CreateParameter();
					parameter.ParameterName = item.Key;
					parameter.Value = item.Value;
					command.Parameters.Add(parameter);
				}
				using (IDataReader reader = command.ExecuteReader())
				{
					while (reader.Read())
					{
						File.WriteAllBytes(fileName, (byte[])reader[0]);
					}
				}
			}

			return fileName;
		}

		/// <summary>
		/// Import model to given table 
		/// </summary>
		/// <param name="connection">Db connection</param>
		/// <param name="request">model request</param>
		public virtual void ImportModel(IDbConnection connection, ImportModelRequestParams request)
		{
			WithDbChange(connection, request, (request) =>
			{
				Dictionary<string, object> parameters = new Dictionary<string, object>();

				using (IDbCommand command = connection.CreateCommand())
				{
					command.CommandText = GetInsertModelQuery(request.TableName, request.SchemaName, request.Model, parameters);

					foreach (var item in parameters)
					{
						var parameter = command.CreateParameter();
						parameter.ParameterName = item.Key;
						parameter.Value = item.Value;
						command.Parameters.Add(parameter);
					}
					command.ExecuteNonQuery();
					return true;
				}
			});
		}

		/// <summary>
		/// Updates model 
		/// </summary>
		/// <param name="connection">Db connection</param>
		/// <param name="request">model request</param>
		public virtual void UpdateModel(IDbConnection connection, UpdateModelRequestParams request)
		{
			WithDbChange(connection, request, (request) =>
			{
				Dictionary<string, object> parameters = new Dictionary<string, object>();
				using (IDbCommand command = connection.CreateCommand())
				{
					command.CommandText = GetUpdateModelQuery(request.TableName, request.SchemaName, request.Model, parameters);

					foreach (var item in parameters)
					{
						var parameter = command.CreateParameter();
						parameter.ParameterName = item.Key;
						parameter.Value = item.Value;
						command.Parameters.Add(parameter);
					}
					command.ExecuteNonQuery();
					return true;
				}
			});
		}

		/// <summary>
		/// Deletes a model from the given table 
		/// </summary>
		/// <param name="connection">Db connection</param>
		/// <param name="request">model request</param>
		public virtual void DeleteModel(IDbConnection connection, DeleteModelRequestParams request)
		{
			WithDbChange(connection, request, (request) =>
			{
				Dictionary<string, object> parameters = new Dictionary<string, object>();
				using (IDbCommand command = connection.CreateCommand())
				{
					command.CommandText = GetDeleteModelQuery(request.TableName, request.SchemaName, request.ModelId, parameters);

					foreach (var item in parameters)
					{
						var parameter = command.CreateParameter();
						parameter.ParameterName = item.Key;
						parameter.Value = item.Value;
						command.Parameters.Add(parameter);
					}
					command.ExecuteNonQuery();
					return true;
				}
			});
		}

		/// <summary>
		/// Configures model table
		/// </summary>
		/// <param name="connection">Db connection</param>
		/// <param name="request">model request</param>
		public virtual void ConfigureImportTable(IDbConnection connection, ModelRequestBase request)
		{
			WithDbChange(connection, request, (request) =>
			{
				Dictionary<string, object> parameters = new Dictionary<string, object>();
				using (IDbCommand command = connection.CreateCommand())
				{
					command.CommandText = GetCreateModelTableQuery(request.TableName, request.SchemaName);

					foreach (var item in parameters)
					{
						var parameter = command.CreateParameter();
						parameter.ParameterName = item.Key;
						parameter.Value = item.Value;
						command.Parameters.Add(parameter);
					}
					command.ExecuteNonQuery();
					return true;
				}
			});
		}

		/// <summary>
		/// Verifies model table 
		/// </summary>
		/// <param name="connection">Db connection</param>
		/// <param name="request">model request</param>
		public virtual bool VerifyImportTable(IDbConnection connection, ModelRequestBase request)
		{
			int result = WithDbChange(connection, request, (request) =>
			{
				Dictionary<string, object> parameters = new Dictionary<string, object>();
				using (IDbCommand command = connection.CreateCommand())
				{
					command.CommandText = GetConfigTableVerificationQuery(request.DatabaseName, request.TableName, request.SchemaName);

					command.ExecuteNonQuery();
					using (IDataReader reader = command.ExecuteReader())
					{
						while (reader.Read())
						{
							return reader.GetInt32(0);
						}
					}
					return 0;
				}
			});

			return result == 1;
		}

		private TResult WithDbChange<T, TResult>(IDbConnection connection, T request, Func<T, TResult> operation) where T : ModelRequestBase
		{
			string currentDb = connection.Database;
			if (connection.Database != request.DatabaseName)
			{
				connection.ChangeDatabase(request.DatabaseName);
			}
			TResult result = operation(request);

			if (connection.Database != currentDb)
			{
				connection.ChangeDatabase(currentDb);
			}
			return result;
		}

		private ModelMetadata LoadModelMetadata(IDataReader reader)
		{
			return new ModelMetadata
			{
				Id = reader.GetInt32(0),
				ModelName = reader.GetString(1),
				Description = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
				Version = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
				Created = reader.IsDBNull(4) ? string.Empty : reader.GetDateTime(4).ToString(),
				Framework = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
				FrameworkVersion = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
				DeploymentTime = reader.IsDBNull(7) ? string.Empty : reader.GetDateTime(7).ToString(),
				DeployedBy = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
				RunId = reader.IsDBNull(9) ? string.Empty : reader.GetString(9),
				ContentLength = reader.GetInt64(10),
			};
		}

		private const string ModelSelectColumns = @"
        SELECT model_id, model_name, model_description, model_version, model_creation_time, model_framework, model_framework_version, model_deployment_time, User_Name(deployed_by), run_id, 
        len(model)";

		private static string GetThreePartsTableName(string dbName, string tableName, string schemaName)
		{
			return $"[{CUtils.EscapeStringCBracket(dbName)}].[{CUtils.EscapeStringCBracket(schemaName)}].[{CUtils.EscapeStringCBracket(tableName)}]";
		}

		private static string GetTwoPartsTableName(string tableName, string schemaName)
		{
			return $"[{CUtils.EscapeStringCBracket(schemaName)}].[{CUtils.EscapeStringCBracket(tableName)}]";
		}

		private static string GetSelectModelsQuery(string dbName, string tableName, string schemaName)
		{
			return $@"
        {ModelSelectColumns}
        FROM {GetThreePartsTableName(dbName, tableName, schemaName)}
		WHERE model_name not like 'MLmodel' and model_name not like 'conda.yaml'
		ORDER BY model_id";
		}

		private static string GetConfigTableVerificationQuery(string dbName, string tableName, string schemaName)
		{
			string twoPartsTableName = GetTwoPartsTableName(CUtils.EscapeStringSQuote(tableName), CUtils.EscapeStringSQuote(schemaName));
			return $@"
		IF NOT EXISTS (
			SELECT name
				FROM sys.databases
				WHERE name = N'{CUtils.EscapeStringSQuote(dbName)}'
		)
		BEGIN
			SELECT 0
		END
		ELSE
		BEGIN
			USE [{CUtils.EscapeStringCBracket(dbName)}]
			IF EXISTS
				(  SELECT t.name, s.name
					FROM sys.tables t join sys.schemas s on t.schema_id=t.schema_id
					WHERE t.name = '{CUtils.EscapeStringSQuote(tableName)}'
					AND s.name = '{CUtils.EscapeStringSQuote(schemaName)}'
				)
			BEGIN
				IF EXISTS (SELECT * FROM syscolumns WHERE ID=OBJECT_ID('{twoPartsTableName}') AND NAME='model_name')
					AND EXISTS (SELECT * FROM syscolumns WHERE ID=OBJECT_ID('{twoPartsTableName}') AND NAME='model')
					AND EXISTS (SELECT * FROM syscolumns WHERE ID=OBJECT_ID('{twoPartsTableName}') AND NAME='model_id')
					AND EXISTS (SELECT * FROM syscolumns WHERE ID=OBJECT_ID('{twoPartsTableName}') AND NAME='model_description')
					AND EXISTS (SELECT * FROM syscolumns WHERE ID=OBJECT_ID('{twoPartsTableName}') AND NAME='model_framework')
					AND EXISTS (SELECT * FROM syscolumns WHERE ID=OBJECT_ID('{twoPartsTableName}') AND NAME='model_framework_version')
					AND EXISTS (SELECT * FROM syscolumns WHERE ID=OBJECT_ID('{twoPartsTableName}') AND NAME='model_version')
					AND EXISTS (SELECT * FROM syscolumns WHERE ID=OBJECT_ID('{twoPartsTableName}') AND NAME='model_creation_time')
					AND EXISTS (SELECT * FROM syscolumns WHERE ID=OBJECT_ID('{twoPartsTableName}') AND NAME='model_deployment_time')
					AND EXISTS (SELECT * FROM syscolumns WHERE ID=OBJECT_ID('{twoPartsTableName}') AND NAME='deployed_by')
					AND EXISTS (SELECT * FROM syscolumns WHERE ID=OBJECT_ID('{twoPartsTableName}') AND NAME='run_id')
				BEGIN
					SELECT 1
				END
				ELSE
				BEGIN
					SELECT 0
				END
			END
			ELSE
				SELECT 1
		END";
		}


		private static string GetCreateModelTableQuery(string tableName, string schemaName)
		{
			return $@"
		IF NOT EXISTS
			(  SELECT t.name, s.name
				FROM sys.tables t join sys.schemas s on t.schema_id=t.schema_id
				WHERE t.name = '{CUtils.EscapeStringSQuote(tableName)}'
				AND s.name = '{CUtils.EscapeStringSQuote(schemaName)}'
			)
		BEGIN
		CREATE TABLE {GetTwoPartsTableName(tableName, schemaName)} (
			[model_id] [int] IDENTITY(1,1) NOT NULL,
			[model_name] [varchar](256) NOT NULL,
			[model_framework] [varchar](256) NULL,
			[model_framework_version] [varchar](256) NULL,
			[model] [varbinary](max) NOT NULL,
			[model_version] [varchar](256) NULL,
			[model_creation_time] [datetime2] NULL,
			[model_deployment_time] [datetime2] NULL,
			[deployed_by] [int] NULL,
			[model_description] [varchar](256) NULL,
			[run_id] [varchar](256) NULL,
		CONSTRAINT [{CUtils.EscapeStringCBracket(tableName)}_models_pk] PRIMARY KEY CLUSTERED
		(
			[model_id] ASC
		)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
		) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
		ALTER TABLE {GetTwoPartsTableName(tableName, schemaName)} ADD  CONSTRAINT [{CUtils.EscapeStringCBracket(tableName)}_deployment_time]  DEFAULT (getdate()) FOR [model_deployment_time]
		END
";
		}

		private static string GetInsertModelQuery(string tableName, string schemaName, ModelMetadata model, Dictionary<string, object> parameters)
		{
			string twoPartsTableName = GetTwoPartsTableName(tableName, schemaName);

			return $@"
		INSERT INTO {twoPartsTableName}
		(model_name, model, model_version, model_description, model_creation_time, model_framework, model_framework_version, run_id, deployed_by)
		VALUES (
			{DatabaseUtils.AddStringParameterForInsert(model.ModelName ?? "")},
		  	{DatabaseUtils.AddByteArrayParameterForInsert("Content", model.FilePath ?? "", parameters)},
			{DatabaseUtils.AddStringParameterForInsert(model.Version ?? "")},
			{DatabaseUtils.AddStringParameterForInsert(model.Description ?? "")},
			{DatabaseUtils.AddStringParameterForInsert(model.Created)},
			{DatabaseUtils.AddStringParameterForInsert(model.Framework ?? "")},
			{DatabaseUtils.AddStringParameterForInsert(model.FrameworkVersion ?? "")},
			{DatabaseUtils.AddStringParameterForInsert(model.RunId ?? "")},
			USER_ID (Current_User)
		)
";
		}

		private static string GetUpdateModelQuery(string tableName, string schemaName, ModelMetadata model, Dictionary<string, object> parameters)
		{
			string twoPartsTableName = GetTwoPartsTableName(tableName, schemaName);
			parameters.Add(ModelIdParameterName, model.Id);

			return $@"
		UPDATE {twoPartsTableName}
		SET
			{DatabaseUtils.AddStringParameterForUpdate("model_name", model.ModelName ?? "")},
			{DatabaseUtils.AddStringParameterForUpdate("model_version", model.Version ?? "")},
			{DatabaseUtils.AddStringParameterForUpdate("model_description", model.Description ?? "")},
			{DatabaseUtils.AddStringParameterForUpdate("model_creation_time", model.Created)},
			{DatabaseUtils.AddStringParameterForUpdate("model_framework", model.Framework ?? "")},
			{DatabaseUtils.AddStringParameterForUpdate("model_framework_version", model.FrameworkVersion ?? "")},
			{DatabaseUtils.AddStringParameterForUpdate("run_id", model.RunId ?? "")}
		WHERE model_id = @{ModelIdParameterName}

";
		}

		private static string GetDeleteModelQuery(string tableName, string schemaName, int modelId, Dictionary<string, object> parameters)
		{
			string twoPartsTableName = GetTwoPartsTableName(tableName, schemaName);
			parameters.Add(ModelIdParameterName, modelId);

			return $@"
		DELETE FROM {twoPartsTableName}
		WHERE model_id = @{ModelIdParameterName}
";
		}

		private static string GetSelectModelContentQuery(string dbName, string tableName, string schemaName, int modelId, Dictionary<string, object> parameters)
		{
			string threePartsTableName = GetThreePartsTableName(dbName, tableName, schemaName);
			parameters.Add(ModelIdParameterName, modelId);

			return $@"
			SELECT model 
			FROM {threePartsTableName}
			WHERE model_id = @{ModelIdParameterName}
";
		}

		private const string ModelIdParameterName = "ModelId";
	}
}
