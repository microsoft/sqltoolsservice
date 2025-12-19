//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Threading.Tasks;
using Microsoft.Data.Tools.Schema.CommandLineTool;
using Microsoft.SqlServer.Dac;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.SqlPackage;
using Microsoft.SqlTools.ServiceLayer.SqlPackage.Contracts;
using Microsoft.SqlTools.ServiceLayer.DacFx.Contracts;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.SqlPackage
{
    /// <summary>
    /// Tests for SqlPackageService
    /// </summary>
    public class SqlPackageServiceTests
    {
        private SqlPackageService service;

        [SetUp]
        public void Setup()
        {
            service = SqlPackageService.Instance;
        }

        [Test]
        public void ServiceInstanceShouldBeSingleton()
        {
            // Arrange & Act
            var instance1 = SqlPackageService.Instance;
            var instance2 = SqlPackageService.Instance;

            // Assert
            Assert.AreSame(instance1, instance2, "Service should be a singleton");
        }


        [Test]
        public async Task GeneratePublishCommand_ShouldReturnValidCommand()
        {
            // Arrange
            var requestContext = new Mock<RequestContext<SqlPackageCommandResult>>();
            SqlPackageCommandResult? capturedResult = null;
            requestContext.Setup(x => x.SendResult(It.IsAny<SqlPackageCommandResult>()))
                .Callback<SqlPackageCommandResult>(r => capturedResult = r)
                .Returns(Task.CompletedTask);

            var parameters = new SqlPackageCommandParams
            {
                Action = CommandLineToolAction.Publish,
                Arguments = JsonConvert.SerializeObject(new
                {
                    SourceFile = "C:\\test\\database.dacpac",
                    TargetServerName = "localhost",
                    TargetDatabaseName = "TestDB"
                }),
                DeploymentOptions = new DeploymentOptions(),
                Variables = new System.Collections.Generic.Dictionary<string, string>
                {
                    { "Environment", "Production" },
                    { "DatabaseVersion", "1.0.0" }
                }
            };

            // Set deployment options via BooleanOptionsDictionary
            parameters.DeploymentOptions.BooleanOptionsDictionary[nameof(DacDeployOptions.BackupDatabaseBeforeChanges)].Value = true;
            parameters.DeploymentOptions.BooleanOptionsDictionary[nameof(DacDeployOptions.BlockOnPossibleDataLoss)].Value = false;
            parameters.DeploymentOptions.BooleanOptionsDictionary[nameof(DacDeployOptions.IncludeCompositeObjects)].Value = true;
            parameters.DeploymentOptions.BooleanOptionsDictionary[nameof(DacDeployOptions.VerifyDeployment)].Value = false;
            parameters.DeploymentOptions.ExcludeObjectTypes.Value = ["ServerTriggers", "ExternalStreamingJobs"];

            // Act
            await service.HandleGenerateSqlPackageCommandRequest(parameters, requestContext.Object);

            // Assert
            Assert.IsNotNull(capturedResult, "Result should not be null");
            Assert.IsTrue(capturedResult.Success, "Command generation should succeed");
            Assert.IsNotNull(capturedResult.Command, "Generated command should not be null");
            StringAssert.Contains("SqlPackage", capturedResult.Command, "Sqlpacakge command should have SqlPackage");
            StringAssert.Contains("/Action:Publish", capturedResult.Command, "Command should have publish action");
            StringAssert.Contains("/SourceFile:\"C:\\test\\database.dacpac\"", capturedResult.Command, "command should have the sourceFile");
            StringAssert.Contains("/p:ExcludeObjectTypes=ServerTriggers;ExternalStreamingJobs", capturedResult.Command, "Command should return exclude object types");
        }

        [Test]
        public async Task GenerateExtractCommand_ShouldReturnValidCommand()
        {
            // Arrange
            var requestContext = new Mock<RequestContext<SqlPackageCommandResult>>();
            SqlPackageCommandResult? capturedResult = null;
            requestContext.Setup(x => x.SendResult(It.IsAny<SqlPackageCommandResult>()))
                .Callback<SqlPackageCommandResult>(r => capturedResult = r)
                .Returns(Task.CompletedTask);

            var parameters = new SqlPackageCommandParams
            {
                Action = CommandLineToolAction.Extract,
                Arguments = JsonConvert.SerializeObject(new
                {
                    TargetFile = "C:\\test\\output.dacpac",
                    SourceConnectionString = "Server=localhost;Database=TestDB;Integrated Security=true;"
                }),
                ExtractOptions = new Microsoft.SqlServer.Dac.DacExtractOptions
                {
                    ExtractApplicationScopedObjectsOnly = false,
                    ExtractReferencedServerScopedElements = false,
                    IgnoreExtendedProperties = true,
                    IgnorePermissions = false
                }
            };

            // Act
            await service.HandleGenerateSqlPackageCommandRequest(parameters, requestContext.Object);

            // Assert
            Assert.IsNotNull(capturedResult, "Result should not be null");
            Assert.IsTrue(capturedResult.Success, "Command generation should succeed");
            Assert.IsNotNull(capturedResult.Command, "Generated command should not be null");
            StringAssert.Contains("SqlPackage", capturedResult.Command, "Command should contain SqlPackage");
            StringAssert.Contains("/Action:Extract", capturedResult.Command, "Command should have extract action");
            StringAssert.Contains("output.dacpac", capturedResult.Command, "Command should contain output file path");
        }

        [Test]
        public async Task GenerateScriptCommand_ShouldReturnValidCommand()
        {
            // Arrange
            var requestContext = new Mock<RequestContext<SqlPackageCommandResult>>();
            SqlPackageCommandResult? capturedResult = null;
            requestContext.Setup(x => x.SendResult(It.IsAny<SqlPackageCommandResult>()))
                .Callback<SqlPackageCommandResult>(r => capturedResult = r)
                .Returns(Task.CompletedTask);

            var parameters = new SqlPackageCommandParams
            {
                Action = CommandLineToolAction.Script,
                Arguments = JsonConvert.SerializeObject(new
                {
                    SourceFile = "C:\\test\\database.dacpac",
                    TargetServerName = "localhost",
                    TargetDatabaseName = "TestDB",
                    OutputPath = "C:\\test\\script.sql",
                    TargetConnectionString = "Server=localhost;Database=TestDB;Integrated Security=true;"
                }),
                DeploymentOptions = new DeploymentOptions(),
                Variables = new System.Collections.Generic.Dictionary<string, string>
                {
                    { "Environment", "Staging" },
                    { "DebugMode", "false" }
                }
            };

            // Set deployment options via BooleanOptionsDictionary
            parameters.DeploymentOptions.BooleanOptionsDictionary[nameof(DacDeployOptions.GenerateSmartDefaults)].Value = true;
            parameters.DeploymentOptions.BooleanOptionsDictionary[nameof(DacDeployOptions.IncludeTransactionalScripts)].Value = true;
            parameters.DeploymentOptions.BooleanOptionsDictionary[nameof(DacDeployOptions.ScriptDatabaseOptions)].Value = true;
            parameters.DeploymentOptions.BooleanOptionsDictionary[nameof(DacDeployOptions.CommentOutSetVarDeclarations)].Value = false;

            // Act
            await service.HandleGenerateSqlPackageCommandRequest(parameters, requestContext.Object);

            // Assert
            Assert.IsNotNull(capturedResult, "Result should not be null");
            Assert.IsTrue(capturedResult.Success, "Command generation should succeed");
            Assert.IsNotNull(capturedResult.Command, "Generated command should not be null");
            StringAssert.Contains("SqlPackage", capturedResult.Command, "Command should contain SqlPackage");
            StringAssert.Contains("/Action:Script", capturedResult.Command, "Command should have script action");
            StringAssert.Contains("script.sql", capturedResult.Command, "Command should contain output script path");
        }

        [Test]
        public async Task GenerateExportCommand_ShouldReturnValidCommand()
        {
            // Arrange
            var requestContext = new Mock<RequestContext<SqlPackageCommandResult>>();
            SqlPackageCommandResult? capturedResult = null;
            requestContext.Setup(x => x.SendResult(It.IsAny<SqlPackageCommandResult>()))
                .Callback<SqlPackageCommandResult>(r => capturedResult = r)
                .Returns(Task.CompletedTask);

            var parameters = new SqlPackageCommandParams
            {
                Action = CommandLineToolAction.Export,
                Arguments = JsonConvert.SerializeObject(new
                {
                    SourceServerName = "localhost",
                    SourceDatabaseName = "TestDB",
                    TargetFile = "C:\\test\\export.bacpac",
                    SourceConnectionString = "Server=localhost;Database=TestDB;Integrated Security=true;"
                }),
                ExportOptions = new Microsoft.SqlServer.Dac.DacExportOptions
                {
                    CommandTimeout = 120,
                        VerifyFullTextDocumentTypesSupported = true
                }
            };

            // Act
            await service.HandleGenerateSqlPackageCommandRequest(parameters, requestContext.Object);

            // Assert
            Assert.IsNotNull(capturedResult, "Result should not be null");
            Assert.IsTrue(capturedResult.Success, "Command generation should succeed");
            Assert.IsNotNull(capturedResult.Command, "Generated command should not be null");
            StringAssert.Contains("SqlPackage", capturedResult.Command, "Command should contain SqlPackage");
            StringAssert.Contains("/Action:Export", capturedResult.Command, "Command should have export action");
            StringAssert.Contains("export.bacpac", capturedResult.Command, "Command should contain export file path");
        }

        [Test]
        public async Task GenerateImportCommand_ShouldReturnValidCommand()
        {
            // Arrange
            var requestContext = new Mock<RequestContext<SqlPackageCommandResult>>();
            SqlPackageCommandResult? capturedResult = null;
            requestContext.Setup(x => x.SendResult(It.IsAny<SqlPackageCommandResult>()))
                .Callback<SqlPackageCommandResult>(r => capturedResult = r)
                .Returns(Task.CompletedTask);

            var parameters = new SqlPackageCommandParams
            {
                Action = CommandLineToolAction.Import,
                Arguments = JsonConvert.SerializeObject(new
                {
                    SourceFile = "C:\\test\\data.bacpac",
                    TargetServerName = "localhost",
                    TargetDatabaseName = "TestDB",
                    TargetConnectionString = "Server=localhost;Database=TestDB;Integrated Security=true;"
                }),
                ImportOptions = new Microsoft.SqlServer.Dac.DacImportOptions
                {
                    CommandTimeout = 180,
                }
            };

            // Act
            await service.HandleGenerateSqlPackageCommandRequest(parameters, requestContext.Object);

            // Assert
            Assert.IsNotNull(capturedResult, "Result should not be null");
            Assert.IsTrue(capturedResult.Success, "Command generation should succeed");
            Assert.IsNotNull(capturedResult.Command, "Generated command should not be null");
            StringAssert.Contains("SqlPackage", capturedResult.Command, "Command should contain SqlPackage");
            StringAssert.Contains("/Action:Import", capturedResult.Command, "Command should have import action");
            StringAssert.Contains("data.bacpac", capturedResult.Command, "Command should contain import file path");
        }
    }
}
