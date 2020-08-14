//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.Metadata.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Diagram.Contracts
{
    public enum DiagramObject
    {
        Database = 1,
        Schema = 2,
        Table = 3
    }

    public class DiagramRequestParams
    {
        public string OwnerUri { get; set; }
        public string Schema { get; set; }
        public string Server { get; set; }
        public string Database { get; set; }
        public string Table { get; set; }
        public DiagramObject DiagramView { get; set; }

    }

    public class DiagramRequestResult
    {
        public DiagramMetadata Metadata { get; set; }
    }

    public class DiagramModelRequest
    {
        public static readonly
            RequestType<DiagramRequestParams, DiagramRequestResult> Type =
                RequestType<DiagramRequestParams, DiagramRequestResult>.Create("diagram/model");
    }

    public class DiagramPropertiesRequest
    {
        public static readonly
            RequestType<DiagramRequestParams, DiagramRequestResult> Type =
                RequestType<DiagramRequestParams, DiagramRequestResult>.Create("diagram/properties");
    }

    public interface IDiagramMetadata
    {
        public string Name { get; set; }
    }

    public class GridData {
        public Dictionary<string, string>[] rows { get; set; }
    }

    public class DiagramMetadata {
        public string Name { get; set; }
        public Dictionary<string, string> Properties { get; set; }
        public Dictionary<string, GridData> Grids { get; set; }
    }

}

