//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.FileBrowser;
using Microsoft.SqlTools.ServiceLayer.FileBrowser.Contracts;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Moq;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.FileBrowser
{
    /// <summary>
    /// File browser service tests
    /// </summary>
    public class FileBrowserServiceTests
    {
        #region Request handle tests

        [Fact]
        public async void HandleFileBrowserOpenRequestTest()
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

        [Fact]
        public async void HandleFileBrowserExpandRequestTest()
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

        [Fact]
        public async void HandleFileBrowserValidateRequestTest()
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

        [Fact]
        public async void HandleFileBrowserCloseRequestTest()
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

            // Result should return false since it's trying to close a filebrowser that was never opened
            requestContext.Verify(x => x.SendResult(It.Is<FileBrowserCloseResponse>(p => p.Succeeded == false)));
        }

        #endregion

        [Fact]
        public async void OpenFileBrowserTest()
        {
            var liveConnection = LiveConnectionHelper.InitLiveConnectionInfo();
            FileBrowserService service = new FileBrowserService();

            var openParams = new FileBrowserOpenParams
            {
                OwnerUri = liveConnection.ConnectionInfo.OwnerUri,
                ExpandPath = "",
                FileFilters = new string[1] { "*" }
            };

            var serviceHostMock = new Mock<IProtocolEndpoint>();
            service.ServiceHost = serviceHostMock.Object;
            await service.RunFileBrowserOpenTask(openParams);

            // Verify complete notification event was fired and the result
            serviceHostMock.Verify(x => x.SendEvent(FileBrowserOpenCompleteNotification.Type, 
                It.Is<FileBrowserOpenCompleteParams>(p => p.Succeeded == true
                && p.FileTree != null
                && p.FileTree.RootNode != null
                && p.FileTree.RootNode.Children != null
                && p.FileTree.RootNode.Children.Count > 0)), 
                Times.Once());
        }

        [Fact]
        public async void ValidateSelectedFilesWithNullValidatorTest()
        {
            var liveConnection = LiveConnectionHelper.InitLiveConnectionInfo();
            FileBrowserService service = new FileBrowserService();

            var validateParams = new FileBrowserValidateParams
            {
                // Do not pass any service so that the file validator will be null
                ServiceType = "",
                OwnerUri = liveConnection.ConnectionInfo.OwnerUri,
                SelectedFiles = new string[] { "" }
            };

            var serviceHostMock = new Mock<IProtocolEndpoint>();
            service.ServiceHost = serviceHostMock.Object;

            // Validate files with null file validator
            await service.RunFileBrowserValidateTask(validateParams);

            // Verify complete notification event was fired and the result
            serviceHostMock.Verify(x => x.SendEvent(FileBrowserValidateCompleteNotification.Type, It.Is<FileBrowserValidateCompleteParams>(p => p.Succeeded == true)), Times.Once());
        }

        [Fact]
        public async void InvalidFileValidationTest()
        {
            FileBrowserService service = new FileBrowserService();
            service.RegisterValidatePathsCallback("TestService", ValidatePaths);

            var validateParams = new FileBrowserValidateParams
            {
                // Do not pass any service so that the file validator will be null
                ServiceType = "TestService",
                SelectedFiles = new string[] { "" }
            };

            var serviceHostMock = new Mock<IProtocolEndpoint>();
            service.ServiceHost = serviceHostMock.Object;

            // Validate files with null file validator
            await service.RunFileBrowserValidateTask(validateParams);

            // Verify complete notification event was fired and the result
            serviceHostMock.Verify(x => x.SendEvent(FileBrowserValidateCompleteNotification.Type, It.Is<FileBrowserValidateCompleteParams>(p => p.Succeeded == false)), Times.Once());
        }

        #region private methods

        private bool ValidatePaths(FileBrowserValidateEventArgs eventArgs, out string message)
        {
            message = string.Empty;
            return false;
        }

        #endregion
    }
}
