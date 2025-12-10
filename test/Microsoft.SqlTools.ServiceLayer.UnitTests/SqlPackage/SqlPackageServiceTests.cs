//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Threading.Tasks;
using Microsoft.Data.Tools.Schema.CommandLineTool;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.SqlPackage;
using Microsoft.SqlTools.ServiceLayer.SqlPackage.Contracts;
using Moq;
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
            Assert.AreSame(instance1, instance2);
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

            var parameters = new GenerateSqlPackageCommandParams
            {
                Action = CommandLineToolAction.Publish,
                Arguments = "{\"SourceFile\":\"C:\\\\test\\\\database.dacpac\",\"TargetServerName\":\"localhost\",\"TargetDatabaseName\":\"TestDB\",\"TargetConnectionString\":\"Server=localhost;Database=TestDB;Integrated Security=true;\"}",
                DeploymentOptions = new Microsoft.SqlServer.Dac.DacDeployOptions
                {
                    BackupDatabaseBeforeChanges = true,
                    BlockOnPossibleDataLoss = true,
                    IncludeCompositeObjects = true,
                    VerifyDeployment = true
                },
                Variables = new System.Collections.Generic.Dictionary<string, string>
                {
                    { "Environment", "Production" },
                    { "DatabaseVersion", "1.0.0" }
                }
            };

            // Act
            await service.HandleGenerateSqlPackageCommandRequest(parameters, requestContext.Object);

            // Assert
            Assert.IsNotNull(capturedResult);
            Assert.IsTrue(capturedResult.Success);
            Assert.IsNotNull(capturedResult.Command);
            StringAssert.Contains("SqlPackage", capturedResult.Command);
            StringAssert.Contains("/Action:Publish", capturedResult.Command);
            StringAssert.Contains("database.dacpac", capturedResult.Command);
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

            var parameters = new GenerateSqlPackageCommandParams
            {
                Action = CommandLineToolAction.Extract,
                Arguments = "{\"SourceServerName\":\"localhost\",\"SourceDatabaseName\":\"TestDB\",\"TargetFile\":\"C:\\\\test\\\\output.dacpac\",\"SourceConnectionString\":\"Server=localhost;Database=TestDB;Integrated Security=true;\"}",
                ExtractOptions = new Microsoft.SqlServer.Dac.DacExtractOptions
                {
                    ExtractApplicationScopedObjectsOnly = true,
                    ExtractReferencedServerScopedElements = Microsoft.SqlServer.Dac.DacSchemaModelStorageType.Memory,
                    IgnoreExtendedProperties = false,
                    IgnorePermissions = false
                }
            };

            // Act
            await service.HandleGenerateSqlPackageCommandRequest(parameters, requestContext.Object);

            // Assert
            Assert.IsNotNull(capturedResult);
            Assert.IsTrue(capturedResult.Success);
            Assert.IsNotNull(capturedResult.Command);
            StringAssert.Contains("SqlPackage", capturedResult.Command);
            StringAssert.Contains("/Action:Extract", capturedResult.Command);
            StringAssert.Contains("output.dacpac", capturedResult.Command);
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

            var parameters = new GenerateSqlPackageCommandParams
            {
                Action = CommandLineToolAction.Script,
                Arguments = "{\"SourceFile\":\"C:\\\\test\\\\database.dacpac\",\"TargetServerName\":\"localhost\",\"TargetDatabaseName\":\"TestDB\",\"OutputPath\":\"C:\\\\test\\\\script.sql\",\"TargetConnectionString\":\"Server=localhost;Database=TestDB;Integrated Security=true;\"}",
                DeploymentOptions = new Microsoft.SqlServer.Dac.DacDeployOptions
                {
                    GenerateSmartDefaults = true,
                    IncludeTransactionalScripts = true,
                    ScriptDatabaseOptions = true,
                    CommentOutSetVarDeclarations = false
                },
                Variables = new System.Collections.Generic.Dictionary<string, string>
                {
                    { "Environment", "Staging" },
                    { "DebugMode", "false" }
                }
            };

            // Act
            await service.HandleGenerateSqlPackageCommandRequest(parameters, requestContext.Object);

            // Assert
            Assert.IsNotNull(capturedResult);
            Assert.IsTrue(capturedResult.Success);
            Assert.IsNotNull(capturedResult.Command);
            StringAssert.Contains("SqlPackage", capturedResult.Command);
            StringAssert.Contains("/Action:Script", capturedResult.Command);
            StringAssert.Contains("script.sql", capturedResult.Command);
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

            var parameters = new GenerateSqlPackageCommandParams
            {
                Action = CommandLineToolAction.Export,
                Arguments = "{\"SourceServerName\":\"localhost\",\"SourceDatabaseName\":\"TestDB\",\"TargetFile\":\"C:\\\\test\\\\export.bacpac\",\"SourceConnectionString\":\"Server=localhost;Database=TestDB;Integrated Security=true;\"}",
                ExportOptions = new Microsoft.SqlServer.Dac.DacExportOptions
                {
                    CommandTimeout = 120,
                    VerifyFullTextDocumentTypesSupported = true
                }
            };

            // Act
            await service.HandleGenerateSqlPackageCommandRequest(parameters, requestContext.Object);

            // Assert
            Assert.IsNotNull(capturedResult);
            Assert.IsTrue(capturedResult.Success);
            Assert.IsNotNull(capturedResult.Command);
            StringAssert.Contains("SqlPackage", capturedResult.Command);
            StringAssert.Contains("/Action:Export", capturedResult.Command);
            StringAssert.Contains("export.bacpac", capturedResult.Command);
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

            var parameters = new GenerateSqlPackageCommandParams
            {
                Action = CommandLineToolAction.Import,
                Arguments = "{\"SourceFile\":\"C:\\\\test\\\\data.bacpac\",\"TargetServerName\":\"localhost\",\"TargetDatabaseName\":\"TestDB\",\"TargetConnectionString\":\"Server=localhost;Database=TestDB;Integrated Security=true;\"}",
                ImportOptions = new Microsoft.SqlServer.Dac.DacImportOptions
                {
                    CommandTimeout = 180,
                    DatabaseEdition = Microsoft.SqlServer.Dac.DacAzureDatabaseSpecification.Standard,
                    DatabaseServiceObjective = "S3",
                    DatabaseMaximumSize = 100
                }
            };

            // Act
            await service.HandleGenerateSqlPackageCommandRequest(parameters, requestContext.Object);

            // Assert
            Assert.IsNotNull(capturedResult);
            Assert.IsTrue(capturedResult.Success);
            Assert.IsNotNull(capturedResult.Command);
            StringAssert.Contains("SqlPackage", capturedResult.Command);
            StringAssert.Contains("/Action:Import", capturedResult.Command);
            StringAssert.Contains("data.bacpac", capturedResult.Command);
        }
    }
}
