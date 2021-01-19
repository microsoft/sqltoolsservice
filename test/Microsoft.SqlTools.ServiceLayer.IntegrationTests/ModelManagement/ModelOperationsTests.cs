//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.Data.SqlClient;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.ModelManagement;
using Microsoft.SqlTools.ServiceLayer.ModelManagement.Contracts;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using NUnit.Framework;
using System;
using System.Data;
using System.IO;
using System.Linq;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.ModelManagement
{
    public class ModelOperationsTests
    {
        [Test]
        public void VerifyImportTableShouldReturnTrueGivenNoTable()
        {
            bool expected = true;
            bool actual = VerifyModelOperation((dbConnection, databaseName , tableName) =>
            {
                ModelOperations modelOperations = new ModelOperations();
                ModelRequestBase request = new ModelRequestBase
                {
                    DatabaseName = databaseName,
                    SchemaName = "dbo",
                    TableName = tableName
                };
                return modelOperations.VerifyImportTable(dbConnection, request);
            });

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void ImportModelShouldImportSuccessfullyGivenValidModel()
        {
            string modelFilePath = Path.GetTempFileName();
            File.WriteAllText(modelFilePath, "Test model");
            ModelMetadata expected = new ModelMetadata
            {
                FilePath = modelFilePath,
                Created = DateTime.Now.ToString(),
                Description = "model description",
                ModelName = "model name",
                RunId = "run id",
                Version = "1.2",
                Framework = "ONNX",
                FrameworkVersion = "1.3",
            };
            ModelMetadata actual = VerifyModelOperation((dbConnection, databaseName , tableName) =>
            {
                ModelOperations modelOperations = new ModelOperations();
                
                ImportModelRequestParams request = new ImportModelRequestParams
                {
                    DatabaseName = databaseName,
                    SchemaName = "dbo",
                    TableName = tableName,
                    Model = expected
                };
                modelOperations.ConfigureImportTable(dbConnection, request);
                modelOperations.ImportModel(dbConnection, request);
                var models = modelOperations.GetModels(dbConnection, request);
                return models.FirstOrDefault(x => x.ModelName == expected.ModelName);
            });

            Assert.IsNotNull(actual);
            Assert.AreEqual(expected.RunId, actual.RunId);
            Assert.AreEqual(expected.Description, actual.Description);
            Assert.AreEqual(expected.Framework, actual.Framework);
            Assert.AreEqual(expected.Version, actual.Version);
            Assert.AreEqual(expected.FrameworkVersion, actual.FrameworkVersion);
            Assert.IsFalse(string.IsNullOrWhiteSpace(actual.DeploymentTime));
            Assert.IsFalse(string.IsNullOrWhiteSpace(actual.DeployedBy));
        }

        [Test]
        public void DownloadModelShouldDownloadSuccessfullyGivenValidModel()
        {
            string expected = "Test model";

            string modelFilePath = Path.GetTempFileName();
            File.WriteAllText(modelFilePath, expected);
            ModelMetadata model = new ModelMetadata
            {
                FilePath = modelFilePath,
                Created = DateTime.Now.ToString(),
                Description = "model description",
                ModelName = "model name",
                RunId = "run id",
                Version = "1.2",
                Framework = "ONNX",
                FrameworkVersion = "1.3",
            };
            string actual = VerifyModelOperation((dbConnection, databaseName , tableName) =>
            {
                ModelOperations modelOperations = new ModelOperations();
                ImportModelRequestParams request = new ImportModelRequestParams
                {
                    DatabaseName = databaseName,
                    SchemaName = "dbo",
                    TableName = tableName,
                    Model = model
                };
                modelOperations.ConfigureImportTable(dbConnection, request);
                modelOperations.ImportModel(dbConnection, request);
                var models = modelOperations.GetModels(dbConnection, request);
                var importedModel = models.FirstOrDefault(x => x.ModelName == model.ModelName);
                Assert.IsNotNull(importedModel);
                DownloadModelRequestParams downloadRequest = new DownloadModelRequestParams
                {
                    DatabaseName = databaseName,
                    SchemaName = "dbo",
                    TableName = tableName,
                    ModelId = importedModel.Id
                };
                string downloadedFile = modelOperations.DownloadModel(dbConnection, downloadRequest);
                return File.ReadAllText(downloadedFile);

            });

            Assert.IsNotNull(actual);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void UpdateModelShouldUpdateSuccessfullyGivenValidModel()
        {
            string modelFilePath = Path.GetTempFileName();
            File.WriteAllText(modelFilePath, "Test");
            ModelMetadata model = new ModelMetadata
            {
                FilePath = modelFilePath,
                Created = DateTime.Now.ToString(),
                Description = "model description",
                ModelName = "model name",
                RunId = "run id",
                Version = "1.2",
                Framework = "ONNX",
                FrameworkVersion = "1.3",
            };
            ModelMetadata expected = model;

            ModelMetadata actual = VerifyModelOperation((dbConnection, databaseName , tableName) =>
            {
                ModelOperations modelOperations = new ModelOperations();
                ImportModelRequestParams request = new ImportModelRequestParams
                {
                    DatabaseName = databaseName,
                    SchemaName = "dbo",
                    TableName = tableName,
                    Model = model
                };
                modelOperations.ConfigureImportTable(dbConnection, request);
                modelOperations.ImportModel(dbConnection, request);
                var models = modelOperations.GetModels(dbConnection, request);
                var importedModel = models.FirstOrDefault(x => x.ModelName == model.ModelName);
                Assert.IsNotNull(importedModel);

                UpdateModelRequestParams updateRequest = new UpdateModelRequestParams
                {
                    DatabaseName = databaseName,
                    SchemaName = "dbo",
                    TableName = tableName,
                    Model = new ModelMetadata
                    {
                        Description = request.Model.Description + "updated",
                        ModelName = request.Model.ModelName + "updated",
                        Version = request.Model.Version + "updated",
                        Framework = request.Model.Framework + "updated",
                        RunId = request.Model.RunId + "updated",
                        FrameworkVersion = request.Model.FrameworkVersion + "updated",
                        Id = importedModel.Id
                    }
                };
                modelOperations.UpdateModel(dbConnection, updateRequest);
                models = modelOperations.GetModels(dbConnection, request);
                var updatedModel = models.FirstOrDefault(x => x.ModelName == updateRequest.Model.ModelName);
                Assert.IsNotNull(updatedModel);
                return updatedModel;
            });

            Assert.IsNotNull(actual);
            Assert.AreEqual(expected.Description + "updated", actual.Description);
            Assert.AreEqual(expected.ModelName + "updated", actual.ModelName);
            Assert.AreEqual(expected.Version + "updated", actual.Version);
            Assert.AreEqual(expected.Framework + "updated", actual.Framework);
            Assert.AreEqual(expected.FrameworkVersion + "updated", actual.FrameworkVersion);
            Assert.AreEqual(expected.RunId + "updated", actual.RunId);
        }

        [Test]
        public void DeleteModelShouldDeleteSuccessfullyGivenValidModel()
        {
            string modelFilePath = Path.GetTempFileName();
            File.WriteAllText(modelFilePath, "Test");
            ModelMetadata model = new ModelMetadata
            {
                FilePath = modelFilePath,
                Created = DateTime.Now.ToString(),
                Description = "model description",
                ModelName = "model name",
                RunId = "run id",
                Version = "1.2",
                Framework = "ONNX",
                FrameworkVersion = "1.3",
            };
            ModelMetadata expected = model;

            ModelMetadata actual = VerifyModelOperation((dbConnection, databaseName , tableName) =>
            {
                ModelOperations modelOperations = new ModelOperations();
                ImportModelRequestParams request = new ImportModelRequestParams
                {
                    DatabaseName = databaseName,
                    SchemaName = "dbo",
                    TableName = tableName,
                    Model = model
                };
                modelOperations.ConfigureImportTable(dbConnection, request);
                modelOperations.ImportModel(dbConnection, request);
                var models = modelOperations.GetModels(dbConnection, request);
                var importedModel = models.FirstOrDefault(x => x.ModelName == model.ModelName);
                Assert.IsNotNull(importedModel);

                DeleteModelRequestParams deleteRequest = new DeleteModelRequestParams
                {
                    DatabaseName = databaseName,
                    SchemaName = "dbo",
                    TableName = tableName,
                    ModelId = importedModel.Id
                };
                modelOperations.DeleteModel(dbConnection, deleteRequest);
                models = modelOperations.GetModels(dbConnection, request);
                var updatedModel = models.FirstOrDefault(x => x.ModelName == model.ModelName);
                return updatedModel;
            });

            Assert.IsNull(actual);
        }

        [Test]
        public void VerifyImportTableShouldReturnTrueGivenTableCreatdByTheService()
        {
            bool expected = true;
            bool actual = VerifyModelOperation((dbConnection, databaseName , tableName) =>
            {
                ModelOperations modelOperations = new ModelOperations();
                ModelRequestBase request = new ModelRequestBase
                {
                    DatabaseName = databaseName,
                    SchemaName = "dbo",
                    TableName = tableName
                };
                modelOperations.ConfigureImportTable(dbConnection, request);
                return modelOperations.VerifyImportTable(dbConnection, request);
            });

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void VerifyImportTableShouldReturnTrueGivenTableNameThatNeedsEscaping()
        {
            bool expected = true;
            bool actual = VerifyModelOperation((dbConnection, databaseName , tableName) =>
            {
                ModelOperations modelOperations = new ModelOperations();
                ModelRequestBase request = new ModelRequestBase
                {
                    DatabaseName = databaseName,
                    SchemaName = "dbo",
                    TableName = tableName
                };
                modelOperations.ConfigureImportTable(dbConnection, request);
                return modelOperations.VerifyImportTable(dbConnection, request);
            }, null, "models[]'");

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void VerifyImportTableShouldReturnFalseGivenInvalidDbName()
        {
            Assert.Throws(typeof(SqlException), () =>  VerifyModelOperation((dbConnection, databaseName , tableName) =>
            {
                ModelOperations modelOperations = new ModelOperations();
                ModelRequestBase request = new ModelRequestBase
                {
                    DatabaseName = "invalidDb",
                    SchemaName = "dbo",
                    TableName = tableName
                };
                modelOperations.ConfigureImportTable(dbConnection, request);
                return modelOperations.VerifyImportTable(dbConnection, request);
            }));
        }

        [Test]
        public void VerifyImportTableShouldReturnFalseGivenInvalidTable()
        {
            bool expected = false;
            bool actual = VerifyModelOperation((dbConnection, databaseName , tableName) =>
            {
                dbConnection.ChangeDatabase(databaseName);
                using (IDbCommand command = dbConnection.CreateCommand())
                {
                    command.CommandText = $"Create Table {tableName} (Id int, name varchar(10))";
                    command.ExecuteNonQuery();
                }
                dbConnection.ChangeDatabase("master");

                ModelOperations modelOperations = new ModelOperations();
                ModelRequestBase request = new ModelRequestBase
                {
                    DatabaseName = databaseName,
                    SchemaName = "dbo",
                    TableName = tableName
                };
                return modelOperations.VerifyImportTable(dbConnection, request);
            });

            Assert.AreEqual(expected, actual);
        }

        private T VerifyModelOperation<T>(Func<IDbConnection, string, string, T> operation, string dbName = null, string tbName = null)
        {
            string databaseName = dbName ?? "testModels_" + new Random().Next(10000000, 99999999);
            string tableName = tbName ?? "models";
            using (SqlTestDb testDb = SqlTestDb.CreateNew(TestServerType.OnPrem, false, databaseName))
            {
                var liveConnection = LiveConnectionHelper.InitLiveConnectionInfo(databaseName);
                ConnectionInfo connInfo = liveConnection.ConnectionInfo;
                IDbConnection dbConnection = ConnectionService.OpenSqlConnection(connInfo);
                dbConnection.ChangeDatabase("master");

                T result = operation(dbConnection, databaseName, tableName);
                Assert.AreEqual(dbConnection.Database, "master");
                return result;
            }
        }
    }
}
