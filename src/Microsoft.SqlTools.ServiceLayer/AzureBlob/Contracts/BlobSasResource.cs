//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

namespace Microsoft.SqlTools.ServiceLayer.AzureBlob.Contracts
{
    public static class BlobSasResource
    {
        /*
         * Specify "c" if the shared resource is a blob container. 
         * This grants access to the content and metadata of any blob in the container, 
         * and to the list of blobs in the container.
         */
        public const string BLOB_CONTAINER = "c";
        
        /*
         * Specify "b" if the shared resource is a blob. 
         * This grants access to the content and metadata of the blob.
         */
        public const string BLOB = "b";

        /*
         * Beginning in version 2018-11-09, specify "bs" if the shared resource is a blob snapshot. 
         * This grants access to the content and metadata of the specific snapshot, 
         * but not the corresponding root blob.
         */
        public const string BLOB_SNAPSHOT = "bs";

        /*
         * Beginning in version 2019-12-12, specify "bv" if the shared resource is a blob version. 
         * This grants access to the content and metadata of the specific version, 
         * but not the corresponding root blob.
         */
        public const string BLOB_VERSION = "bv";
    }
}
