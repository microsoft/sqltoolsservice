using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Moq;
using System.Threading.Tasks;
using NUnit.Framework;
using Azure.Storage.Blobs;
using Microsoft.SqlTools.ServiceLayer.DisasterRecovery;
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
            Uri result = sharedAccessSignatureCreator.GetServiceSasUriForContainer(mockBlobContainerClient.Object);
            Assert.AreEqual(result, null);
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
