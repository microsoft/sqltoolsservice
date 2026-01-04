//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Tools.Schema.CommandLineTool;
using Microsoft.Data.Tools.Schema.CommandLineTool.Contracts;
using Microsoft.SqlServer.Dac;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.DacFx.Contracts;
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
                CommandLineArguments = new ServiceLayer.SqlPackage.Contracts.SqlPackageCommandLineArguments
                {
                    Action = CommandLineToolAction.Publish,
                    SourceFile = "C:\\test\\database.dacpac",
                    TargetServerName = "localhost",
                    TargetDatabaseName = "TestDB"
                },
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
                CommandLineArguments = new ServiceLayer.SqlPackage.Contracts.SqlPackageCommandLineArguments
                {
                    Action = CommandLineToolAction.Extract,
                    TargetFile = Path.Combine(Path.GetTempPath(), "output.dacpac"),
                    SourceConnectionString = "Server=localhost;Database=TestDB;Integrated Security=true;"
                },
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
                CommandLineArguments = new ServiceLayer.SqlPackage.Contracts.SqlPackageCommandLineArguments
                {
                    Action = CommandLineToolAction.Script,
                    SourceFile = "C:\\test\\database.dacpac",
                    TargetServerName = "localhost",
                    TargetDatabaseName = "TestDB",
                    OutputPath = Path.Combine(Path.GetTempPath(), "script.sql")
                },
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
                CommandLineArguments = new ServiceLayer.SqlPackage.Contracts.SqlPackageCommandLineArguments
                {
                    Action = CommandLineToolAction.Export,
                    SourceServerName = "localhost",
                    SourceDatabaseName = "TestDB",
                    TargetFile = Path.Combine(Path.GetTempPath(), "temp.bacpac")
                },
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
            StringAssert.Contains("temp.bacpac", capturedResult.Command, "Command should contain export file path");
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
                CommandLineArguments = new ServiceLayer.SqlPackage.Contracts.SqlPackageCommandLineArguments
                {
                    Action = CommandLineToolAction.Import,
                    SourceFile = "C:\\test\\data.bacpac",
                    TargetServerName = "localhost",
                    TargetDatabaseName = "TestDB"
                },
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

        [Test]
        public async Task GeneratePublishCommand_WithUnmaskedData_ShouldIncludeUnmaskedFlag()
        {
            // Arrange
            var requestContext = new Mock<RequestContext<SqlPackageCommandResult>>();
            SqlPackageCommandResult? capturedResult = null;
            requestContext.Setup(x => x.SendResult(It.IsAny<SqlPackageCommandResult>()))
                .Callback<SqlPackageCommandResult>(r => capturedResult = r)
                .Returns(Task.CompletedTask);

            var parameters = new SqlPackageCommandParams
            {
                CommandLineArguments = new ServiceLayer.SqlPackage.Contracts.SqlPackageCommandLineArguments
                {
                    Action = CommandLineToolAction.Publish,
                    SourceFile = "C:\\test\\database.dacpac",
                    TargetServerName = "localhost",
                    TargetDatabaseName = "TestDB"
                },
                DeploymentOptions = new DeploymentOptions(),
                MaskMode = MaskMode.Unmasked  // Set to Unmasked to enable unmasked data
            };

            // Act
            await service.HandleGenerateSqlPackageCommandRequest(parameters, requestContext.Object);

            // Assert
            Assert.IsNotNull(capturedResult, "Result should not be null");
            Assert.IsTrue(capturedResult.Success, "Command generation should succeed");
            Assert.IsNotNull(capturedResult.Command, "Generated command should not be null");
            StringAssert.Contains("/Action:Publish", capturedResult.Command, "Command should have publish action");
            StringAssert.Contains("/Unmasked", capturedResult.Command, "Command should contain unmasked flag when Masked is false");
        }

        [Test]
        public async Task GeneratePublishCommand_WithMaskedData_ShouldNotIncludeUnmaskedFlag()
        {
            // Arrange
            var requestContext = new Mock<RequestContext<SqlPackageCommandResult>>();
            SqlPackageCommandResult? capturedResult = null;
            requestContext.Setup(x => x.SendResult(It.IsAny<SqlPackageCommandResult>()))
                .Callback<SqlPackageCommandResult>(r => capturedResult = r)
                .Returns(Task.CompletedTask);

            var parameters = new SqlPackageCommandParams
            {
                CommandLineArguments = new ServiceLayer.SqlPackage.Contracts.SqlPackageCommandLineArguments
                {
                    Action = CommandLineToolAction.Publish,
                    SourceFile = "C:\\test\\database.dacpac",
                    TargetServerName = "localhost",
                    TargetDatabaseName = "TestDB"
                },
                DeploymentOptions = new DeploymentOptions(),
                MaskMode = MaskMode.Masked  // Set to Masked for masked data (explicit)
            };

            // Act
            await service.HandleGenerateSqlPackageCommandRequest(parameters, requestContext.Object);

            // Assert
            Assert.IsNotNull(capturedResult, "Result should not be null");
            Assert.IsTrue(capturedResult.Success, "Command generation should succeed");
            Assert.IsNotNull(capturedResult.Command, "Generated command should not be null");
            StringAssert.Contains("/Action:Publish", capturedResult.Command, "Command should have publish action");
            Assert.IsFalse(capturedResult.Command.Contains("/Unmasked"), "Command should not contain unmasked flag when Masked is true");
        }

        [Test]
        public async Task GenerateExtractCommand_WithUnmaskedData_ShouldIncludeUnmaskedFlag()
        {
            // Arrange
            var requestContext = new Mock<RequestContext<SqlPackageCommandResult>>();
            SqlPackageCommandResult? capturedResult = null;
            requestContext.Setup(x => x.SendResult(It.IsAny<SqlPackageCommandResult>()))
                .Callback<SqlPackageCommandResult>(r => capturedResult = r)
                .Returns(Task.CompletedTask);

            var parameters = new SqlPackageCommandParams
            {
                CommandLineArguments = new ServiceLayer.SqlPackage.Contracts.SqlPackageCommandLineArguments
                {
                    Action = CommandLineToolAction.Extract,
                    TargetFile = Path.Combine(Path.GetTempPath(), "output.dacpac"),
                    SourceConnectionString = "Server=localhost;Database=TestDB;Integrated Security=true;"
                },
                ExtractOptions = new Microsoft.SqlServer.Dac.DacExtractOptions(),
                MaskMode = MaskMode.Unmasked  // Unmasked data
            };

            // Act
            await service.HandleGenerateSqlPackageCommandRequest(parameters, requestContext.Object);

            // Assert
            Assert.IsNotNull(capturedResult, "Result should not be null");
            Assert.IsTrue(capturedResult.Success, "Command generation should succeed");
            Assert.IsNotNull(capturedResult.Command, "Generated command should not be null");
            StringAssert.Contains("/Action:Extract", capturedResult.Command, "Command should have extract action");
            StringAssert.Contains("/Unmasked", capturedResult.Command, "Command should contain unmasked flag when Masked is false");
        }
    }
}
