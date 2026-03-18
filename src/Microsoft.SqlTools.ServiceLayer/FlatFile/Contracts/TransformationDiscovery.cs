//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System.Collections.Generic;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.FlatFile.Contracts
{
    public class LearnTransformationParams
    {
        public string OperationId { get; set; }

        public IList<string> ColumnNames { get; set; }

        public IList<string> TransformationExamples { get; set; }

        public IList<int> TransformationExampleRowIndices { get; set; }
    }

    public class LearnTransformationResponse
    {
        public IList<string> TransformationPreview { get; set; }
    }

    public class LearnTransformationRequest
    {
        public static readonly RequestType<LearnTransformationParams, LearnTransformationResponse> Type =
            RequestType<LearnTransformationParams, LearnTransformationResponse>.Create("flatfile/learnTransformation");
    }

    public class SaveTransformationParams
    {
        public string OperationId { get; set; }

        public string DerivedColumnName { get; set; }
    }

    public class SaveTransformationResponse
    {
        public int NumTransformations { get; set; }
    }

    public class SaveTransformationRequest
    {
        public static readonly RequestType<SaveTransformationParams, SaveTransformationResponse> Type =
            RequestType<SaveTransformationParams, SaveTransformationResponse>.Create("flatfile/saveTransformation");
    }
}
