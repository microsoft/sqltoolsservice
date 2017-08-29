//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;
using Xunit;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.Metadata.Contracts;
using Microsoft.SqlTools.ServiceLayer.Scripting;
using Moq;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Scripting.Contracts;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.Scripting
{
    /// <summary>
    /// Tests for the scripting service component
    /// </summary>
    public class ScriptingServiceTests
    {
        private const string SchemaName = "dbo";
        private const string TableName = "spt_monitor";
        private const string ViewName = "test";
        private const string DatabaseName = "test-db";
        private const string StoredProcName = "test-sp";
        private string[] objects = new string[5] {"Table", "View", "Schema", "Database", "SProc"};

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

        private static ObjectMetadata GenerateMetadata(string objectType)
        {
            var metadata = new ObjectMetadata()
            {
                Schema = SchemaName,
                Name = objectType
            };
            switch(objectType)
            {
                case("Table"):
                    metadata.MetadataType = MetadataType.Table;
                    metadata.Name = TableName;
                    break;
                case("View"):
                    metadata.MetadataType = MetadataType.View;
                    metadata.Name = ViewName;
                    break;
                case("Database"):
                    metadata.MetadataType = MetadataType.Database;
                    metadata.Name = DatabaseName;
                    break;
                case("Schema"):
                    metadata.MetadataType = MetadataType.Schema;
                    metadata.MetadataTypeName = SchemaName;
                    break;
                case("SProc"):
                    metadata.MetadataType = MetadataType.SProc;
                    metadata.MetadataTypeName = StoredProcName;
                    break;
                default:
                    metadata.MetadataType = MetadataType.Table;
                    metadata.Name = TableName;
                    break;                    
            }
            return metadata;
        }

        // private async Task<Mock<RequestContext<ScriptingScriptAsResult>>> SendAndValidateScriptRequest()
        // {
        //     var result = GetLiveAutoCompleteTestObjects();
        //     var requestContext = new Mock<RequestContext<ScriptingScriptAsResult>>();
        //     requestContext.Setup(x => x.SendResult(It.IsAny<ScriptingScriptAsResult>())).Returns(Task.FromResult(new object()));

        //     var scriptingParams = new ScriptingScriptAsParams
        //     {
        //         OwnerUri = result.ConnectionInfo.OwnerUri,
        //         //Operation = operation,
        //         //Metadata = GenerateMetadata(objectType)
        //     };

        //     // await ScriptingService.HandleScriptingScriptAsRequest(scriptingParams, requestContext.Object);

        //     return requestContext;
        // }

        /// <summary>
        /// Verify the script as select request
        /// </summary>
        [Fact]
        public async void ScriptingScriptAsSelect()
        {
            foreach (string obj in objects)
            {
                //Assert.NotNull(await SendAndValidateScriptRequest());
            }
        }

        /// <summary>
        /// Verify the script as create request
        /// </summary>
        [Fact]
        public async void ScriptingScriptAsCreate()
        {
            foreach (string obj in objects)
            {
                //Assert.NotNull(await SendAndValidateScriptRequest());
            }
        }

        /// <summary>
        /// Verify the script as insert request
        /// </summary>
        [Fact]
        public async void ScriptingScriptAsInsert()
        {
            foreach (string obj in objects)
            {
                //Assert.NotNull(await SendAndValidateScriptRequest());
            }
        }

        /// <summary>
        /// Verify the script as update request
        /// </summary>
        [Fact]
        public async void ScriptingScriptAsUpdate()
        {
            foreach (string obj in objects)
            {
                //Assert.NotNull(await SendAndValidateScriptRequest());
            }
        }

        /// <summary>
        /// Verify the script as delete request
        /// </summary>
        [Fact]
        public async void ScriptingScriptAsDelete()
        {
            foreach (string obj in objects)
            {
                //Assert.NotNull(await SendAndValidateScriptRequest());
            }
        }
    }
}
