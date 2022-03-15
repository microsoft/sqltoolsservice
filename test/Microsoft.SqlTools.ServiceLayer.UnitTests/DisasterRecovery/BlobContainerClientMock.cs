using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Blobs;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.DisasterRecovery
{
    class BlobContainerClientMock : BlobContainerClient
    {

        private bool canGenerateSasUri;

        public BlobContainerClientMock(bool canGenerateSasUri)
        {
            this.canGenerateSasUri = canGenerateSasUri;
        }

        public override bool CanGenerateSasUri
        {
            get
            {
                return canGenerateSasUri;
            }
        }
    }
}
