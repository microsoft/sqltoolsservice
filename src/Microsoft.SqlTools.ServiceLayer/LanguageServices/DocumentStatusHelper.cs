//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.LanguageServices
{
    /// <summary>
    /// Helper class to send events to the client
    /// </summary>
    public class DocumentStatusHelper
    {
        public const string DefinitionRequested = "DefinitionRequested";
        public const string DefinitionRequestCompleted = "DefinitionRequestCompleted";

        /// <summary>
        /// Sends an event for specific document using the existing request context
        /// </summary>
        public async static Task SendStatusChange<T>(RequestContext<T> requestContext, TextDocumentPosition textDocumentPosition, string status)
        {
            if (requestContext != null)
            {
                string ownerUri = textDocumentPosition != null && textDocumentPosition.TextDocument != null ? textDocumentPosition.TextDocument.Uri : "";
                await requestContext.SendEvent(StatusChangedNotification.Type, new StatusChangeParams()
                {
                    OwnerUri = ownerUri,
                    Status = status
                });
            }
        }
    }
}
