//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Moq;
using NUnit.Framework;
using Azure.Storage.Blobs;
using Microsoft.SqlTools.ServiceLayer.AzureBlob;
using Microsoft.SqlServer.Management.Smo;
using Azure.Storage.Sas;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.DisasterRecovery
{
    class SharedAccessSignatureCreatorTests
    {
        [Test]
        public void GetServiceSasUriForContainerReturnsNullWhenCannotGenerateSasUri()
        {
            var mockBlobContainerClient = new Mock<BlobContainerClient>();
            mockBlobContainerClient.Setup(x => x.CanGenerateSasUri).Returns(false);
            var mockServer = new Server();
            SharedAccessSignatureCreator sharedAccessSignatureCreator = new SharedAccessSignatureCreator(mockServer);
            Assert.Throws<FailedOperationException>(() => sharedAccessSignatureCreator.GetServiceSasUriForContainer(mockBlobContainerClient.Object));
        }

        [Test]
        public void GetServiceSasUriForContainerReturnsSasUri()
        {
            Uri sharedAccessSignatureUriMock = new Uri("https://azureblob/mocked-shared-access-signature");
            var mockBlobContainerClient = new Mock<BlobContainerClient>();
            mockBlobContainerClient.Setup(x => x.CanGenerateSasUri).Returns(true);
            mockBlobContainerClient.Setup(x => x.GenerateSasUri(It.IsAny<BlobSasBuilder>())).Returns(sharedAccessSignatureUriMock);
            var mockServer = new Server();
            SharedAccessSignatureCreator sharedAccessSignatureCreator = new SharedAccessSignatureCreator(mockServer);
            Uri result = sharedAccessSignatureCreator.GetServiceSasUriForContainer(mockBlobContainerClient.Object);
            Assert.AreEqual(result, sharedAccessSignatureUriMock);
        }
    }
}
