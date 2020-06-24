//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Metadata.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Diagram.Contracts
{
    public class DiagramSchemaParams 
    {
        public string OwnerUri { get; set; }
    }

    public class DiagramSchemaResult
    {
        public ObjectMetadata[] Metadata { get; set; }
    }

    public class DiagramSchemaRequest
    {
        public static readonly
            RequestType<DiagramSchemaParams, DiagramSchemaResult> Type =
                RequestType<DiagramSchemaParams, DiagramSchemaResult>.Create("diagram/schema");
    }
}

