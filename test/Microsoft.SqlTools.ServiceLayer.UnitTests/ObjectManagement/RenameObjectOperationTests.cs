//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System.Globalization;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.ObjectManagement;
using Microsoft.SqlTools.ServiceLayer.ObjectManagement.Contracts;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Moq;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.ObjectManagement
{
    [TestFixture]
    public class RenameObjectOperationTests
    {
        [Test]
        public void ExecuteInvokesRenameWithDescriptiveProgress()
        {
            var handler = new Mock<IObjectTypeHandler>();
            handler.Setup(x => x.Rename("connection-uri", "Server/Database[@Name='master']/Table[@Name='OldTable' and @Schema='dbo']", "NewTable"))
                .Returns(Task.CompletedTask);

            var requestParams = new RenameRequestParams
            {
                ConnectionUri = "connection-uri",
                ObjectUrn = "Server/Database[@Name='master']/Table[@Name='OldTable' and @Schema='dbo']",
                NewName = "NewTable",
            };

            var operation = new RenameObjectOperation(handler.Object, requestParams)
            {
                SqlTask = new SqlTask()
            };

            operation.Execute(TaskExecutionMode.Execute);

            handler.Verify(x => x.Rename("connection-uri", "Server/Database[@Name='master']/Table[@Name='OldTable' and @Schema='dbo']", "NewTable"), Times.Once);
            Assert.That(operation.TaskName, Is.EqualTo(string.Format(CultureInfo.CurrentCulture, global::Microsoft.SqlTools.ServiceLayer.SR.RenameTaskName, "OldTable")));
            Assert.That(operation.SqlTask.PercentComplete, Is.EqualTo(-1));
            Assert.That(operation.SqlTask.ProgressMessage, Is.EqualTo("Rename table to 'NewTable'."));
        }

        [Test]
        public void ExecuteUsesDatabaseSpecificProgressForDatabaseRenames()
        {
            var handler = new Mock<IObjectTypeHandler>();
            handler.Setup(x => x.Rename("connection-uri", "Server/Database[@Name='OldDb']", "NewDb"))
                .Returns(Task.CompletedTask);

            var requestParams = new RenameRequestParams
            {
                ConnectionUri = "connection-uri",
                ObjectType = SqlObjectType.Database,
                ObjectUrn = "Server/Database[@Name='OldDb']",
                NewName = "NewDb",
            };

            var operation = new RenameObjectOperation(handler.Object, requestParams)
            {
                SqlTask = new SqlTask()
            };

            operation.Execute(TaskExecutionMode.Execute);

            handler.Verify(x => x.Rename("connection-uri", "Server/Database[@Name='OldDb']", "NewDb"), Times.Once);
            Assert.That(operation.TaskName, Is.EqualTo(string.Format(CultureInfo.CurrentCulture, global::Microsoft.SqlTools.ServiceLayer.SR.RenameTaskName, "OldDb")));
            Assert.That(operation.TargetDatabaseName, Is.EqualTo("NewDb"));
            Assert.That(operation.SqlTask.PercentComplete, Is.EqualTo(-1));
            Assert.That(operation.SqlTask.ProgressMessage, Is.EqualTo("Rename database to 'NewDb'."));
        }
    }
}