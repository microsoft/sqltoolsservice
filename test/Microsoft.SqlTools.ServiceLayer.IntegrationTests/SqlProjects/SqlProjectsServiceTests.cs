//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.SqlServer.Dac.Projects;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.SqlProjects;
using Microsoft.SqlTools.ServiceLayer.SqlProjects.Contracts;
using Moq;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.SqlProjects
{
    public class SqlProjectsServiceTests
    {
        [Test]
        public async Task TestOpenCloseProject()
        {
            // setup

            var newSdkMock = new MockRequest<SqlProjectResult>();
            var newLegacyMock = new MockRequest<SqlProjectResult>();
            var closeMock = new MockRequest<SqlProjectResult>();
            var openMock = new MockRequest<SqlProjectResult>();

            string sdkProjectUri = TestContextHelpers.GetTestProjectPath(nameof(TestOpenCloseProject) + "Sdk");
            string legacyProjectUri = TestContextHelpers.GetTestProjectPath(nameof(TestOpenCloseProject) + "Legacy");

            if (File.Exists(sdkProjectUri)) File.Delete(sdkProjectUri);
            if (File.Exists(legacyProjectUri)) File.Delete(legacyProjectUri);

            var service = new SqlProjectsService();

            Assert.AreEqual(0, service.Projects.Count);

            await service.HandleNewSqlProjectRequest(new NewSqlProjectParams()
            {
                ProjectUri = sdkProjectUri,
                SqlProjectType = ProjectType.SdkStyle

            }, newSdkMock.Object);

            Assert.IsTrue(newSdkMock?.Result?.Success);
            Assert.AreEqual(1, service.Projects.Count);
            Assert.IsTrue(service.Projects.ContainsKey(sdkProjectUri));

            await service.HandleNewSqlProjectRequest(new NewSqlProjectParams()
            {
                ProjectUri = legacyProjectUri,
                SqlProjectType = ProjectType.LegacyStyle
            }, newLegacyMock.Object);

            Assert.IsTrue(newLegacyMock?.Result?.Success);
            Assert.AreEqual(2, service.Projects.Count);
            Assert.IsTrue(service.Projects.ContainsKey(legacyProjectUri));

            await service.HandleCloseSqlProjectRequest(new SqlProjectParams() { ProjectUri = sdkProjectUri }, closeMock.Object);

            Assert.IsTrue(closeMock?.Result?.Success);
            Assert.AreEqual(1, service.Projects.Count);
            Assert.IsTrue(!service.Projects.ContainsKey(sdkProjectUri));

            await service.HandleOpenSqlProjectRequest(new SqlProjectParams() { ProjectUri = sdkProjectUri }, openMock.Object);

            Assert.IsTrue(openMock?.Result?.Success);
            Assert.AreEqual(2, service.Projects.Count);
            Assert.IsTrue(service.Projects.ContainsKey(sdkProjectUri));
        }
    }

    public class MockRequest<T> where T : SqlProjectResult
    {
        private T? result;
        public T Result => result ?? throw new InvalidOperationException("No result has been sent for the request");

        private Mock<RequestContext<T>> Mock;
        public RequestContext<T> Object => Mock.Object;

        public MockRequest()
        {
            Mock = new Mock<RequestContext<T>>();

            Mock.Setup(x => x.SendResult(It.IsAny<T>()))
                .Callback<T>(actual => result = actual)
                .Returns(Task.CompletedTask);
        }
    }
}
