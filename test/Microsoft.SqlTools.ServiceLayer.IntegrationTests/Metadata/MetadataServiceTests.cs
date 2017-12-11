//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.Metadata;
using Microsoft.SqlTools.ServiceLayer.Metadata.Contracts;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Moq;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
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

        private void CreateTestTable(SqlConnection sqlConn)
        {
            string sql = string.Format("IF OBJECT_ID('{0}.{1}', 'U') IS NULL CREATE TABLE {0}.{1}(id int)",
                this.testTableSchema, this.testTableName);
            using (var sqlCommand = new SqlCommand(sql, sqlConn))
            {
                sqlCommand.ExecuteNonQuery(); 
            }            
        }

        private void DeleteTestTable(SqlConnection sqlConn)
        {
            string sql = string.Format("IF OBJECT_ID('{0}.{1}', 'U') IS NOT NULL DROP TABLE {0}.{1}",
                this.testTableSchema, this.testTableName);
            using (var sqlCommand = new SqlCommand(sql, sqlConn))
            {
                sqlCommand.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Verify that the metadata service correctly returns details for user tables
        /// </summary>
        [Fact]
        public void MetadataReturnsUserTable()
        {
            this.testTableName += new Random().Next(1000000, 9999999).ToString();

            var result = GetLiveAutoCompleteTestObjects();
            var sqlConn = ConnectionService.OpenSqlConnection(result.ConnectionInfo);
            Assert.NotNull(sqlConn);

            CreateTestTable(sqlConn);

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

            DeleteTestTable(sqlConn);
        }

        [Fact]
        public async void GetTableInfoReturnsValidResults()
        {
            this.testTableName += new Random().Next(1000000, 9999999).ToString();
                   
            var result = GetLiveAutoCompleteTestObjects();
            var sqlConn = ConnectionService.OpenSqlConnection(result.ConnectionInfo);

            CreateTestTable(sqlConn);

            var requestContext = new Mock<RequestContext<TableMetadataResult>>();
            requestContext.Setup(x => x.SendResult(It.IsAny<TableMetadataResult>())).Returns(Task.FromResult(new object()));

            var metadataParmas = new TableMetadataParams
            {
                OwnerUri = result.ConnectionInfo.OwnerUri,
                Schema = this.testTableSchema,
                ObjectName = this.testTableName
            };

            await MetadataService.HandleGetTableRequest(metadataParmas, requestContext.Object);

            DeleteTestTable(sqlConn);

            requestContext.VerifyAll();
        }

        [Fact]
        public async void GetViewInfoReturnsValidResults()
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

        [Fact]
        public async void VerifyMetadataList()
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

            await VerifyMetadataList(query, expectedMetadataList);
        }

        private async Task VerifyMetadataList(string query, List<ObjectMetadata> expectedMetadataList)
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

            if(expectedMetadataList.Count() != result.Metadata.Count())
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
