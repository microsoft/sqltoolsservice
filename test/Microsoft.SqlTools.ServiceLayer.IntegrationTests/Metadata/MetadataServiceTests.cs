//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.Metadata;
using Microsoft.SqlTools.ServiceLayer.Metadata.Contracts;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Microsoft.SqlTools.SqlCore.Metadata;
using Moq;
using NUnit.Framework;
using static Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility.LiveConnectionHelper;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.Metadata
{
    /// <summary>
    /// Tests for the Metadata service component
    /// </summary>
    public class MetadataServiceTests
    {
        private string testTableSchema = "dbo";
        private string testTableName = "MetadataTestTable";
        private string testTableName2 = "SecondMetadataTestTable";

        private LiveConnectionHelper.TestConnectionResult GetLiveAutoCompleteTestObjects()
        {
            var textDocument = new TextDocumentPosition
            {
                TextDocument = new TextDocumentIdentifier { Uri = Test.Common.Constants.OwnerUri },
                Position = new Position
                {
                    Line = 0,
                    Character = 0
                }
            };

            var result = LiveConnectionHelper.InitLiveConnectionInfo();
            result.TextDocumentPosition = textDocument;
            return result;
        }

        private void CreateTestTable(SqlConnection sqlConn, string testTableSchema, string testTableName)
        {
            string sql = string.Format("IF OBJECT_ID('{0}.{1}', 'U') IS NULL CREATE TABLE {0}.{1}(id int)",
                testTableSchema, testTableName);
            using (var sqlCommand = new SqlCommand(sql, sqlConn))
            {
                sqlCommand.ExecuteNonQuery();
            }
        }

        private void DeleteTestTable(SqlConnection sqlConn, string testTableSchema, string testTableName)
        {
            string sql = string.Format("IF OBJECT_ID('{0}.{1}', 'U') IS NOT NULL DROP TABLE {0}.{1}",
                testTableSchema, testTableName);
            using (var sqlCommand = new SqlCommand(sql, sqlConn))
            {
                sqlCommand.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Verify that the metadata service correctly returns details for user tables
        /// </summary>
        [Test]
        public void MetadataReturnsUserTable()
        {
            this.testTableName += new Random().Next(1000000, 9999999).ToString();

            var result = GetLiveAutoCompleteTestObjects();
            var sqlConn = ConnectionService.OpenSqlConnection(result.ConnectionInfo);
            Assert.NotNull(sqlConn);

            CreateTestTable(sqlConn, this.testTableSchema, this.testTableName);

            var metadata = new List<ObjectMetadata>();
            MetadataService.ReadMetadata(sqlConn, metadata);
            Assert.NotNull(metadata.Count > 0);

            bool foundTestTable = false;
            foreach (var item in metadata)
            {
                if (string.Equals(item.Schema, this.testTableSchema, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(item.Name, this.testTableName, StringComparison.OrdinalIgnoreCase))
                {
                    foundTestTable = true;
                    break;
                }
            }
            Assert.True(foundTestTable);

            DeleteTestTable(sqlConn, this.testTableSchema, this.testTableName);
        }

        [Test]
        public async Task GetTableInfoReturnsValidResults()
        {
            this.testTableName += new Random().Next(1000000, 9999999).ToString();

            var result = GetLiveAutoCompleteTestObjects();
            var sqlConn = ConnectionService.OpenSqlConnection(result.ConnectionInfo);

            CreateTestTable(sqlConn, this.testTableSchema, this.testTableName);

            var requestContext = new Mock<RequestContext<TableMetadataResult>>();
            requestContext.Setup(x => x.SendResult(It.IsAny<TableMetadataResult>())).Returns(Task.FromResult(new object()));

            var metadataParmas = new TableMetadataParams
            {
                OwnerUri = result.ConnectionInfo.OwnerUri,
                Schema = this.testTableSchema,
                ObjectName = this.testTableName
            };

            await MetadataService.HandleGetTableRequest(metadataParmas, requestContext.Object);

            DeleteTestTable(sqlConn, this.testTableSchema, this.testTableName);

            requestContext.VerifyAll();
        }

        [Test]
        public async Task VerifyGetServerContextualizationRequest()
        {
            this.testTableName += new Random().Next(1000000, 9999999).ToString();
            this.testTableName2 += new Random().Next(1000000, 9999999).ToString();

            var connectionResult = LiveConnectionHelper.InitLiveConnectionInfo(null);
            var sqlConn = ConnectionService.OpenSqlConnection(connectionResult.ConnectionInfo);

            CreateTestTable(sqlConn, this.testTableSchema, this.testTableName);
            CreateTestTable(sqlConn, this.testTableSchema, this.testTableName2);

            var firstCreateTableScript = $"CREATE TABLE [{this.testTableSchema}].[{this.testTableName}]([id] [int] NULL)";
            var secondCreateTableScript = $"CREATE TABLE [{this.testTableSchema}].[{this.testTableName2}]([id] [int] NULL)";

            var mockGetServerContextualizationRequestContext = new Mock<RequestContext<GetServerContextualizationResult>>();
            var actualGetServerContextualizationResponse = new GetServerContextualizationResult();
            mockGetServerContextualizationRequestContext.Setup(x => x.SendResult(It.IsAny<GetServerContextualizationResult>()))
                .Callback<GetServerContextualizationResult>(actual => actualGetServerContextualizationResponse = actual)
                .Returns(Task.CompletedTask);

            var getServerContextualizationParams = new GetServerContextualizationParams
            {
                OwnerUri = connectionResult.ConnectionInfo.OwnerUri
            };

            // First call generates context, stores it in temp file and returns the generated context
            await MetadataService.GetServerContextualization(getServerContextualizationParams, mockGetServerContextualizationRequestContext.Object);

            Assert.IsTrue(actualGetServerContextualizationResponse.Context.Contains(firstCreateTableScript));
            Assert.IsTrue(actualGetServerContextualizationResponse.Context.Contains(secondCreateTableScript));

            // Second call gets the context from the temp file and returns that read file context.
            await MetadataService.GetServerContextualization(getServerContextualizationParams, mockGetServerContextualizationRequestContext.Object);

            DeleteTestTable(sqlConn, this.testTableSchema, this.testTableName);
            DeleteTestTable(sqlConn, this.testTableSchema, this.testTableName2);

            Assert.IsTrue(actualGetServerContextualizationResponse.Context.Contains(firstCreateTableScript));
            Assert.IsTrue(actualGetServerContextualizationResponse.Context.Contains(secondCreateTableScript));

            DeleteServerContextualizationTempFile(sqlConn.DataSource);

            mockGetServerContextualizationRequestContext.VerifyAll();
        }

        private void DeleteServerContextualizationTempFile(string serverName)
        {
            var bytes = Encoding.UTF8.GetBytes(serverName);
            var encodedServerName = Convert.ToBase64String(bytes);
            var tempFileName = $"{encodedServerName}.tmp";

            var tempFilePath = Path.Combine(Path.GetTempPath(), tempFileName);
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }

        [Test]
        public async Task GetViewInfoReturnsValidResults()
        {
            var result = GetLiveAutoCompleteTestObjects();
            var requestContext = new Mock<RequestContext<TableMetadataResult>>();
            requestContext.Setup(x => x.SendResult(It.IsAny<TableMetadataResult>())).Returns(Task.FromResult(new object()));

            var metadataParmas = new TableMetadataParams
            {
                OwnerUri = result.ConnectionInfo.OwnerUri,
                Schema = "sys",
                ObjectName = "all_objects"
            };

            await MetadataService.HandleGetViewRequest(metadataParmas, requestContext.Object);

            requestContext.VerifyAll();
        }

        [Test]
        public async Task VerifyMetadataList()
        {
            string query = @"CREATE TABLE testTable1 (c1 int)
                            GO
                            CREATE PROCEDURE testSp1 @StartProductID [int] AS  BEGIN Select * from sys.all_columns END
                            GO
                            CREATE VIEW testView1 AS SELECT * from sys.all_columns
                            GO
                            CREATE FUNCTION testFun1() RETURNS [int] AS BEGIN RETURN 1 END
                            GO
                            CREATE FUNCTION [testFun2](@CityID int)
                            RETURNS TABLE
                            WITH SCHEMABINDING
                            AS
                            RETURN SELECT 1 AS AccessResult
                            GO";

            List<ObjectMetadata> expectedMetadataList = new List<ObjectMetadata>
            {
                new ObjectMetadata
                {
                    MetadataType = MetadataType.Table,
                    MetadataTypeName = "Table",
                    Name = "testTable1",
                    Schema = "dbo"
                },
                new ObjectMetadata
                {
                    MetadataType = MetadataType.SProc,
                    MetadataTypeName = "StoredProcedure",
                    Name = "testSp1",
                    Schema = "dbo"
                },
                new ObjectMetadata
                {
                    MetadataType = MetadataType.View,
                    MetadataTypeName = "View",
                    Name = "testView1",
                    Schema = "dbo"
                },
                new ObjectMetadata
                {
                    MetadataType = MetadataType.Function,
                    MetadataTypeName = "UserDefinedFunction",
                    Name = "testFun1",
                    Schema = "dbo"
                },
                 new ObjectMetadata
                {
                    MetadataType = MetadataType.Function,
                    MetadataTypeName = "UserDefinedFunction",
                    Name = "testFun2",
                    Schema = "dbo"
                }
            };

            await VerifyMetadataListCore(query, expectedMetadataList);
        }

        private async Task VerifyMetadataListCore(string query, List<ObjectMetadata> expectedMetadataList)
        {
            var testDb = await SqlTestDb.CreateNewAsync(TestServerType.OnPrem, false, null, query, "MetadataTests");
            try
            {
                var requestContext = new Mock<RequestContext<MetadataQueryResult>>();
                requestContext.Setup(x => x.SendResult(It.IsAny<MetadataQueryResult>())).Returns(Task.FromResult(new object()));
                ConnectionService connectionService = LiveConnectionHelper.GetLiveTestConnectionService();
                using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
                {
                    //Opening a connection to db to lock the db
                    TestConnectionResult connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync(testDb.DatabaseName, queryTempFile.FilePath, ConnectionType.Default);

                    MetadataService service = new MetadataService();
                    await service.HandleMetadataListRequest(new MetadataQueryParams
                    {
                        OwnerUri = queryTempFile.FilePath
                    }, requestContext.Object);
                    Thread.Sleep(2000);
                    await service.MetadataListTask;

                    requestContext.Verify(x => x.SendResult(It.Is<MetadataQueryResult>(r => VerifyResult(r, expectedMetadataList))));
                    connectionService.Disconnect(new ServiceLayer.Connection.Contracts.DisconnectParams
                    {
                        OwnerUri = queryTempFile.FilePath
                    });
                }
            }
            catch
            {
                throw;
            }
            finally
            {
                await testDb.CleanupAsync();
            }
        }

        private static bool VerifyResult(MetadataQueryResult result, List<ObjectMetadata> expectedMetadataList)
        {
            if (expectedMetadataList == null)
            {
                return result.Metadata == null;
            }

            if (expectedMetadataList.Count != result.Metadata.Length)
            {
                return false;
            }
            foreach (ObjectMetadata expected in expectedMetadataList)
            {
                if (!result.Metadata.Any(x => x.MetadataType == expected.MetadataType && x.MetadataTypeName == expected.MetadataTypeName && x.Name == expected.Name && x.Schema == expected.Schema))
                {
                    return false;
                }
            }
            return true;
        }

    }
}
