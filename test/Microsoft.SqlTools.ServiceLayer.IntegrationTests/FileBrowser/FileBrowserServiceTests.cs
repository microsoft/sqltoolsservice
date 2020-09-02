//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.FileBrowser;
using Microsoft.SqlTools.ServiceLayer.FileBrowser.Contracts;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.Test.Common.RequestContextMocking;
using Moq;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.FileBrowser
{
    /// <summary>
    /// File browser service tests
    /// </summary>
    public class FileBrowserServiceTests
    {
        #region Request handle tests

        [Test]
        public async Task HandleFileBrowserOpenRequestTest()
        {
            var liveConnection = LiveConnectionHelper.InitLiveConnectionInfo();
            FileBrowserService service = new FileBrowserService();
            var openRequestContext = new Mock<RequestContext<bool>>();
            openRequestContext.Setup(x => x.SendResult(It.IsAny<bool>()))
                .Returns(Task.FromResult(new object()));

            var openParams = new FileBrowserOpenParams
            {
                OwnerUri = liveConnection.ConnectionInfo.OwnerUri,
                ExpandPath = "",
                FileFilters = new string[1] {"*"}
            };

            await service.HandleFileBrowserOpenRequest(openParams, openRequestContext.Object);
            openRequestContext.Verify(x => x.SendResult(It.Is<bool>(p => p == true)));
        }

        [Test]
        public async Task HandleFileBrowserExpandRequestTest()
        {
            var liveConnection = LiveConnectionHelper.InitLiveConnectionInfo();
            FileBrowserService service = new FileBrowserService();
            var requestContext = new Mock<RequestContext<bool>>();
            requestContext.Setup(x => x.SendResult(It.IsAny<bool>())).Returns(Task.FromResult(new object()));

            var inputParams = new FileBrowserExpandParams
            {
                OwnerUri = liveConnection.ConnectionInfo.OwnerUri,
                ExpandPath = ""
            };

            await service.HandleFileBrowserExpandRequest(inputParams, requestContext.Object);
            requestContext.Verify(x => x.SendResult(It.Is<bool>(p => p == true)));
        }

        [Test]
        public async Task HandleFileBrowserValidateRequestTest()
        {
            var liveConnection = LiveConnectionHelper.InitLiveConnectionInfo();
            FileBrowserService service = new FileBrowserService();
            var requestContext = new Mock<RequestContext<bool>>();
            requestContext.Setup(x => x.SendResult(It.IsAny<bool>())).Returns(Task.FromResult(new object()));

            var inputParams = new FileBrowserValidateParams
            {
                OwnerUri = liveConnection.ConnectionInfo.OwnerUri,
                ServiceType = ""
            };

            await service.HandleFileBrowserValidateRequest(inputParams, requestContext.Object);
            requestContext.Verify(x => x.SendResult(It.Is<bool>(p => p == true)));
        }

        [Test]
        public async Task HandleFileBrowserCloseRequestTest()
        {
            var liveConnection = LiveConnectionHelper.InitLiveConnectionInfo();
            FileBrowserService service = new FileBrowserService();
            var requestContext = new Mock<RequestContext<FileBrowserCloseResponse>>();
            requestContext.Setup(x => x.SendResult(It.IsAny<FileBrowserCloseResponse>())).Returns(Task.FromResult(new object()));

            var inputParams = new FileBrowserCloseParams
            {
                OwnerUri = liveConnection.ConnectionInfo.OwnerUri
            };

            await service.HandleFileBrowserCloseRequest(inputParams, requestContext.Object);
            requestContext.Verify(x => x.SendResult(It.Is<FileBrowserCloseResponse>(p => p.Succeeded == true)));
        }

        #endregion

        [Test]
        public async Task OpenFileBrowserTest()
        {
            var liveConnection = LiveConnectionHelper.InitLiveConnectionInfo();
            FileBrowserService service = new FileBrowserService();

            var openParams = new FileBrowserOpenParams
            {
                OwnerUri = liveConnection.ConnectionInfo.OwnerUri,
                ExpandPath = "",
                FileFilters = new[] { "*" }
            };

            var efv = new EventFlowValidator<bool>()
                .AddEventValidation(FileBrowserOpenedNotification.Type, eventParams =>
                {
                    Assert.True(eventParams.Succeeded);
                    Assert.NotNull(eventParams.FileTree);
                    Assert.NotNull(eventParams.FileTree.RootNode);
                    Assert.NotNull(eventParams.FileTree.RootNode.Children);
                    Assert.True(eventParams.FileTree.RootNode.Children.Count > 0);
                })
                .Complete();
            await service.RunFileBrowserOpenTask(openParams, efv.Object);
            efv.Validate();
        }

        [Test]
        public async Task ValidateSelectedFilesWithNullValidatorTest()
        {
            var liveConnection = LiveConnectionHelper.InitLiveConnectionInfo();
            FileBrowserService service = new FileBrowserService();

            var validateParams = new FileBrowserValidateParams
            {
                // Do not pass any service so that the file validator will be null
                ServiceType = "",
                OwnerUri = liveConnection.ConnectionInfo.OwnerUri,
                SelectedFiles = new[] { "" }
            };

            var efv = new EventFlowValidator<bool>()
                .AddEventValidation(FileBrowserValidatedNotification.Type, eventParams => Assert.True(eventParams.Succeeded))
                .Complete();

            // Validate files with null file validator
            await service.RunFileBrowserValidateTask(validateParams, efv.Object);
            efv.Validate();
        }

        [Test]
        public async Task InvalidFileValidationTest()
        {
            FileBrowserService service = new FileBrowserService();
            service.RegisterValidatePathsCallback("TestService", ValidatePaths);

            var validateParams = new FileBrowserValidateParams
            {
                // Do not pass any service so that the file validator will be null
                ServiceType = "TestService",
                SelectedFiles = new[] { "" }
            };

            var efv = new EventFlowValidator<bool>()
                .AddEventValidation(FileBrowserValidatedNotification.Type, eventParams => Assert.False(eventParams.Succeeded))
                .Complete();

            // Validate files with null file validator
            await service.RunFileBrowserValidateTask(validateParams, efv.Object);

            // Verify complete notification event was fired and the result
            efv.Validate();
        }

        #region private methods

        private static bool ValidatePaths(FileBrowserValidateEventArgs eventArgs, out string message)
        {
            message = string.Empty;
            return false;
        }

        #endregion
    }
}
