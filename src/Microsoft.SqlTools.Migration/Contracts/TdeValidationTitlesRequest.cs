//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;

namespace Microsoft.SqlTools.Migration.Contracts
{
    public class TdeValidationTitlesParams
    {
    }

    public class TdeValidationTitlesRequest
    {
        public static readonly RequestType<TdeValidationTitlesParams, string[]> Type =
            RequestType<TdeValidationTitlesParams, string[]>.Create("migration/tdevalidationtitles");
    }
}
