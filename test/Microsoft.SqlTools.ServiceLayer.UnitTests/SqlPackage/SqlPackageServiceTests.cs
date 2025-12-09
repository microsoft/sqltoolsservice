//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Threading.Tasks;
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
                Action = "Publish",
                SourceFile = "C:\\test\\database.dacpac",
                TargetServerName = "localhost",
                TargetDatabaseName = "TestDB"
            };

            // Act
            await service.HandleGenerateSqlPackageCommandRequest(parameters, requestContext.Object);

            // Assert
            Assert.IsNotNull(capturedResult);
            Assert.IsTrue(capturedResult.Success);
            Assert.IsNotNull(capturedResult.Command);
            StringAssert.StartsWith("sqlpackage ", capturedResult.Command);
            StringAssert.Contains("/Action:Publish", capturedResult.Command);
            StringAssert.Contains("/SourceFile:", capturedResult.Command);
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
                Action = "Extract",
                SourceServerName = "localhost",
                SourceDatabaseName = "TestDB",
                TargetFile = "C:\\test\\output.dacpac"
            };

            // Act
            await service.HandleGenerateSqlPackageCommandRequest(parameters, requestContext.Object);

            // Assert
            Assert.IsNotNull(capturedResult);
            Assert.IsTrue(capturedResult.Success);
            Assert.IsNotNull(capturedResult.Command);
            StringAssert.StartsWith("sqlpackage ", capturedResult.Command);
            StringAssert.Contains("/Action:Extract", capturedResult.Command);
            StringAssert.Contains("/TargetFile:", capturedResult.Command);
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
                Action = "Script",
                SourceFile = "C:\\test\\database.dacpac",
                TargetServerName = "localhost",
                TargetDatabaseName = "TestDB",
                TargetFile = "C:\\test\\script.sql"
            };

            // Act
            await service.HandleGenerateSqlPackageCommandRequest(parameters, requestContext.Object);

            // Assert
            Assert.IsNotNull(capturedResult);
            Assert.IsTrue(capturedResult.Success);
            Assert.IsNotNull(capturedResult.Command);
            StringAssert.StartsWith("sqlpackage ", capturedResult.Command);
            StringAssert.Contains("/Action:Script", capturedResult.Command);
            StringAssert.Contains("/SourceFile:", capturedResult.Command);
            StringAssert.Contains("/OutputPath:", capturedResult.Command);
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
                Action = "Export",
                SourceServerName = "localhost",
                SourceDatabaseName = "TestDB",
                TargetFile = "C:\\test\\export.bacpac"
            };

            // Act
            await service.HandleGenerateSqlPackageCommandRequest(parameters, requestContext.Object);

            // Assert
            Assert.IsNotNull(capturedResult);
            Assert.IsTrue(capturedResult.Success);
            Assert.IsNotNull(capturedResult.Command);
            StringAssert.StartsWith("sqlpackage ", capturedResult.Command);
            StringAssert.Contains("/Action:Export", capturedResult.Command);
            StringAssert.Contains("/TargetFile:", capturedResult.Command);
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
                Action = "Import",
                SourceFile = "C:\\test\\data.bacpac",
                TargetServerName = "localhost",
                TargetDatabaseName = "TestDB"
            };

            // Act
            await service.HandleGenerateSqlPackageCommandRequest(parameters, requestContext.Object);

            // Assert
            Assert.IsNotNull(capturedResult);
            Assert.IsTrue(capturedResult.Success);
            Assert.IsNotNull(capturedResult.Command);
            StringAssert.StartsWith("sqlpackage ", capturedResult.Command);
            StringAssert.Contains("/Action:Import", capturedResult.Command);
            StringAssert.Contains("/SourceFile:", capturedResult.Command);
            StringAssert.Contains("data.bacpac", capturedResult.Command);
        }
    }
}
