//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.ObjectManagement.Contracts
{
    public class SearchRequestParams : GeneralRequestDetails
    {
        /// <summary>
        /// The context id.
        /// </summary>
        public string? ContextId { get; set; }

        public string[]? ObjectTypes { get; set; }

        public string? SearchText { get; set; }
        public string? Schema { get; set; }
        public string? Database { get; set; }
    }

    public class SearchResultItem
    {
        public string? Name { get; set; }
        public string? Schema { get; set; }
        public string? Type { get; set; }
    }

    public class SearchRequest
    {
        public static readonly RequestType<SearchRequestParams, SearchResultItem[]> Type = RequestType<SearchRequestParams, SearchResultItem[]>.Create("objectManagement/search");
    }
}