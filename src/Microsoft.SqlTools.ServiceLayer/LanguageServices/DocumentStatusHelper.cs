//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.LanguageServices.Contracts;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.LanguageServices
{
    public class DocumentStatusHelper
    {
        public const string DefinitionRequested = "DefinitionRequested";
        public const string DefinitionRequestCompleted = "DefinitionRequestCompleted";

        public async static Task SendStatusChange(TextDocumentPosition textDocumentPosition, string status)
        {
            string ownerUri = textDocumentPosition != null && textDocumentPosition.TextDocument != null ? textDocumentPosition.TextDocument.Uri : "";
            await ServiceHost.Instance.SendEvent(StatusChangedNotification.Type, new StatusChangeParams()
            {
                OwnerUri = ownerUri,
                Status = status
            });
        }
    }
}
