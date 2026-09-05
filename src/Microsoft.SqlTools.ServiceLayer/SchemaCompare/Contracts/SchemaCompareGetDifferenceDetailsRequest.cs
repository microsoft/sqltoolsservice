//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.SqlCore.SchemaCompare.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.SchemaCompare.Contracts
{
    class SchemaCompareGetDifferenceDetailsRequest
    {
        public static readonly RequestType<SchemaCompareDifferenceDetailsParams, SchemaCompareDifferenceDetailsResult> Type =
            RequestType<SchemaCompareDifferenceDetailsParams, SchemaCompareDifferenceDetailsResult>.Create(
                "schemaCompare/getDifferenceDetails");
    }
}
