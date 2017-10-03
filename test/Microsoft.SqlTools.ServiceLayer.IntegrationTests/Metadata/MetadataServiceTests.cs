//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.Metadata;
using Microsoft.SqlTools.ServiceLayer.Metadata.Contracts;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Moq;
using Xunit;

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
            var sqlConn = LiveConnectionHelper.GetLiveTestConnectionService().OpenSqlConnection(result.ConnectionInfo);
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
            var sqlConn = LiveConnectionHelper.GetLiveTestConnectionService().OpenSqlConnection(result.ConnectionInfo);

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

    }
}
