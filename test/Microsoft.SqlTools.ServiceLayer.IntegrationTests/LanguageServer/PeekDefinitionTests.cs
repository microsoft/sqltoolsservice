//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using System;
using System.IO;
using System.Threading;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.LanguageServices;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Microsoft.SqlTools.Test.Utility;
using Moq;
using Xunit;
using Location = Microsoft.SqlTools.ServiceLayer.Workspace.Contracts.Location;
using Microsoft.SqlServer.Management.SqlParser.Intellisense;
using Microsoft.SqlTools.ServiceLayer.Test.Common;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.LanguageServices
{
    /// <summary>
    /// Tests for the language service peek definition/ go to definition feature
    /// </summary>
    public class PeekDefinitionTests
    {
        private const string OwnerUri = "testFile1";

        /// <summary>
        /// Test get definition for a table object with active connection
        /// </summary>
        [Fact]
        public void GetValidTableDefinitionTest()
        {
            // Get live connectionInfo and serverConnection
            ConnectionInfo connInfo = TestObjects.InitLiveConnectionInfoForDefinition();
            ServerConnection serverConnection = TestObjects.InitLiveServerConnectionForDefinition(connInfo);

            PeekDefinition peekDefinition = new PeekDefinition(serverConnection, connInfo);
            string objectName = "spt_monitor";

            string schemaName = null;
            string objectType = "TABLE";

            // Get locations for valid table object
            Location[] locations = peekDefinition.GetSqlObjectDefinition(peekDefinition.GetTableScripts, objectName, schemaName, objectType);
            Assert.NotNull(locations);
            Cleanup(locations);
        }

        /// <summary>
        /// Test get definition for a invalid table object with active connection
        /// </summary>
        [Fact]
        public void GetTableDefinitionInvalidObjectTest()
        {
            // Get live connectionInfo and serverConnection
            ConnectionInfo connInfo = TestObjects.InitLiveConnectionInfoForDefinition();
            ServerConnection serverConnection = TestObjects.InitLiveServerConnectionForDefinition(connInfo);

            PeekDefinition peekDefinition = new PeekDefinition(serverConnection, connInfo);
            string objectName = "test_invalid";
            string schemaName = null;
            string objectType = "TABLE";

            // Get locations for invalid table object
            Location[] locations = peekDefinition.GetSqlObjectDefinition(peekDefinition.GetTableScripts, objectName, schemaName, objectType);
            Assert.Null(locations);
        }

        /// <summary>
        /// Test get definition for a valid table object with schema and active connection
        /// </summary>
        [Fact]
        public void GetTableDefinitionWithSchemaTest()
        {
            // Get live connectionInfo and serverConnection
            ConnectionInfo connInfo = TestObjects.InitLiveConnectionInfoForDefinition();
            ServerConnection serverConnection = TestObjects.InitLiveServerConnectionForDefinition(connInfo);

            PeekDefinition peekDefinition = new PeekDefinition(serverConnection, connInfo);
            string objectName = "spt_monitor";

            string schemaName = "dbo";
            string objectType = "TABLE";

            // Get locations for valid table object with schema name
            Location[] locations = peekDefinition.GetSqlObjectDefinition(peekDefinition.GetTableScripts, objectName, schemaName, objectType);
            Assert.NotNull(locations);
            Cleanup(locations);
        }

        /// <summary>
        /// Test GetDefinition with an unsupported type(schema - dbo). Expect a error result.
        /// </summary>
        [Fact]
        public void GetUnsupportedDefinitionErrorTest()
        {
            TextDocumentPosition textDocument = new TextDocumentPosition
            {
                TextDocument = new TextDocumentIdentifier { Uri = OwnerUri },
                Position = new Position
                {
                    Line = 0,
                    // test for 'dbo'
                    Character = 15
                }
            };

            TestConnectionResult connectionResult = TestObjects.InitLiveConnectionInfo();
            connectionResult.ScriptFile.Contents = "select * from dbo.func ()";
            var languageService = new LanguageService();
            ScriptParseInfo scriptInfo = new ScriptParseInfo { IsConnected = true };
            languageService.ScriptParseInfoMap.Add(OwnerUri, scriptInfo);

            // When I call the language service
            var result = languageService.GetDefinition(textDocument, connectionResult.ScriptFile, connectionResult.ConnectionInfo);

            // Then I expect null locations and an error to be reported
            Assert.NotNull(result);
            Assert.True(result.IsErrorResult);
        }

        /// <summary>
        /// Get Definition for a object with no definition. Expect a error result
        /// </summary>
        [Fact]
        public void GetDefinitionWithNoResultsFoundError()
        {
            ConnectionInfo connInfo = TestObjects.InitLiveConnectionInfoForDefinition();
            ServerConnection serverConnection = TestObjects.InitLiveServerConnectionForDefinition(connInfo);

            PeekDefinition peekDefinition = new PeekDefinition(serverConnection, connInfo);
            string objectName = "from";
            Position position = new Position()
            {
                Line = 1,
                Character = 14
            };
            ScriptParseInfo scriptParseInfo = new ScriptParseInfo() { IsConnected = true };
            Mock<IBindingContext> bindingContextMock = new Mock<IBindingContext>();
            DefinitionResult result = peekDefinition.GetScript(scriptParseInfo.ParseResult, position, bindingContextMock.Object.MetadataDisplayInfoProvider, objectName, null);

            Assert.NotNull(result);
            Assert.True(result.IsErrorResult);
            Assert.Equal(SR.PeekDefinitionNoResultsError, result.Message);
        }

        /// <summary>
        /// Test GetDefinition with a forced timeout. Expect a error result.
        /// </summary>
        [Fact]
        public void GetDefinitionTimeoutTest()
        {
            // Given a binding queue that will automatically time out
            var languageService = new LanguageService();
            Mock<ConnectedBindingQueue> queueMock = new Mock<ConnectedBindingQueue>();
            languageService.BindingQueue = queueMock.Object;
            ManualResetEvent mre = new ManualResetEvent(true); // Do not block
            Mock<QueueItem> itemMock = new Mock<QueueItem>();
            itemMock.Setup(i => i.ItemProcessed).Returns(mre);

            DefinitionResult timeoutResult = null;

            queueMock.Setup(q => q.QueueBindingOperation(
                It.IsAny<string>(),
                It.IsAny<Func<IBindingContext, CancellationToken, object>>(),
                It.IsAny<Func<IBindingContext, object>>(),
                It.IsAny<int?>(),
                It.IsAny<int?>()))
            .Callback<string, Func<IBindingContext, CancellationToken, object>, Func<IBindingContext, object>, int?, int?>(
                (key, bindOperation, timeoutOperation, t1, t2) =>
            {
                timeoutResult = (DefinitionResult) timeoutOperation((IBindingContext)null);
                itemMock.Object.Result = timeoutResult;
            })
            .Returns(() => itemMock.Object);

            TextDocumentPosition textDocument = new TextDocumentPosition
            {
                TextDocument = new TextDocumentIdentifier { Uri = OwnerUri },
                Position = new Position
                {
                    Line = 0,
                    Character = 20
                }
            };
            TestConnectionResult connectionResult = TestObjects.InitLiveConnectionInfo();
            ScriptFile scriptFile = connectionResult.ScriptFile;
            ConnectionInfo connInfo = connectionResult.ConnectionInfo;
            scriptFile.Contents = "select * from dbo.func ()";

            ScriptParseInfo scriptInfo = new ScriptParseInfo { IsConnected = true };
            languageService.ScriptParseInfoMap.Add(OwnerUri, scriptInfo);

            // When I call the language service
            var result = languageService.GetDefinition(textDocument, scriptFile, connInfo);

            // Then I expect null locations and an error to be reported
            Assert.NotNull(result);
            Assert.True(result.IsErrorResult);
            // Check timeout message
            Assert.Equal(SR.PeekDefinitionTimedoutError, result.Message);
        }

        /// <summary>
        /// Test get definition for a view object with active connection
        /// </summary>
        [Fact]
        public void GetValidViewDefinitionTest()
        {
            ConnectionInfo connInfo = TestObjects.InitLiveConnectionInfoForDefinition();
            ServerConnection serverConnection = TestObjects.InitLiveServerConnectionForDefinition(connInfo);

            PeekDefinition peekDefinition = new PeekDefinition(serverConnection, connInfo);
            string objectName = "objects";
            string schemaName = "sys";
            string objectType = "VIEW";

            Location[] locations = peekDefinition.GetSqlObjectDefinition(peekDefinition.GetViewScripts, objectName, schemaName, objectType);
            Assert.NotNull(locations);
            Cleanup(locations);
        }

        /// <summary>
        /// Test get definition for an invalid view object with no schema name and with active connection
        /// </summary>
        [Fact]
        public void GetViewDefinitionInvalidObjectTest()
        {
            // Get live connectionInfo and serverConnection
            ConnectionInfo connInfo = TestObjects.InitLiveConnectionInfoForDefinition();
            ServerConnection serverConnection = TestObjects.InitLiveServerConnectionForDefinition(connInfo);

            PeekDefinition peekDefinition = new PeekDefinition(serverConnection, connInfo);
            string objectName = "objects";
            string schemaName = null;
            string objectType = "VIEW";

            Location[] locations = peekDefinition.GetSqlObjectDefinition(peekDefinition.GetViewScripts, objectName, schemaName, objectType);
            Assert.Null(locations);
        }

        /// <summary>
        /// Test get definition for a stored procedure object with active connection
        /// </summary>
        [Fact]
        public void GetStoredProcedureDefinitionTest()
        {
            // Get live connectionInfo and serverConnection
            ConnectionInfo connInfo = TestObjects.InitLiveConnectionInfoForDefinition();
            ServerConnection serverConnection = TestObjects.InitLiveServerConnectionForDefinition(connInfo);

            PeekDefinition peekDefinition = new PeekDefinition(serverConnection, connInfo);
            string objectName = "sp_MSrepl_startup";

            string schemaName = "dbo";
            string objectType = "PROCEDURE";

            Location[] locations = peekDefinition.GetSqlObjectDefinition(peekDefinition.GetStoredProcedureScripts, objectName, schemaName, objectType);
            Assert.NotNull(locations);
            Cleanup(locations);
        }

        /// <summary>
        /// Test get definition for a stored procedure object that does not exist with active connection
        /// </summary>
        [Fact]
        public void GetStoredProcedureDefinitionFailureTest()
        {
            // Get live connectionInfo and serverConnection
            ConnectionInfo connInfo = TestObjects.InitLiveConnectionInfoForDefinition();
            ServerConnection serverConnection = TestObjects.InitLiveServerConnectionForDefinition(connInfo);

            PeekDefinition peekDefinition = new PeekDefinition(serverConnection, connInfo);
            string objectName = "SP2";
            string schemaName = "dbo";
            string objectType = "PROCEDURE";

            Location[] locations = peekDefinition.GetSqlObjectDefinition(peekDefinition.GetStoredProcedureScripts, objectName, schemaName, objectType);
            Assert.Null(locations);
        }

        /// <summary>
        /// Test get definition for a stored procedure object with active connection and no schema
        /// </summary>
        [Fact]
        public void GetStoredProcedureDefinitionWithoutSchemaTest()
        {
            // Get live connectionInfo and serverConnection
            ConnectionInfo connInfo = TestObjects.InitLiveConnectionInfoForDefinition();
            ServerConnection serverConnection = TestObjects.InitLiveServerConnectionForDefinition(connInfo);

            PeekDefinition peekDefinition = new PeekDefinition(serverConnection, connInfo);
            string objectName = "sp_MSrepl_startup";
            string schemaName = null;
            string objectType = "PROCEDURE";

            Location[] locations = peekDefinition.GetSqlObjectDefinition(peekDefinition.GetStoredProcedureScripts, objectName, schemaName, objectType);
            Assert.NotNull(locations);
            Cleanup(locations);
        }

        /// <summary>
        /// Test get definition for a scalar valued function object with active connection and explicit schema name. Expect non-null locations
        /// </summary>
        [Fact]
        public void GetScalarValuedFunctionDefinitionWithSchemaNameSuccessTest()
        {
            string query = @"
CREATE FUNCTION[dbo].[pd_addTwo](@number int)  
RETURNS int
AS   
BEGIN
    RETURN @number + 2;
        END;  

GO";

            using (SqlTestDb testDb = SqlTestDb.CreateNew(TestServerType.OnPrem, query))
            {
                // Get live connectionInfo and serverConnection
                ConnectionInfo connInfo = TestObjects.InitLiveConnectionInfoForDefinition(testDb.DatabaseName);
                ServerConnection serverConnection = TestObjects.InitLiveServerConnectionForDefinition(connInfo);

                PeekDefinition peekDefinition = new PeekDefinition(serverConnection, connInfo);
                string objectName = "pd_addTwo";
                string schemaName = "dbo";
                string objectType = "FUNCTION";

                Location[] locations = peekDefinition.GetSqlObjectDefinition(peekDefinition.GetScalarValuedFunctionScripts, objectName, schemaName, objectType);
                Assert.NotNull(locations);
                Cleanup(locations);
            }
        }

        /// <summary>
        /// Test get definition for a table valued function object with active connection and explicit schema name. Expect non-null locations
        /// </summary>
        [Fact]
        public void GetTableValuedFunctionDefinitionWithSchemaNameSuccessTest()
        {
            string query = @"
CREATE FUNCTION [dbo].[pd_returnTable] ()  
RETURNS TABLE  
AS  
RETURN   
(  
    select * from master.dbo.spt_monitor 
);  

GO";

            using (SqlTestDb testDb = SqlTestDb.CreateNew(TestServerType.OnPrem, query))
            {
                // Get live connectionInfo and serverConnection
                ConnectionInfo connInfo = TestObjects.InitLiveConnectionInfoForDefinition(testDb.DatabaseName);
                ServerConnection serverConnection = TestObjects.InitLiveServerConnectionForDefinition(connInfo);

                PeekDefinition peekDefinition = new PeekDefinition(serverConnection, connInfo);
                string objectName = "pd_returnTable";
                string schemaName = "dbo";
                string objectType = "FUNCTION";

                Location[] locations = peekDefinition.GetSqlObjectDefinition(peekDefinition.GetTableValuedFunctionScripts, objectName, schemaName, objectType);
                Assert.NotNull(locations);
                Cleanup(locations);
            }
        }

        /// <summary>
        /// Test get definition for a scalar valued function object that doesn't exist with active connection. Expect null locations
        /// </summary>
        [Fact]
        public void GetScalarValuedFunctionDefinitionWithNonExistentFailureTest()
        {
            // Get live connectionInfo and serverConnection
            ConnectionInfo connInfo = TestObjects.InitLiveConnectionInfoForDefinition();
            ServerConnection serverConnection = TestObjects.InitLiveServerConnectionForDefinition(connInfo);

            PeekDefinition peekDefinition = new PeekDefinition(serverConnection, connInfo);
            string objectName = "doesNotExist";
            string schemaName = "dbo";
            string objectType = "FUNCTION";

            Location[] locations = peekDefinition.GetSqlObjectDefinition(peekDefinition.GetScalarValuedFunctionScripts, objectName, schemaName, objectType);
            Assert.Null(locations);
        }

        /// <summary>
        /// Test get definition for a table valued function object that doesn't exist with active connection. Expect null locations
        /// </summary>
        [Fact]
        public void GetTableValuedFunctionDefinitionWithNonExistentObjectFailureTest()
        {
            // Get live connectionInfo and serverConnection
            ConnectionInfo connInfo = TestObjects.InitLiveConnectionInfoForDefinition();
            ServerConnection serverConnection = TestObjects.InitLiveServerConnectionForDefinition(connInfo);

            PeekDefinition peekDefinition = new PeekDefinition(serverConnection, connInfo);
            string objectName = "doesNotExist";
            string schemaName = "dbo";
            string objectType = "FUNCTION";

            Location[] locations = peekDefinition.GetSqlObjectDefinition(peekDefinition.GetTableValuedFunctionScripts, objectName, schemaName, objectType);
            Assert.Null(locations);
        }

        /// <summary>
        /// Test get definition for a scalar valued function object with active connection. Expect non-null locations
        /// </summary>
        [Fact]
        public void GetScalarValuedFunctionDefinitionWithoutSchemaNameSuccessTest()
        {
            string query = @"
CREATE FUNCTION[dbo].[pd_addTwo](@number int)  
RETURNS int
AS   
BEGIN
    RETURN @number + 2;
        END;  

GO";

            using (SqlTestDb testDb = SqlTestDb.CreateNew(TestServerType.OnPrem, query))
            {
                // Get live connectionInfo and serverConnection
                ConnectionInfo connInfo = TestObjects.InitLiveConnectionInfoForDefinition(testDb.DatabaseName);
                ServerConnection serverConnection = TestObjects.InitLiveServerConnectionForDefinition(connInfo);


                PeekDefinition peekDefinition = new PeekDefinition(serverConnection, connInfo);
                string objectName = "pd_addTwo";
                string schemaName = null;
                string objectType = "FUNCTION";

                Location[] locations = peekDefinition.GetSqlObjectDefinition(peekDefinition.GetScalarValuedFunctionScripts, objectName, schemaName, objectType);
                Assert.NotNull(locations);
                Cleanup(locations);
            }
        }

        /// <summary>
        /// Test get definition for a table valued function object with active connection. Expect non-null locations
        /// </summary>
        [Fact]
        public void GetTableValuedFunctionDefinitionWithoutSchemaNameSuccessTest()
        {
            string query = @"
CREATE FUNCTION [dbo].[pd_returnTable] ()  
RETURNS TABLE  
AS  
RETURN   
(  
    select * from master.dbo.spt_monitor 
);  

GO";

            using (SqlTestDb testDb = SqlTestDb.CreateNew(TestServerType.OnPrem, query))
            {
                // Get live connectionInfo and serverConnection
                ConnectionInfo connInfo = TestObjects.InitLiveConnectionInfoForDefinition(testDb.DatabaseName);
                ServerConnection serverConnection = TestObjects.InitLiveServerConnectionForDefinition(connInfo);

                PeekDefinition peekDefinition = new PeekDefinition(serverConnection, connInfo);
                string objectName = "pd_returnTable";
                string schemaName = null;
                string objectType = "FUNCTION";

                Location[] locations = peekDefinition.GetSqlObjectDefinition(peekDefinition.GetTableValuedFunctionScripts, objectName, schemaName, objectType);
                Assert.NotNull(locations);
                Cleanup(locations);
            }
        }


        /// <summary>
        /// Test get definition for a user defined data type object with active connection and explicit schema name. Expect non-null locations
        /// </summary>
        [Fact]
        public void GetUserDefinedDataTypeDefinitionWithSchemaNameSuccessTest()
        {
            string query = @"
CREATE TYPE [dbo].[pd_ssn] FROM [varchar](11) NOT NULL
GO";

            using (SqlTestDb testDb = SqlTestDb.CreateNew(TestServerType.OnPrem, query))
            {
                // Get live connectionInfo and serverConnection
                ConnectionInfo connInfo = TestObjects.InitLiveConnectionInfoForDefinition(testDb.DatabaseName);
                ServerConnection serverConnection = TestObjects.InitLiveServerConnectionForDefinition(connInfo);

                PeekDefinition peekDefinition = new PeekDefinition(serverConnection, connInfo);
                string objectName = "pd_ssn";
                string schemaName = "dbo";
                string objectType = "Type";

                Location[] locations = peekDefinition.GetSqlObjectDefinition(peekDefinition.GetUserDefinedDataTypeScripts, objectName, schemaName, objectType);
                Assert.NotNull(locations);
                Cleanup(locations);
            }
        }

        /// <summary>
        /// Test get definition for a user defined data type object with active connection. Expect non-null locations
        /// </summary>
        [Fact]
        public void GetUserDefinedDataTypeDefinitionWithoutSchemaNameSuccessTest()
        {
            string query = @"
CREATE TYPE [dbo].[pd_ssn] FROM [varchar](11) NOT NULL
GO";

            using (SqlTestDb testDb = SqlTestDb.CreateNew(TestServerType.OnPrem, query))
            {
                // Get live connectionInfo and serverConnection
                ConnectionInfo connInfo = TestObjects.InitLiveConnectionInfoForDefinition(testDb.DatabaseName);
                ServerConnection serverConnection = TestObjects.InitLiveServerConnectionForDefinition(connInfo);

                PeekDefinition peekDefinition = new PeekDefinition(serverConnection, connInfo);
                string objectName = "pd_ssn";
                string schemaName = null;
                string objectType = "Type";

                Location[] locations = peekDefinition.GetSqlObjectDefinition(peekDefinition.GetUserDefinedDataTypeScripts, objectName, schemaName, objectType);
                Assert.NotNull(locations);
                Cleanup(locations);
            }
        }

        /// <summary>
        /// Test get definition for a user defined data type object that doesn't exist with active connection. Expect null locations
        /// </summary>
        [Fact]
        public void GetUserDefinedDataTypeDefinitionWithNonExistentFailureTest()
        {
            // Get live connectionInfo and serverConnection
            ConnectionInfo connInfo = TestObjects.InitLiveConnectionInfoForDefinition();
            ServerConnection serverConnection = TestObjects.InitLiveServerConnectionForDefinition(connInfo);

            PeekDefinition peekDefinition = new PeekDefinition(serverConnection, connInfo);
            string objectName = "doesNotExist";
            string schemaName = "dbo";
            string objectType = "Type";

            Location[] locations = peekDefinition.GetSqlObjectDefinition(peekDefinition.GetUserDefinedDataTypeScripts, objectName, schemaName, objectType);
            Assert.Null(locations);
        }

        /// <summary>
        /// Test get definition for a user defined table type object with active connection and explicit schema name. Expect non-null locations
        /// </summary>
        [Fact]
        public void GetUserDefinedTableTypeDefinitionWithSchemaNameSuccessTest()
        {
            string query = @"
CREATE TYPE [dbo].[pd_locationTableType] AS TABLE(
	[LocationName] [varchar](50) NULL,
	[CostRate] [int] NULL
)
GO";

            using (SqlTestDb testDb = SqlTestDb.CreateNew(TestServerType.OnPrem, query))
            {
                // Get live connectionInfo and serverConnection
                ConnectionInfo connInfo = TestObjects.InitLiveConnectionInfoForDefinition(testDb.DatabaseName);
                ServerConnection serverConnection = TestObjects.InitLiveServerConnectionForDefinition(connInfo);

                PeekDefinition peekDefinition = new PeekDefinition(serverConnection, connInfo);
                string objectName = "pd_locationTableType";
                string schemaName = "dbo";
                string objectType = "Type";

                Location[] locations = peekDefinition.GetSqlObjectDefinition(peekDefinition.GetUserDefinedTableTypeScripts, objectName, schemaName, objectType);
                Assert.NotNull(locations);
                Cleanup(locations);
            }
        }

        /// <summary>
        /// Test get definition for a user defined table type object with active connection. Expect non-null locations
        /// </summary>
        [Fact]
        public void GetUserDefinedTableTypeDefinitionWithoutSchemaNameSuccessTest()
        {
            string query = @"
CREATE TYPE [dbo].[pd_locationTableType] AS TABLE(
	[LocationName] [varchar](50) NULL,
	[CostRate] [int] NULL
)
GO";

            using (SqlTestDb testDb = SqlTestDb.CreateNew(TestServerType.OnPrem, query))
            {
                // Get live connectionInfo and serverConnection
                ConnectionInfo connInfo = TestObjects.InitLiveConnectionInfoForDefinition(testDb.DatabaseName);
                ServerConnection serverConnection = TestObjects.InitLiveServerConnectionForDefinition(connInfo);

                PeekDefinition peekDefinition = new PeekDefinition(serverConnection, connInfo);
                string objectName = "pd_locationTableType";
                string schemaName = null;
                string objectType = "Type";

                Location[] locations = peekDefinition.GetSqlObjectDefinition(peekDefinition.GetUserDefinedTableTypeScripts, objectName, schemaName, objectType);
                Assert.NotNull(locations);
                Cleanup(locations);
            }
        }

        /// <summary>
        /// Test get definition for a user defined table type object that doesn't exist with active connection. Expect null locations
        /// </summary>
        [Fact]
        public void GetUserDefinedTableTypeDefinitionWithNonExistentFailureTest()
        {
            // Get live connectionInfo and serverConnection
            ConnectionInfo connInfo = TestObjects.InitLiveConnectionInfoForDefinition();
            ServerConnection serverConnection = TestObjects.InitLiveServerConnectionForDefinition(connInfo);

            PeekDefinition peekDefinition = new PeekDefinition(serverConnection, connInfo);
            string objectName = "doesNotExist";
            string schemaName = "dbo";
            string objectType = "Type";

            Location[] locations = peekDefinition.GetSqlObjectDefinition(peekDefinition.GetUserDefinedTableTypeScripts, objectName, schemaName, objectType);
            Assert.Null(locations);
        }

        /// <summary>
        /// Test get definition for a synonym object with active connection and explicit schema name. Expect non-null locations
        /// </summary>
        [Fact]
        public void GetSynonymDefinitionWithSchemaNameSuccessTest()
        {
            string query = @"
CREATE SYNONYM [dbo].[pd_testTable] FOR master.dbo.spt_monitor
GO";

            using (SqlTestDb testDb = SqlTestDb.CreateNew(TestServerType.OnPrem, query))
            {
                // Get live connectionInfo and serverConnection
                ConnectionInfo connInfo = TestObjects.InitLiveConnectionInfoForDefinition(testDb.DatabaseName);
                ServerConnection serverConnection = TestObjects.InitLiveServerConnectionForDefinition(connInfo);

                PeekDefinition peekDefinition = new PeekDefinition(serverConnection, connInfo);
                string objectName = "pd_testTable";
                string schemaName = "dbo";
                string objectType = "Synonym";

                Location[] locations = peekDefinition.GetSqlObjectDefinition(peekDefinition.GetSynonymScripts, objectName, schemaName, objectType);
                Assert.NotNull(locations);
                Cleanup(locations);
            }
        }


        /// <summary>
        /// Test get definition for a Synonym object with active connection. Expect non-null locations
        /// </summary>
        [Fact]
        public void GetSynonymDefinitionWithoutSchemaNameSuccessTest()
        {
            string query = @"
CREATE SYNONYM [dbo].[pd_testTable] FOR master.dbo.spt_monitor
GO";

            using (SqlTestDb testDb = SqlTestDb.CreateNew(TestServerType.OnPrem, query))
            {
                // Get live connectionInfo and serverConnection
                ConnectionInfo connInfo = TestObjects.InitLiveConnectionInfoForDefinition(testDb.DatabaseName);
                ServerConnection serverConnection = TestObjects.InitLiveServerConnectionForDefinition(connInfo);

                PeekDefinition peekDefinition = new PeekDefinition(serverConnection, connInfo);
                string objectName = "pd_testTable";
                string schemaName = null;
                string objectType = "Synonym";

                Location[] locations = peekDefinition.GetSqlObjectDefinition(peekDefinition.GetSynonymScripts, objectName, schemaName, objectType);
                Assert.NotNull(locations);
                Cleanup(locations);
            }
        }

        /// <summary>
        /// Test get definition for a Synonym object that doesn't exist with active connection. Expect null locations
        /// </summary>
        [Fact]
        public void GetSynonymDefinitionWithNonExistentFailureTest()
        {
            // Get live connectionInfo and serverConnection
            ConnectionInfo connInfo = TestObjects.InitLiveConnectionInfoForDefinition();
            ServerConnection serverConnection = TestObjects.InitLiveServerConnectionForDefinition(connInfo);

            PeekDefinition peekDefinition = new PeekDefinition(serverConnection, connInfo);
            string objectName = "doesNotExist";
            string schemaName = "dbo";
            string objectType = "Synonym";

            Location[] locations = peekDefinition.GetSqlObjectDefinition(peekDefinition.GetSynonymScripts, objectName, schemaName, objectType);
            Assert.Null(locations);
        }

        /// <summary>
        /// Test get definition using declaration type for a view object with active connection
        /// Expect a non-null result with location
        /// </summary>
        [Fact]
        public void GetDefinitionUsingDeclarationTypeWithValidObjectTest()
        {
            ConnectionInfo connInfo = TestObjects.InitLiveConnectionInfoForDefinition();
            ServerConnection serverConnection = TestObjects.InitLiveServerConnectionForDefinition(connInfo);

            PeekDefinition peekDefinition = new PeekDefinition(serverConnection, connInfo);
            string objectName = "objects";
            string schemaName = "sys";

            DefinitionResult result = peekDefinition.GetDefinitionUsingDeclarationType(DeclarationType.View, "master.sys.objects", objectName, schemaName);
            Assert.NotNull(result);
            Assert.NotNull(result.Locations);
            Assert.False(result.IsErrorResult);
            Cleanup(result.Locations);

        }

        /// <summary>
        /// Test get definition using declaration type for a non existent view object with active connection
        /// Expect a non-null result with location
        /// </summary>
        [Fact]
        public void GetDefinitionUsingDeclarationTypeWithNonexistentObjectTest()
        {
            ConnectionInfo connInfo = TestObjects.InitLiveConnectionInfoForDefinition();
            ServerConnection serverConnection = TestObjects.InitLiveServerConnectionForDefinition(connInfo);

            PeekDefinition peekDefinition = new PeekDefinition(serverConnection, connInfo);
            string objectName = "doesNotExist";
            string schemaName = "sys";

            DefinitionResult result = peekDefinition.GetDefinitionUsingDeclarationType(DeclarationType.View, "master.sys.objects", objectName, schemaName);
            Assert.NotNull(result);
            Assert.True(result.IsErrorResult);
        }

        /// <summary>
        /// Test get definition using quickInfo text for a view object with active connection
        /// Expect a non-null result with location
        /// </summary>
        [Fact]
        public void GetDefinitionUsingQuickInfoTextWithValidObjectTest()
        {
            ConnectionInfo connInfo = TestObjects.InitLiveConnectionInfoForDefinition();
            ServerConnection serverConnection = TestObjects.InitLiveServerConnectionForDefinition(connInfo);

            PeekDefinition peekDefinition = new PeekDefinition(serverConnection, connInfo);
            string objectName = "objects";
            string schemaName = "sys";
            string quickInfoText = "view master.sys.objects";

            DefinitionResult result = peekDefinition.GetDefinitionUsingQuickInfoText(quickInfoText, objectName, schemaName);
            Assert.NotNull(result);
            Assert.NotNull(result.Locations);
            Assert.False(result.IsErrorResult);
            Cleanup(result.Locations);

        }

        /// <summary>
        /// Test get definition using quickInfo text for a view object with active connection
        /// Expect a non-null result with location
        /// </summary>
        [Fact]
        public void GetDefinitionUsingQuickInfoTextWithNonexistentObjectTest()
        {
            ConnectionInfo connInfo = TestObjects.InitLiveConnectionInfoForDefinition();
            ServerConnection serverConnection = TestObjects.InitLiveServerConnectionForDefinition(connInfo);

            PeekDefinition peekDefinition = new PeekDefinition(serverConnection, connInfo);
            string objectName = "doesNotExist";
            string schemaName = "sys";
            string quickInfoText = "view master.sys.objects";

            DefinitionResult result = peekDefinition.GetDefinitionUsingQuickInfoText(quickInfoText, objectName, schemaName);
            Assert.NotNull(result);
            Assert.True(result.IsErrorResult);
        }

        /// <summary>
        /// Helper method to clean up script files
        /// </summary>
        private void Cleanup(Location[] locations)
        {
            Uri fileUri = new Uri(locations[0].Uri);
            if (File.Exists(fileUri.LocalPath))
            {
                try
                {
                    File.Delete(fileUri.LocalPath);
                }
                catch(Exception)
                {

                }
            }
        }
    }
}
